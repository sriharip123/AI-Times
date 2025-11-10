namespace JSON_Whisperer.Models
{
    /// <summary>
    /// Defines exit codes for the application
    /// </summary>
    public static class ExitCodes
    {
        /// <summary>
        /// Success - operation completed successfully
        /// </summary>
        public const int Success = 0;

        /// <summary>
        /// General error - unspecified failure
        /// </summary>
        public const int GeneralError = 1;

        /// <summary>
        /// Configuration error - invalid or missing configuration
        /// </summary>
        public const int ConfigurationError = 2;

        /// <summary>
        /// Service unavailable - required service is not accessible
        /// </summary>
        public const int ServiceUnavailable = 3;

        /// <summary>
        /// Validation error - data or configuration validation failed
        /// </summary>
        public const int ValidationError = 4;

        /// <summary>
        /// Argument error - invalid command-line arguments
        /// </summary>
        public const int ArgumentError = 5;
    }
}
