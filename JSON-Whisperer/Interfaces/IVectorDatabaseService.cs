using JSON_Whisperer.Models;

namespace JSON_Whisperer.Interfaces
{
    /// <summary>
    /// Service for managing vector embeddings in ScyllaDB database
    /// </summary>
    public interface IVectorDatabaseService
    {
        /// <summary>
        /// Initializes the database connection and creates necessary tables
        /// </summary>
        /// <returns>True if initialization was successful, false otherwise</returns>
        Task<bool> InitializeAsync();

        /// <summary>
        /// Stores a vector embedding in the database
        /// </summary>
        /// <param name="id">Unique identifier for the embedding</param>
        /// <param name="embedding">Vector embedding as float array</param>
        /// <param name="jsonContent">Original JSON content</param>
        /// <param name="description">Human-readable description</param>
        /// <param name="metadata">Optional metadata</param>
        /// <returns>True if storage was successful, false otherwise</returns>
        Task<bool> StoreEmbeddingAsync(string id, float[] embedding, string jsonContent, string description, Dictionary<string, string>? metadata = null);

        /// <summary>
        /// Finds similar embeddings using cosine similarity
        /// </summary>
        /// <param name="queryEmbedding">Query vector to find similarities for</param>
        /// <param name="maxResults">Maximum number of results to return</param>
        /// <param name="threshold">Minimum similarity threshold (0.0 to 1.0)</param>
        /// <returns>List of similarity matches</returns>
        Task<List<SimilarityMatch>> FindSimilarAsync(float[] queryEmbedding, int maxResults = 5, float threshold = 0.7f);

        /// <summary>
        /// Checks if the database connection is active
        /// </summary>
        /// <returns>True if connected, false otherwise</returns>
        Task<bool> IsConnectedAsync();

        /// <summary>
        /// Gets the total number of embeddings stored in the database
        /// </summary>
        /// <returns>Count of stored embeddings</returns>
        Task<long> GetEmbeddingCountAsync();

        /// <summary>
        /// Checks if an embedding with the given ID already exists
        /// </summary>
        /// <param name="id">Embedding ID to check</param>
        /// <returns>True if exists, false otherwise</returns>
        Task<bool> EmbeddingExistsAsync(string id);

        /// <summary>
        /// Deletes an embedding by ID
        /// </summary>
        /// <param name="id">ID of the embedding to delete</param>
        /// <returns>True if deletion was successful, false otherwise</returns>
        Task<bool> DeleteEmbeddingAsync(string id);

        /// <summary>
        /// Closes the database connection and cleans up resources
        /// </summary>
        Task DisposeAsync();
    }
}