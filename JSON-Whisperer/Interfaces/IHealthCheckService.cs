using JSON_Whisperer.Models;

namespace JSON_Whisperer.Interfaces
{
    /// <summary>
    /// Service for performing health checks on all system services
    /// </summary>
    public interface IHealthCheckService
    {
        /// <summary>
        /// Performs health checks on all services in parallel
        /// </summary>
        /// <returns>Aggregate health check result for all services</returns>
        Task<HealthCheckResult> CheckAllServicesAsync();

        /// <summary>
        /// Checks the health of the Ollama AI service
        /// </summary>
        /// <returns>Health status of the Ollama service</returns>
        Task<ServiceHealthStatus> CheckOllamaAsync();

        /// <summary>
        /// Checks the health of the ScyllaDB database service
        /// </summary>
        /// <returns>Health status of the ScyllaDB service</returns>
        Task<ServiceHealthStatus> CheckScyllaDbAsync();

        /// <summary>
        /// Checks the health of the embedding service
        /// </summary>
        /// <returns>Health status of the embedding service</returns>
        Task<ServiceHealthStatus> CheckEmbeddingServiceAsync();

        /// <summary>
        /// Checks the health of the knowledge base
        /// </summary>
        /// <returns>Health status of the knowledge base</returns>
        Task<ServiceHealthStatus> CheckKnowledgeBaseAsync();
    }
}
