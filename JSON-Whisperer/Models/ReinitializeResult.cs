namespace JSON_Whisperer.Models
{
    /// <summary>
    /// Result of knowledge base reinitialization operation
    /// </summary>
    public class ReinitializeResult
    {
        /// <summary>
        /// Number of JSON files processed
        /// </summary>
        public int FilesProcessed { get; set; }

        /// <summary>
        /// Number of embeddings successfully created
        /// </summary>
        public int EmbeddingsCreated { get; set; }

        /// <summary>
        /// Number of errors encountered during processing
        /// </summary>
        public int Errors { get; set; }

        /// <summary>
        /// Total duration of the reinitialization operation
        /// </summary>
        public TimeSpan Duration { get; set; }

        /// <summary>
        /// List of error messages encountered during processing
        /// </summary>
        public List<string> ErrorMessages { get; set; } = new List<string>();

        /// <summary>
        /// Number of embeddings cleared before reinitialization
        /// </summary>
        public int EmbeddingsCleared { get; set; }

        /// <summary>
        /// Indicates whether the operation completed successfully
        /// </summary>
        public bool Success => Errors == 0 && EmbeddingsCreated > 0;
    }
}
