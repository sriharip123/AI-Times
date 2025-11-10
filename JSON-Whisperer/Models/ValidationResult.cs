namespace JSON_Whisperer.Models
{
    /// <summary>
    /// Represents the result of validating a configuration section
    /// </summary>
    public class ValidationResult
    {
        /// <summary>
        /// Name of the configuration section being validated
        /// </summary>
        public string Section { get; set; } = string.Empty;

        /// <summary>
        /// Indicates whether the configuration section is valid
        /// </summary>
        public bool IsValid { get; set; }

        /// <summary>
        /// List of validation errors found
        /// </summary>
        public List<string> Errors { get; set; } = new();

        /// <summary>
        /// List of validation warnings
        /// </summary>
        public List<string> Warnings { get; set; } = new();

        /// <summary>
        /// Timestamp when the validation was performed
        /// </summary>
        public DateTime ValidatedAt { get; set; } = DateTime.UtcNow;
    }
}
