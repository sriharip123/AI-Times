namespace JSON_Whisperer.Models
{
    /// <summary>
    /// Result of knowledge base validation operation
    /// </summary>
    public class KnowledgeBaseValidationResult
    {
        /// <summary>
        /// Total number of JSON files found
        /// </summary>
        public int TotalFiles { get; set; }

        /// <summary>
        /// Number of valid JSON files
        /// </summary>
        public int ValidFiles { get; set; }

        /// <summary>
        /// Number of invalid JSON files
        /// </summary>
        public int InvalidFiles { get; set; }

        /// <summary>
        /// Number of files missing description metadata
        /// </summary>
        public int MissingDescriptions { get; set; }

        /// <summary>
        /// List of validation errors
        /// </summary>
        public List<string> Errors { get; set; } = new List<string>();

        /// <summary>
        /// List of validation warnings
        /// </summary>
        public List<string> Warnings { get; set; } = new List<string>();

        /// <summary>
        /// Indicates whether all files are valid
        /// </summary>
        public bool IsValid => InvalidFiles == 0 && Errors.Count == 0;

        /// <summary>
        /// Duration of the validation operation
        /// </summary>
        public TimeSpan Duration { get; set; }
    }
}
