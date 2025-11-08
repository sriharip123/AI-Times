namespace JSON_Whisperer.Models
{
    /// <summary>
    /// Represents a similarity match result from vector search
    /// </summary>
    public class SimilarityMatch
    {
        /// <summary>
        /// Unique identifier of the matched embedding
        /// </summary>
        public string Id { get; set; } = string.Empty;

        /// <summary>
        /// The JSON content of the matched example
        /// </summary>
        public string JsonContent { get; set; } = string.Empty;

        /// <summary>
        /// Human-readable description of the matched JSON
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Similarity score (0.0 to 1.0, where 1.0 is identical)
        /// </summary>
        public float SimilarityScore { get; set; }

        /// <summary>
        /// Optional metadata from the matched embedding
        /// </summary>
        public Dictionary<string, string>? Metadata { get; set; }
    }
}