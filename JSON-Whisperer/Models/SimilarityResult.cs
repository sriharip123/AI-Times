namespace JSON_Whisperer.Models
{
    /// <summary>
    /// Represents the result of a similarity search operation
    /// </summary>
    public class SimilarityResult
    {
        /// <summary>
        /// List of similarity matches found
        /// </summary>
        public List<SimilarityMatch> Matches { get; set; } = new();

        /// <summary>
        /// The highest similarity score among all matches
        /// </summary>
        public float HighestScore { get; set; }

        /// <summary>
        /// Total number of matches found (may be more than returned if limited)
        /// </summary>
        public int TotalMatches { get; set; }

        /// <summary>
        /// Time taken to perform the similarity search
        /// </summary>
        public TimeSpan ProcessingTime { get; set; }

        /// <summary>
        /// The similarity threshold used for the search
        /// </summary>
        public float ThresholdUsed { get; set; }

        /// <summary>
        /// Maximum number of results requested
        /// </summary>
        public int MaxResultsRequested { get; set; }
    }
}