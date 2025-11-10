using JSON_Whisperer.Interfaces;
using JSON_Whisperer.Models;

namespace JSON_Whisperer.Services
{
    /// <summary>
    /// Parses and validates command-line arguments
    /// </summary>
    public class CommandLineParser : ICommandLineParser
    {
        private static readonly Dictionary<string, DiagnosticCommand> DiagnosticFlags = new()
        {
            { "--health-check", DiagnosticCommand.HealthCheck },
            { "--validate-config", DiagnosticCommand.ValidateConfig },
            { "--test-ollama", DiagnosticCommand.TestOllama },
            { "--test-scylla", DiagnosticCommand.TestScylla },
            { "--test-embedding", DiagnosticCommand.TestEmbedding },
            { "--test-similarity", DiagnosticCommand.TestSimilarity },
            { "--reinitialize-knowledge-base", DiagnosticCommand.ReinitializeKnowledgeBase },
            { "--validate-knowledge-base", DiagnosticCommand.ValidateKnowledgeBase },
            { "--benchmark-all", DiagnosticCommand.BenchmarkAll },
            { "--benchmark-similarity", DiagnosticCommand.BenchmarkSimilarity },
            { "--benchmark-vector-operations", DiagnosticCommand.BenchmarkVectorOperations },
            { "--benchmark-embedding", DiagnosticCommand.BenchmarkEmbedding }
        };

        /// <summary>
        /// Parses command-line arguments into structured options
        /// </summary>
        public CommandLineOptions Parse(string[] args)
        {
            var options = new CommandLineOptions();

            if (args == null || args.Length == 0)
            {
                return options;
            }

            for (int i = 0; i < args.Length; i++)
            {
                string arg = args[i].ToLowerInvariant();

                switch (arg)
                {
                    // Help flags
                    case "--help":
                    case "-h":
                        options.HelpRequested = true;
                        options.Mode = ExecutionMode.Help;
                        break;

                    // Verbose flags
                    case "--verbose":
                    case "-v":
                        options.VerboseMode = true;
                        break;

                    // No similarity flag
                    case "--no-similarity":
                        options.NoSimilarity = true;
                        break;

                    // File input flag
                    case "--file":
                    case "-f":
                        if (i + 1 < args.Length)
                        {
                            options.FilePath = args[++i];
                        }
                        else
                        {
                            options.IsValid = false;
                            options.ErrorMessage = $"Flag '{args[i]}' requires a file path argument.";
                            return options;
                        }
                        break;

                    // Diagnostic commands
                    default:
                        if (DiagnosticFlags.TryGetValue(arg, out var diagnosticCommand))
                        {
                            // Only set diagnostic command if not already set and not in Help mode
                            if (options.DiagnosticCommand == null && options.Mode != ExecutionMode.Help)
                            {
                                options.DiagnosticCommand = diagnosticCommand;
                                options.Mode = ExecutionMode.Diagnostic;
                            }
                        }
                        else if (arg.StartsWith("-"))
                        {
                            // Unknown flag
                            options.IsValid = false;
                            options.ErrorMessage = $"Unknown flag: {args[i]}";
                            return options;
                        }
                        else
                        {
                            // Treat as JSON content if not a flag
                            if (string.IsNullOrEmpty(options.JsonContent))
                            {
                                options.JsonContent = args[i];
                            }
                        }
                        break;
                }
            }

            return options;
        }

        /// <summary>
        /// Validates the parsed command-line options
        /// </summary>
        public bool IsValid(CommandLineOptions options, out string errorMessage)
        {
            errorMessage = string.Empty;

            // If already marked invalid during parsing, return that error
            if (!options.IsValid)
            {
                errorMessage = options.ErrorMessage ?? "Invalid command-line arguments.";
                return false;
            }

            // Help mode is always valid
            if (options.Mode == ExecutionMode.Help)
            {
                return true;
            }

            // Diagnostic mode validation
            if (options.Mode == ExecutionMode.Diagnostic)
            {
                if (options.DiagnosticCommand == null)
                {
                    errorMessage = "Diagnostic mode requires a diagnostic command.";
                    return false;
                }

                // Check for conflicting flags with certain diagnostic commands
                if (options.NoSimilarity)
                {
                    var command = options.DiagnosticCommand.Value;
                    if (command == DiagnosticCommand.TestScylla ||
                        command == DiagnosticCommand.TestSimilarity ||
                        command == DiagnosticCommand.ReinitializeKnowledgeBase ||
                        command == DiagnosticCommand.ValidateKnowledgeBase ||
                        command == DiagnosticCommand.BenchmarkSimilarity ||
                        command == DiagnosticCommand.BenchmarkVectorOperations)
                    {
                        errorMessage = $"Flag '--no-similarity' conflicts with diagnostic command '--{GetDiagnosticCommandFlag(command)}'. " +
                                     $"This diagnostic command requires similarity/vector services.";
                        return false;
                    }
                }

                // Validate file path if provided with diagnostic command
                if (!string.IsNullOrEmpty(options.FilePath))
                {
                    var command = options.DiagnosticCommand.Value;
                    // Only certain commands accept file input
                    if (command != DiagnosticCommand.ValidateKnowledgeBase &&
                        command != DiagnosticCommand.ReinitializeKnowledgeBase)
                    {
                        errorMessage = $"Diagnostic command '--{GetDiagnosticCommandFlag(command)}' does not accept file input. " +
                                     $"File input is ignored in diagnostic mode.";
                        // This is a warning, not an error - we'll allow it but the file will be ignored
                        // So we don't return false here
                    }
                }

                return true;
            }

            // Normal mode validation
            if (options.Mode == ExecutionMode.Normal)
            {
                // Both file and JSON content cannot be provided
                if (!string.IsNullOrEmpty(options.FilePath) && !string.IsNullOrEmpty(options.JsonContent))
                {
                    errorMessage = "Cannot specify both --file and direct JSON content. Please use one or the other.";
                    return false;
                }

                // Validate file path exists if provided
                if (!string.IsNullOrEmpty(options.FilePath) && !File.Exists(options.FilePath))
                {
                    errorMessage = $"File not found: {options.FilePath}";
                    return false;
                }

                return true;
            }

            return true;
        }

        /// <summary>
        /// Gets the command-line flag for a diagnostic command
        /// </summary>
        private string GetDiagnosticCommandFlag(DiagnosticCommand command)
        {
            return DiagnosticFlags.FirstOrDefault(kvp => kvp.Value == command).Key?.TrimStart('-') ?? command.ToString();
        }
    }
}
