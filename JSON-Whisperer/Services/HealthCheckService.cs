using Microsoft.Extensions.Logging;
using JSON_Whisperer.Interfaces;
using JSON_Whisperer.Models;

namespace JSON_Whisperer.Services
{
    /// <summary>
    /// Service for performing health checks on all system services
    /// </summary>
    public class HealthCheckService : IHealthCheckService
    {
        private readonly ILogger<HealthCheckService> _logger;
        private readonly IAiService _aiService;
        private readonly IEmbeddingService _embeddingService;
        private readonly IVectorDatabaseService _vectorDatabaseService;
        private readonly IKnowledgeBaseService _knowledgeBaseService;
        private readonly AppSettings _appSettings;

        public HealthCheckService(
            ILogger<HealthCheckService> logger,
            IAiService aiService,
            IEmbeddingService embeddingService,
            IVectorDatabaseService vectorDatabaseService,
            IKnowledgeBaseService knowledgeBaseService,
            AppSettings appSettings)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _aiService = aiService ?? throw new ArgumentNullException(nameof(aiService));
            _embeddingService = embeddingService ?? throw new ArgumentNullException(nameof(embeddingService));
            _vectorDatabaseService = vectorDatabaseService ?? throw new ArgumentNullException(nameof(vectorDatabaseService));
            _knowledgeBaseService = knowledgeBaseService ?? throw new ArgumentNullException(nameof(knowledgeBaseService));
            _appSettings = appSettings ?? throw new ArgumentNullException(nameof(appSettings));
        }

        /// <summary>
        /// Performs health checks on all services in parallel
        /// </summary>
        public async Task<Models.HealthCheckResult> CheckAllServicesAsync()
        {
            _logger.LogInformation("Starting comprehensive health check");
            var startTime = DateTime.UtcNow;

            var result = new Models.HealthCheckResult
            {
                CheckedAt = startTime
            };

            try
            {
                // Execute all health checks in parallel for better performance
                var healthCheckTasks = new[]
                {
                    Task.Run(async () => ("Ollama", await CheckOllamaAsync())),
                    Task.Run(async () => ("ScyllaDB", await CheckScyllaDbAsync())),
                    Task.Run(async () => ("Embedding", await CheckEmbeddingServiceAsync())),
                    Task.Run(async () => ("KnowledgeBase", await CheckKnowledgeBaseAsync()))
                };

                var healthCheckResults = await Task.WhenAll(healthCheckTasks);

                // Populate the service statuses dictionary
                foreach (var (serviceName, status) in healthCheckResults)
                {
                    result.ServiceStatuses[serviceName] = status;
                }

                result.TotalCheckDuration = DateTime.UtcNow - startTime;

                _logger.LogInformation(
                    "Health check completed in {Duration}ms. Overall status: {Status}",
                    result.TotalCheckDuration.TotalMilliseconds,
                    result.AllHealthy ? "HEALTHY" : "UNHEALTHY"
                );

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during comprehensive health check");
                result.TotalCheckDuration = DateTime.UtcNow - startTime;
                return result;
            }
        }

        /// <summary>
        /// Checks the health of the Ollama AI service
        /// </summary>
        public async Task<ServiceHealthStatus> CheckOllamaAsync()
        {
            var status = new ServiceHealthStatus
            {
                ServiceName = "Ollama",
                CheckedAt = DateTime.UtcNow
            };

            var startTime = DateTime.UtcNow;

            try
            {
                _logger.LogDebug("Checking Ollama service health");

                var isAvailable = await _aiService.IsAvailableAsync();
                status.ResponseTime = DateTime.UtcNow - startTime;

                if (isAvailable)
                {
                    status.IsHealthy = true;
                    status.Message = "Service is available and responding";
                    status.Details["BaseUrl"] = _appSettings.Ollama.BaseUrl;
                    status.Details["ModelName"] = _appSettings.Ollama.ModelName;
                    status.Details["ResponseTimeMs"] = status.ResponseTime.TotalMilliseconds.ToString("F0");

                    _logger.LogDebug("Ollama service is healthy");
                }
                else
                {
                    status.IsHealthy = false;
                    status.Message = "Service is not available";
                    status.Details["BaseUrl"] = _appSettings.Ollama.BaseUrl;
                    status.Details["ModelName"] = _appSettings.Ollama.ModelName;

                    _logger.LogWarning("Ollama service is not available");
                }
            }
            catch (Exception ex)
            {
                status.IsHealthy = false;
                status.ResponseTime = DateTime.UtcNow - startTime;
                status.Message = "Error checking service health";
                status.ErrorMessage = ex.Message;
                status.Details["BaseUrl"] = _appSettings.Ollama.BaseUrl;
                status.Details["ExceptionType"] = ex.GetType().Name;

                _logger.LogError(ex, "Error checking Ollama service health");
            }

            return status;
        }

