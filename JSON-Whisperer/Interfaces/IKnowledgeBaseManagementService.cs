using JSON_Whisperer.Models;

namespace JSON_Whisperer.Interfaces
{
    /// <summary>
    /// Service for managing knowledge base operations including reinitialization and validation
    /// </summary>
    public interface IKnowledgeBaseManagementService
    {
        /// <summary>
        /// Reinitializes the knowledge base by clearing all embeddings and regenerating them from JSON files
        /// </summary>
        /// <returns>Result containing statistics about the reinitialization operation</returns>
        Task<ReinitializeResult> ReinitializeAsync();

        /// <summary>
        /// Validates the knowledge base by checking JSON files and their descriptions
        /// </summary>
        /// <returns>Result containing validation statistics and errors</returns>
        Task<KnowledgeBaseValidationResult> ValidateAsync();

        /// <summary>
        /// Clears all embeddings from the vector database
        /// </summary>
        /// <returns>Number of embeddings cleared</returns>
        Task<int> ClearAllEmbeddingsAsync();

        /// <summary>
        /// Scans the AppData directory for JSON files
        /// </summary>
        /// <returns>List of file paths found</returns>
        Task<List<string>> ScanJsonFilesAsync();
    }
}
