namespace JSON_Whisperer.Models
{
    /// <summary>
    /// Represents parsed command-line options
    /// </summary>
    public class CommandLineOptions
    {
        /// <summary>
        /// The execution mode determined from command-line arguments
        /// </summary>
        public ExecutionMode Mode { get; set; } = ExecutionMode.Normal;

        /// <summary>
        /// Path to input JSON file (--file)
        /// </summary>
        public string? FilePath { get; set; }

        /// <summary>
        /// JSON content provided directly via command line or stdin
        /// </summary>
        public string? JsonContent { get; set; }

        /// <summary>
        /// Enable verbose output mode (--verbose, -v)
        /// </summary>
        public bool VerboseMode { get; set; }

        /// <summary>
        /// Disable similarity matching for this execution (--no-similarity)
        /// </summary>
        public bool NoSimilarity { get; set; }

        /// <summary>
        /// Help information was requested (--help, -h)
        /// </summary>
        public bool HelpRequested { get; set; }

        /// <summary>
        /// The diagnostic command to execute (if Mode is Diagnostic)
        /// </summary>
        public DiagnosticCommand? DiagnosticCommand { get; set; }

        /// <summary>
        /// Indicates whether the options are valid
        /// </summary>
        public bool IsValid { get; set; } = true;

        /// <summary>
        /// Error message if options are invalid
        /// </summary>
        public string? ErrorMessage { get; set; }
    }
}