        /// <summary>
        /// Checks the health of the ScyllaDB database service
        /// </summary>
        public async Task<ServiceHealthStatus> CheckScyllaDbAsync()
        {
            var status = new ServiceHealthStatus
            {
                ServiceName = "ScyllaDB",
                CheckedAt = DateTime.UtcNow
            };

            var startTime = DateTime.UtcNow;

            try
            {
                _logger.LogDebug("Checking ScyllaDB health");

                var isConnected = await _vectorDatabaseService.IsConnectedAsync();
                status.ResponseTime = DateTime.UtcNow - startTime;

                if (isConnected)
                {
                    status.IsHealthy = true;
                    status.Message = "Database is connected and responding";

                    // Get additional details
                    var embeddingCount = await _vectorDatabaseService.GetEmbeddingCountAsync();
                    status.Details["ContactPoints"] = _appSettings.ScyllaDb.ContactPoints;
                    status.Details["Port"] = _appSettings.ScyllaDb.Port.ToString();
                    status.Details["Keyspace"] = _appSettings.ScyllaDb.Keyspace;
                    status.Details["EmbeddingCount"] = embeddingCount.ToString();
                    status.Details["ResponseTimeMs"] = status.ResponseTime.TotalMilliseconds.ToString("F0");

                    _logger.LogDebug("ScyllaDB is healthy with {Count} embeddings", embeddingCount);
                }
                else
                {
                    status.IsHealthy = false;
                    status.Message = "Database is not connected";
                    status.Details["ContactPoints"] = _appSettings.ScyllaDb.ContactPoints;
                    status.Details["Port"] = _appSettings.ScyllaDb.Port.ToString();
                    status.Details["Keyspace"] = _appSettings.ScyllaDb.Keyspace;

                    _logger.LogWarning("ScyllaDB is not connected");
                }
            }
            catch (Exception ex)
            {
                status.IsHealthy = false;
                status.ResponseTime = DateTime.UtcNow - startTime;
                status.Message = "Error checking database health";
                status.ErrorMessage = ex.Message;
                status.Details["ContactPoints"] = _appSettings.ScyllaDb.ContactPoints;
                status.Details["Port"] = _appSettings.ScyllaDb.Port.ToString();
                status.Details["ExceptionType"] = ex.GetType().Name;

                _logger.LogError(ex, "Error checking ScyllaDB health");
            }

            return status;
        }

        /// <summary>
        /// Checks the health of the embedding service
        /// </summary>
        public async Task<ServiceHealthStatus> CheckEmbeddingServiceAsync()
        {
            var status = new ServiceHealthStatus
            {
                ServiceName = "Embedding",
                CheckedAt = DateTime.UtcNow
            };

            var startTime = DateTime.UtcNow;

            try
            {
                _logger.LogDebug("Checking embedding service health");

                var isAvailable = await _embeddingService.IsEmbeddingServiceAvailableAsync();
                status.ResponseTime = DateTime.UtcNow - startTime;

                if (isAvailable)
                {
                    status.IsHealthy = true;
                    status.Message = "Embedding service is available and responding";
                    status.Details["EmbeddingModel"] = _embeddingService.GetEmbeddingModelName();
                    status.Details["BaseUrl"] = _appSettings.Ollama.BaseUrl;
                    status.Details["ResponseTimeMs"] = status.ResponseTime.TotalMilliseconds.ToString("F0");

                    _logger.LogDebug("Embedding service is healthy");
                }
                else
                {
                    status.IsHealthy = false;
                    status.Message = "Embedding service is not available";
                    status.Details["EmbeddingModel"] = _embeddingService.GetEmbeddingModelName();
                    status.Details["BaseUrl"] = _appSettings.Ollama.BaseUrl;

                    _logger.LogWarning("Embedding service is not available");
                }
            }
            catch (Exception ex)
            {
                status.IsHealthy = false;
                status.ResponseTime = DateTime.UtcNow - startTime;
                status.Message = "Error checking embedding service health";
                status.ErrorMessage = ex.Message;
                status.Details["EmbeddingModel"] = _embeddingService.GetEmbeddingModelName();
                status.Details["ExceptionType"] = ex.GetType().Name;

                _logger.LogError(ex, "Error checking embedding service health");
            }

            return status;
        }

        /// <summary>
        /// Checks the health of the knowledge base
        /// </summary>
        public async Task<ServiceHealthStatus> CheckKnowledgeBaseAsync()
        {
            var status = new ServiceHealthStatus
            {
                ServiceName = "KnowledgeBase",
                CheckedAt = DateTime.UtcNow
            };

            var startTime = DateTime.UtcNow;

            try
            {
                _logger.LogDebug("Checking knowledge base health");

                var examples = await _knowledgeBaseService.LoadExamplesAsync();
                status.ResponseTime = DateTime.UtcNow - startTime;

                if (examples != null && examples.Count > 0)
                {
                    status.IsHealthy = true;
                    status.Message = "Knowledge base is loaded with examples";
                    status.Details["ExampleCount"] = examples.Count.ToString();
                    status.Details["AppDataPath"] = _appSettings.Vector.AppDataPath;
                    status.Details["ResponseTimeMs"] = status.ResponseTime.TotalMilliseconds.ToString("F0");

                    _logger.LogDebug("Knowledge base is healthy with {Count} examples", examples.Count);
                }
                else
                {
                    // No examples is a warning, not a critical failure
                    status.IsHealthy = true;
                    status.Message = "Knowledge base is accessible but contains no examples";
                    status.Details["ExampleCount"] = "0";
                    status.Details["AppDataPath"] = _appSettings.Vector.AppDataPath;
                    status.Details["Warning"] = "No examples found in knowledge base";

                    _logger.LogWarning("Knowledge base contains no examples");
                }
            }
            catch (Exception ex)
            {
                status.IsHealthy = false;
                status.ResponseTime = DateTime.UtcNow - startTime;
                status.Message = "Error checking knowledge base health";
                status.ErrorMessage = ex.Message;
                status.Details["AppDataPath"] = _appSettings.Vector.AppDataPath;
                status.Details["ExceptionType"] = ex.GetType().Name;

                _logger.LogError(ex, "Error checking knowledge base health");
            }

            return status;
        }
    }
}
