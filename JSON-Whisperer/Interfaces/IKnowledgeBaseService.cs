using JSON_Whisperer.Models;

namespace JSON_Whisperer.Interfaces
{
    /// <summary>
    /// Service for managing the knowledge base of JSON examples
    /// </summary>
    public interface IKnowledgeBaseService
    {
        /// <summary>
        /// Loads JSON examples from the AppData directory
        /// </summary>
        /// <returns>List of JSON examples with their descriptions</returns>
        Task<List<JsonExample>> LoadExamplesAsync();

        /// <summary>
        /// Initializes the vector database with knowledge base examples
        /// </summary>
        /// <returns>Task representing the initialization operation</returns>
        Task InitializeVectorDatabaseAsync();
    }
}