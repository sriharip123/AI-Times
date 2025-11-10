using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using JSON_Whisperer.Interfaces;
using JSON_Whisperer.Models;
using Microsoft.Extensions.Logging;

namespace JSON_Whisperer.Services
{
    /// <summary>
    /// Handles different types of JSON input sources including command line arguments, files, and stdin
    /// </summary>
    public class InputHandler : IInputHandler
    {
        private readonly ILogger<InputHandler> _logger;
        private const int StdinTimeoutMs = 5000; // 5 second timeout for stdin

        public InputHandler(ILogger<InputHandler> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Gets JSON input from command line arguments, file path, or stdin
        /// </summary>
        /// <param name="args">Command line arguments (for backward compatibility)</param>
        /// <returns>JSON content as string</returns>
        [Obsolete("Use GetJsonInputAsync(CommandLineOptions) instead. This method is maintained for backward compatibility.")]
        public async Task<string> GetJsonInputAsync(string[] args)
        {
            // For backward compatibility, parse arguments using the old logic
            // This allows existing code to continue working
            var parser = new CommandLineParser();
            var options = parser.Parse(args);
            return await GetJsonInputAsync(options);
        }

        /// <summary>
        /// Gets JSON input from command line options, file path, or stdin
        /// </summary>
        /// <param name="options">Parsed command line options</param>
        /// <returns>JSON content as string</returns>
        public async Task<string> GetJsonInputAsync(CommandLineOptions options)
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            try
            {
                // Priority 1: --file flag with file path
                if (!string.IsNullOrWhiteSpace(options.FilePath))
                {
                    _logger.LogInformation("Reading JSON from file: {FilePath}", options.FilePath);
                    return await ReadFromFileAsync(options.FilePath);
                }
                
                // Priority 2: Direct JSON content from command line or already populated
                if (!string.IsNullOrWhiteSpace(options.JsonContent))
                {
                    // Check if it's a file path (for backward compatibility)
                    if (IsFilePath(options.JsonContent))
                    {
                        _logger.LogInformation("Reading JSON from file: {FilePath}", options.JsonContent);
                        return await ReadFromFileAsync(options.JsonContent);
                    }
                    
                    // Treat as direct JSON content
                    _logger.LogInformation("Using command line argument as JSON content");
                    return options.JsonContent;
                }

                // Priority 3: Read from stdin with timeout
                _logger.LogInformation("No arguments provided, attempting to read from stdin");
                return await ReadFromStdinAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting JSON input");
                throw new InvalidOperationException($"Failed to get JSON input: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Validates that the input contains valid JSON content with detailed error reporting
        /// </summary>
        /// <param name="jsonContent">JSON content to validate</param>
        /// <returns>True if valid JSON, false otherwise</returns>
        public bool ValidateInput(string jsonContent)
        {
            if (string.IsNullOrWhiteSpace(jsonContent))
            {
                _logger.LogWarning("JSON content is null, empty, or contains only whitespace");
                return false;
            }

            // Check for common non-JSON content
            var trimmed = jsonContent.Trim();
            if (!trimmed.StartsWith("{") && !trimmed.StartsWith("["))
            {
                _logger.LogWarning("Content does not appear to be JSON (must start with {{ or [)");
                return false;
            }

            try
            {
                var options = new JsonDocumentOptions
                {
                    AllowTrailingCommas = true,
                    CommentHandling = JsonCommentHandling.Skip
                };

                using var document = System.Text.Json.JsonDocument.Parse(jsonContent, options);
                
                // Additional validation - check if it's not just an empty object/array
                if (document.RootElement.ValueKind == JsonValueKind.Object && 
                    document.RootElement.EnumerateObject().Count() == 0)
                {
                    _logger.LogInformation("JSON is valid but contains an empty object");
                }
                else if (document.RootElement.ValueKind == JsonValueKind.Array && 
                         document.RootElement.GetArrayLength() == 0)
                {
                    _logger.LogInformation("JSON is valid but contains an empty array");
                }

                _logger.LogDebug("JSON validation successful - {Type} with {Size} characters", 
                    document.RootElement.ValueKind, jsonContent.Length);
                return true;
            }
            catch (System.Text.Json.JsonException ex)
            {
                _logger.LogWarning("Invalid JSON format at line {Line}, position {Position}: {Error}", 
                    ex.LineNumber, ex.BytePositionInLine, ex.Message);
                return false;
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning("JSON parsing error: {Error}", ex.Message);
                return false;
            }
        }

        /// <summary>
        /// Validates input and provides detailed error information
        /// </summary>
        /// <param name="jsonContent">JSON content to validate</param>
        /// <param name="errorMessage">Detailed error message if validation fails</param>
        /// <returns>True if valid JSON, false otherwise</returns>
        public bool ValidateInputWithDetails(string jsonContent, out string errorMessage)
        {
            errorMessage = string.Empty;

            if (string.IsNullOrWhiteSpace(jsonContent))
            {
                errorMessage = "Input is empty or contains only whitespace";
                return false;
            }

            var trimmed = jsonContent.Trim();
            if (!trimmed.StartsWith("{") && !trimmed.StartsWith("["))
            {
                errorMessage = "Content does not appear to be JSON. JSON must start with '{' (object) or '[' (array)";
                return false;
            }

            try
            {
                var options = new JsonDocumentOptions
                {
                    AllowTrailingCommas = true,
                    CommentHandling = JsonCommentHandling.Skip
                };

                using var document = System.Text.Json.JsonDocument.Parse(jsonContent, options);
                return true;
            }
            catch (System.Text.Json.JsonException ex)
            {
                errorMessage = $"Invalid JSON format at line {ex.LineNumber ?? 0}, position {ex.BytePositionInLine ?? 0}: {ex.Message}";
                return false;
            }
            catch (ArgumentException ex)
            {
                errorMessage = $"JSON parsing error: {ex.Message}";
                return false;
            }
        }

        /// <summary>
        /// Determines if a string is likely a file path
        /// </summary>
        /// <param name="input">Input string to check</param>
        /// <returns>True if it appears to be a file path</returns>
        private static bool IsFilePath(string input)
        {
            // Check for common file path indicators
            return input.Contains(Path.DirectorySeparatorChar) ||
                   input.Contains(Path.AltDirectorySeparatorChar) ||
                   input.EndsWith(".json", StringComparison.OrdinalIgnoreCase) ||
                   (input.Length > 1 && input[1] == ':') || // Windows drive letter
                   input.StartsWith("./") ||
                   input.StartsWith("../") ||
                   input.StartsWith("/");
        }

        /// <summary>
        /// Reads JSON content from a file with comprehensive validation
        /// </summary>
        /// <param name="filePath">Path to the JSON file</param>
        /// <returns>File content as string</returns>
        private async Task<string> ReadFromFileAsync(string filePath)
        {
            // Validate file path
            if (string.IsNullOrWhiteSpace(filePath))
            {
                throw new ArgumentException("File path cannot be null or empty", nameof(filePath));
            }

            // Normalize the file path
            var normalizedPath = Path.GetFullPath(filePath);
            
            // Check file existence
            if (!File.Exists(normalizedPath))
            {
                throw new FileNotFoundException($"File not found: {normalizedPath}");
            }

            // Check file permissions
            var fileInfo = new FileInfo(normalizedPath);
            if (!fileInfo.Exists)
            {
                throw new FileNotFoundException($"File not accessible: {normalizedPath}");
            }

            // Check if it's actually a file (not a directory)
            if ((fileInfo.Attributes & FileAttributes.Directory) == FileAttributes.Directory)
            {
                throw new InvalidOperationException($"Path is a directory, not a file: {normalizedPath}");
            }

            // Check file size (prevent loading extremely large files)
            const long maxFileSize = 100 * 1024 * 1024; // 100MB limit
            if (fileInfo.Length > maxFileSize)
            {
                throw new InvalidOperationException($"File too large: {fileInfo.Length} bytes. Maximum allowed: {maxFileSize} bytes");
            }

            try
            {
                // Try different encodings if UTF-8 fails
                string content;
                try
                {
                    content = await File.ReadAllTextAsync(normalizedPath, Encoding.UTF8);
                }
                catch (DecoderFallbackException)
                {
                    _logger.LogWarning("UTF-8 decoding failed, trying with default encoding");
                    content = await File.ReadAllTextAsync(normalizedPath);
                }

                if (string.IsNullOrWhiteSpace(content))
                {
                    throw new InvalidOperationException($"File is empty or contains only whitespace: {normalizedPath}");
                }

                _logger.LogDebug("Successfully read {Length} characters from file: {Path}", content.Length, normalizedPath);
                return content;
            }
            catch (UnauthorizedAccessException ex)
            {
                throw new UnauthorizedAccessException($"Access denied to file: {normalizedPath}. Check file permissions.", ex);
            }
            catch (DirectoryNotFoundException ex)
            {
                throw new DirectoryNotFoundException($"Directory not found for file: {normalizedPath}", ex);
            }
            catch (PathTooLongException ex)
            {
                throw new PathTooLongException($"File path too long: {normalizedPath}", ex);
            }
            catch (IOException ex)
            {
                throw new IOException($"Error reading file: {normalizedPath}. The file may be locked or corrupted.", ex);
            }
        }



        /// <summary>
        /// Reads JSON content from stdin with timeout
        /// </summary>
        /// <returns>Stdin content as string</returns>
        private async Task<string> ReadFromStdinAsync()
        {
            using var cts = new CancellationTokenSource(StdinTimeoutMs);
            
            try
            {
                var inputBuilder = new StringBuilder();
                var buffer = new char[1024];
                
                // Check if there's data available on stdin
                if (Console.IsInputRedirected || Console.KeyAvailable)
                {
                    using var reader = new StreamReader(Console.OpenStandardInput(), Encoding.UTF8);
                    
                    while (!cts.Token.IsCancellationRequested)
                    {
                        var readTask = reader.ReadAsync(buffer, 0, buffer.Length);
                        var bytesRead = await readTask.WaitAsync(cts.Token);
                        
                        if (bytesRead == 0)
                            break;
                            
                        inputBuilder.Append(buffer, 0, bytesRead);
                    }
                }
                else
                {
                    throw new TimeoutException("No input available on stdin within timeout period");
                }

                var result = inputBuilder.ToString().Trim();
                
                if (string.IsNullOrEmpty(result))
                {
                    throw new InvalidOperationException("No input received from stdin");
                }

                _logger.LogDebug("Successfully read {Length} characters from stdin", result.Length);
                return result;
            }
            catch (OperationCanceledException)
            {
                throw new TimeoutException($"Stdin read timeout after {StdinTimeoutMs}ms");
            }
        }
    }
}