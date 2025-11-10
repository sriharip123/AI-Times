using JSON_Whisperer.Models;

namespace JSON_Whisperer.Interfaces
{
    /// <summary>
    /// Service for testing individual services independently
    /// </summary>
    public interface IServiceTestingService
    {
        /// <summary>
        /// Tests connectivity to the Ollama service and verifies the model is available
        /// </summary>
        /// <returns>Test result with connectivity and model verification details</returns>
        Task<TestResult> TestOllamaAsync();

        /// <summary>
        /// Tests connectivity to ScyllaDB and verifies the keyspace exists or can be created
        /// </summary>
        /// <returns>Test result with database connectivity and keyspace verification details</returns>
        Task<TestResult> TestScyllaDbAsync();

        /// <summary>
        /// Tests embedding generation with the configured model and verifies dimensions
        /// </summary>
        /// <returns>Test result with embedding generation and dimension verification details</returns>
        Task<TestResult> TestEmbeddingAsync();

        /// <summary>
        /// Tests similarity search functionality end-to-end
        /// </summary>
        /// <returns>Test result with similarity search performance details</returns>
        Task<TestResult> TestSimilarityAsync();
    }
}
