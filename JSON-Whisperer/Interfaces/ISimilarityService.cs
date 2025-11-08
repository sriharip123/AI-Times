using JSON_Whisperer.Models;

namespace JSON_Whisperer.Interfaces
{
    /// <summary>
    /// Service for finding similar JSON patterns using vector embeddings
    /// </summary>
    public interface ISimilarityService
    {
        /// <summary>
        /// Finds similar JSON patterns for the given input JSON
        /// </summary>
        /// <param name="inputJson">JSON content to find similarities for</param>
        /// <returns>Similarity result with matches and metadata</returns>
        Task<SimilarityResult> FindSimilarJsonAsync(string inputJson);

        /// <summary>
        /// Calculates cosine similarity between two vectors
        /// </summary>
        /// <param name="vector1">First vector</param>
        /// <param name="vector2">Second vector</param>
        /// <returns>Similarity score (0.0 to 1.0)</returns>
        float CalculateCosineSimilarity(float[] vector1, float[] vector2);

        /// <summary>
        /// Checks if the similarity service is available (embedding and database services are working)
        /// </summary>
        /// <returns>True if service is available, false otherwise</returns>
        Task<bool> IsAvailableAsync();

        /// <summary>
        /// Gets the current configuration for similarity matching
        /// </summary>
        /// <returns>Configuration information</returns>
        SimilarityConfiguration GetConfiguration();
    }

    /// <summary>
    /// Configuration information for similarity matching
    /// </summary>
    public class SimilarityConfiguration
    {
        /// <summary>
        /// Current similarity threshold
        /// </summary>
        public float Threshold { get; set; }

        /// <summary>
        /// Maximum number of results to return
        /// </summary>
        public int MaxResults { get; set; }

        /// <summary>
        /// Whether similarity matching is enabled
        /// </summary>
        public bool IsEnabled { get; set; }

        /// <summary>
        /// Embedding model being used
        /// </summary>
        public string EmbeddingModel { get; set; } = string.Empty;

        /// <summary>
        /// Number of embeddings in the knowledge base
        /// </summary>
        public long KnowledgeBaseSize { get; set; }
    }
}