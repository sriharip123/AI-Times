using JSON_Whisperer.Models;

namespace JSON_Whisperer.Interfaces
{
    /// <summary>
    /// Service for generating vector embeddings using Ollama's embedding API
    /// </summary>
    public interface IEmbeddingService
    {
        /// <summary>
        /// Generates vector embedding for the given JSON content
        /// </summary>
        /// <param name="jsonContent">JSON content to embed</param>
        /// <returns>Vector embedding as float array</returns>
        Task<float[]> GenerateEmbeddingAsync(string jsonContent);

        /// <summary>
        /// Checks if the embedding service is available and the embedding model is loaded
        /// </summary>
        /// <returns>True if service is available, false otherwise</returns>
        Task<bool> IsEmbeddingServiceAvailableAsync();

        /// <summary>
        /// Gets the name of the embedding model being used
        /// </summary>
        /// <returns>Embedding model name</returns>
        string GetEmbeddingModelName();
    }
}