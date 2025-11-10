namespace JSON_Whisperer.Models
{
    /// <summary>
    /// Represents the result of validating all configuration sections
    /// </summary>
    public class ConfigurationValidationResult
    {
        /// <summary>
        /// List of validation results for each configuration section
        /// </summary>
        public List<ValidationResult> Results { get; set; } = new();

        /// <summary>
        /// Indicates whether all configuration sections are valid
        /// </summary>
        public bool IsValid => Results.All(r => r.IsValid);

        /// <summary>
        /// List of all errors across all configuration sections
        /// </summary>
        public List<string> Errors => Results.Where(r => !r.IsValid)
                                             .SelectMany(r => r.Errors)
                                             .ToList();

        /// <summary>
        /// List of all warnings across all configuration sections
        /// </summary>
        public List<string> Warnings => Results.SelectMany(r => r.Warnings)
                                               .ToList();

        /// <summary>
        /// Timestamp when the validation was performed
        /// </summary>
        public DateTime ValidatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Total number of configuration sections validated
        /// </summary>
        public int TotalSections => Results.Count;

        /// <summary>
        /// Number of valid configuration sections
        /// </summary>
        public int ValidSections => Results.Count(r => r.IsValid);

        /// <summary>
        /// Number of invalid configuration sections
        /// </summary>
        public int InvalidSections => Results.Count(r => !r.IsValid);
    }
}
