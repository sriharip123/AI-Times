using Microsoft.Extensions.Logging;
using JSON_Whisperer.Interfaces;
using JSON_Whisperer.Models;
using System.Diagnostics;

namespace JSON_Whisperer.Services
{
    /// <summary>
    /// Service for testing individual services independently
    /// </summary>
    public class ServiceTestingService : IServiceTestingService
    {
        private readonly ILogger<ServiceTestingService> _logger;
        private readonly IAiService _aiService;
        private readonly IEmbeddingService _embeddingService;
        private readonly IVectorDatabaseService _vectorDatabaseService;
        private readonly ISimilarityService _similarityService;
        private readonly AppSettings _appSettings;

        public ServiceTestingService(
            ILogger<ServiceTestingService> logger,
            IAiService aiService,
            IEmbeddingService embeddingService,
            IVectorDatabaseService vectorDatabaseService,
            ISimilarityService similarityService,
            AppSettings appSettings)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _aiService = aiService ?? throw new ArgumentNullException(nameof(aiService));
            _embeddingService = embeddingService ?? throw new ArgumentNullException(nameof(embeddingService));
            _vectorDatabaseService = vectorDatabaseService ?? throw new ArgumentNullException(nameof(vectorDatabaseService));
            _similarityService = similarityService ?? throw new ArgumentNullException(nameof(similarityService));
            _appSettings = appSettings ?? throw new ArgumentNullException(nameof(appSettings));
        }

        /// <summary>
        /// Tests connectivity to the Ollama service and verifies the model is available
        /// </summary>
        public async Task<TestResult> TestOllamaAsync()
        {
            var result = new TestResult
            {
                TestName = "Ollama Service Test",
                ExecutedAt = DateTime.UtcNow
            };

            var stopwatch = Stopwatch.StartNew();

            try
            {
                _logger.LogInformation("Testing Ollama service connectivity...");

                // Test if Ollama service is available
                var isAvailable = await _aiService.IsAvailableAsync();
                stopwatch.Stop();
                result.Duration = stopwatch.Elapsed;

                if (isAvailable)
                {
                    result.Success = true;
                    result.Message = $"Ollama service is available and model '{_appSettings.Ollama.ModelName}' is loaded";
                    result.Metrics["BaseUrl"] = _appSettings.Ollama.BaseUrl;
                    result.Metrics["ModelName"] = _appSettings.Ollama.ModelName;
                    result.Metrics["ResponseTimeMs"] = result.Duration.TotalMilliseconds;
                    result.Metrics["Status"] = "Connected";

                    _logger.LogInformation(
                        "Ollama service test passed. Model: {Model}, Response time: {ResponseTime}ms",
                        _appSettings.Ollama.ModelName,
                        result.Duration.TotalMilliseconds);
                }
                else
                {
                    result.Success = false;
                    result.Message = $"Ollama service is not available or model '{_appSettings.Ollama.ModelName}' is not loaded";
                    result.ErrorMessage = "Service availability check returned false";
                    result.Metrics["BaseUrl"] = _appSettings.Ollama.BaseUrl;
                    result.Metrics["ModelName"] = _appSettings.Ollama.ModelName;
                    result.Metrics["Status"] = "Unavailable";

                    _logger.LogWarning(
                        "Ollama service test failed. Service is not available at {BaseUrl}",
                        _appSettings.Ollama.BaseUrl);
                }
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                result.Duration = stopwatch.Elapsed;
                result.Success = false;
                result.Message = "Error testing Ollama service";
                result.ErrorMessage = ex.Message;
                result.Metrics["BaseUrl"] = _appSettings.Ollama.BaseUrl;
                result.Metrics["ModelName"] = _appSettings.Ollama.ModelName;
                result.Metrics["ExceptionType"] = ex.GetType().Name;
                result.Metrics["Status"] = "Error";

                _logger.LogError(ex, "Error testing Ollama service");
            }

            return result;
        }

        /// <summary>
        /// Tests connectivity to ScyllaDB and verifies the keyspace exists or can be created
        /// </summary>
        public async Task<TestResult> TestScyllaDbAsync()
        {
            var result = new TestResult
            {
                TestName = "ScyllaDB Service Test",
                ExecutedAt = DateTime.UtcNow
            };

            var stopwatch = Stopwatch.StartNew();

            try
            {
                _logger.LogInformation("Testing ScyllaDB connectivity...");

                // Test if database is connected
                var isConnected = await _vectorDatabaseService.IsConnectedAsync();
                
                if (!isConnected)
                {
                    // Try to initialize the connection
                    _logger.LogInformation("Database not connected. Attempting to initialize...");
                    var initialized = await _vectorDatabaseService.InitializeAsync();
                    
                    if (initialized)
                    {
                        isConnected = await _vectorDatabaseService.IsConnectedAsync();
                    }
                }

                stopwatch.Stop();
                result.Duration = stopwatch.Elapsed;

                if (isConnected)
                {
                    // Get additional information about the database
                    var embeddingCount = await _vectorDatabaseService.GetEmbeddingCountAsync();

                    result.Success = true;
                    result.Message = $"ScyllaDB is connected and keyspace '{_appSettings.ScyllaDb.Keyspace}' is accessible";
                    result.Metrics["ContactPoints"] = _appSettings.ScyllaDb.ContactPoints;
                    result.Metrics["Port"] = _appSettings.ScyllaDb.Port;
                    result.Metrics["Keyspace"] = _appSettings.ScyllaDb.Keyspace;
                    result.Metrics["DataCenter"] = _appSettings.ScyllaDb.DataCenter;
                    result.Metrics["EmbeddingCount"] = embeddingCount;
                    result.Metrics["ResponseTimeMs"] = result.Duration.TotalMilliseconds;
                    result.Metrics["Status"] = "Connected";
                    result.Metrics["KeyspaceVerified"] = true;

                    _logger.LogInformation(
                        "ScyllaDB test passed. Keyspace: {Keyspace}, Embeddings: {Count}, Response time: {ResponseTime}ms",
                        _appSettings.ScyllaDb.Keyspace,
                        embeddingCount,
                        result.Duration.TotalMilliseconds);
                }
                else
                {
                    result.Success = false;
                    result.Message = $"ScyllaDB is not connected or keyspace '{_appSettings.ScyllaDb.Keyspace}' is not accessible";
                    result.ErrorMessage = "Database connection check returned false";
                    result.Metrics["ContactPoints"] = _appSettings.ScyllaDb.ContactPoints;
                    result.Metrics["Port"] = _appSettings.ScyllaDb.Port;
                    result.Metrics["Keyspace"] = _appSettings.ScyllaDb.Keyspace;
                    result.Metrics["Status"] = "Disconnected";
                    result.Metrics["KeyspaceVerified"] = false;

                    _logger.LogWarning(
                        "ScyllaDB test failed. Cannot connect to {ContactPoints}:{Port}",
                        _appSettings.ScyllaDb.ContactPoints,
                        _appSettings.ScyllaDb.Port);
                }
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                result.Duration = stopwatch.Elapsed;
                result.Success = false;
                result.Message = "Error testing ScyllaDB";
                result.ErrorMessage = ex.Message;
                result.Metrics["ContactPoints"] = _appSettings.ScyllaDb.ContactPoints;
                result.Metrics["Port"] = _appSettings.ScyllaDb.Port;
                result.Metrics["Keyspace"] = _appSettings.ScyllaDb.Keyspace;
                result.Metrics["ExceptionType"] = ex.GetType().Name;
                result.Metrics["Status"] = "Error";

                _logger.LogError(ex, "Error testing ScyllaDB");
            }

            return result;
        }

        /// <summary>
        /// Tests embedding generation with the configured model and verifies dimensions
        /// </summary>
        public async Task<TestResult> TestEmbeddingAsync()
        {
            var result = new TestResult
            {
                TestName = "Embedding Service Test",
                ExecutedAt = DateTime.UtcNow
            };

            var stopwatch = Stopwatch.StartNew();

            try
            {
                _logger.LogInformation("Testing embedding service...");

                // Test if embedding service is available
                var isAvailable = await _embeddingService.IsEmbeddingServiceAvailableAsync();

                if (!isAvailable)
                {
                    stopwatch.Stop();
                    result.Duration = stopwatch.Elapsed;
                    result.Success = false;
                    result.Message = $"Embedding service is not available or model '{_embeddingService.GetEmbeddingModelName()}' is not loaded";
                    result.ErrorMessage = "Embedding service availability check returned false";
                    result.Metrics["EmbeddingModel"] = _embeddingService.GetEmbeddingModelName();
                    result.Metrics["BaseUrl"] = _appSettings.Ollama.BaseUrl;
                    result.Metrics["Status"] = "Unavailable";

                    _logger.LogWarning("Embedding service is not available");
                    return result;
                }

                // Generate a test embedding
                var testText = "This is a test JSON document for embedding generation verification.";
                _logger.LogDebug("Generating test embedding for text: {Text}", testText);

                var embedding = await _embeddingService.GenerateEmbeddingAsync(testText);
                stopwatch.Stop();
                result.Duration = stopwatch.Elapsed;

                if (embedding != null && embedding.Length > 0)
                {
                    // Verify embedding dimensions
                    var expectedDimensions = 768; // Default for nomic-embed-text
                    var actualDimensions = embedding.Length;
                    var dimensionsMatch = actualDimensions == expectedDimensions;

                    result.Success = true;
                    result.Message = $"Embedding service is working correctly with model '{_embeddingService.GetEmbeddingModelName()}'";
                    result.Metrics["EmbeddingModel"] = _embeddingService.GetEmbeddingModelName();
                    result.Metrics["BaseUrl"] = _appSettings.Ollama.BaseUrl;
                    result.Metrics["EmbeddingDimensions"] = actualDimensions;
                    result.Metrics["ExpectedDimensions"] = expectedDimensions;
                    result.Metrics["DimensionsMatch"] = dimensionsMatch;
                    result.Metrics["ResponseTimeMs"] = result.Duration.TotalMilliseconds;
                    result.Metrics["Status"] = "Working";
                    result.Metrics["TestTextLength"] = testText.Length;

                    if (!dimensionsMatch)
                    {
                        result.Metrics["Warning"] = $"Embedding dimensions ({actualDimensions}) do not match expected ({expectedDimensions})";
                        _logger.LogWarning(
                            "Embedding dimensions mismatch. Expected: {Expected}, Actual: {Actual}",
                            expectedDimensions,
                            actualDimensions);
                    }

                    _logger.LogInformation(
                        "Embedding service test passed. Model: {Model}, Dimensions: {Dimensions}, Response time: {ResponseTime}ms",
                        _embeddingService.GetEmbeddingModelName(),
                        actualDimensions,
                        result.Duration.TotalMilliseconds);
                }
                else
                {
                    result.Success = false;
                    result.Message = "Embedding generation returned null or empty result";
                    result.ErrorMessage = "Generated embedding is null or has zero dimensions";
                    result.Metrics["EmbeddingModel"] = _embeddingService.GetEmbeddingModelName();
                    result.Metrics["BaseUrl"] = _appSettings.Ollama.BaseUrl;
                    result.Metrics["Status"] = "Failed";

                    _logger.LogWarning("Embedding generation returned null or empty result");
                }
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                result.Duration = stopwatch.Elapsed;
                result.Success = false;
                result.Message = "Error testing embedding service";
                result.ErrorMessage = ex.Message;
                result.Metrics["EmbeddingModel"] = _embeddingService.GetEmbeddingModelName();
                result.Metrics["BaseUrl"] = _appSettings.Ollama.BaseUrl;
                result.Metrics["ExceptionType"] = ex.GetType().Name;
                result.Metrics["Status"] = "Error";

                _logger.LogError(ex, "Error testing embedding service");
            }

            return result;
        }

        /// <summary>
        /// Tests similarity search functionality end-to-end
        /// </summary>
        public async Task<TestResult> TestSimilarityAsync()
        {
            var result = new TestResult
            {
                TestName = "Similarity Search Test",
                ExecutedAt = DateTime.UtcNow
            };

            var stopwatch = Stopwatch.StartNew();

            try
            {
                _logger.LogInformation("Testing similarity search functionality...");

                // Check if similarity service is available
                var isAvailable = await _similarityService.IsAvailableAsync();

                if (!isAvailable)
                {
                    stopwatch.Stop();
                    result.Duration = stopwatch.Elapsed;
                    result.Success = false;
                    result.Message = "Similarity service is not available (embedding or database service may be down)";
                    result.ErrorMessage = "Similarity service availability check returned false";
                    result.Metrics["Status"] = "Unavailable";

                    var config = _similarityService.GetConfiguration();
                    result.Metrics["SimilarityThreshold"] = config.Threshold;
                    result.Metrics["MaxResults"] = config.MaxResults;
                    result.Metrics["IsEnabled"] = config.IsEnabled;
                    result.Metrics["EmbeddingModel"] = config.EmbeddingModel;
                    result.Metrics["KnowledgeBaseSize"] = config.KnowledgeBaseSize;

                    _logger.LogWarning("Similarity service is not available");
                    return result;
                }

                // Perform a test similarity search with sample JSON
                var testJson = @"{
                    ""name"": ""Test User"",
                    ""email"": ""test@example.com"",
                    ""age"": 30,
                    ""active"": true
                }";

                _logger.LogDebug("Performing test similarity search with sample JSON");

                var similarityResult = await _similarityService.FindSimilarJsonAsync(testJson);
                stopwatch.Stop();
                result.Duration = stopwatch.Elapsed;

                if (similarityResult != null)
                {
                    result.Success = true;
                    result.Message = "Similarity search completed successfully";
                    
                    var config = _similarityService.GetConfiguration();
                    result.Metrics["Status"] = "Working";
                    result.Metrics["MatchesFound"] = similarityResult.Matches.Count;
                    result.Metrics["SimilarityThreshold"] = config.Threshold;
                    result.Metrics["MaxResults"] = config.MaxResults;
                    result.Metrics["IsEnabled"] = config.IsEnabled;
                    result.Metrics["EmbeddingModel"] = config.EmbeddingModel;
                    result.Metrics["KnowledgeBaseSize"] = config.KnowledgeBaseSize;
                    result.Metrics["ResponseTimeMs"] = result.Duration.TotalMilliseconds;
                    result.Metrics["TestJsonLength"] = testJson.Length;

                    if (similarityResult.Matches.Count > 0)
                    {
                        var topMatch = similarityResult.Matches.First();
                        result.Metrics["TopMatchScore"] = topMatch.SimilarityScore;
                        result.Metrics["TopMatchId"] = topMatch.Id;
                        
                        _logger.LogInformation(
                            "Similarity search test passed. Matches found: {Count}, Top score: {Score:F3}, Response time: {ResponseTime}ms",
                            similarityResult.Matches.Count,
                            topMatch.SimilarityScore,
                            result.Duration.TotalMilliseconds);
                    }
                    else
                    {
                        result.Metrics["Warning"] = "No matches found (knowledge base may be empty or threshold too high)";
                        _logger.LogInformation(
                            "Similarity search test passed but no matches found. Knowledge base size: {Size}",
                            config.KnowledgeBaseSize);
                    }
                }
                else
                {
                    result.Success = false;
                    result.Message = "Similarity search returned null result";
                    result.ErrorMessage = "FindSimilarJsonAsync returned null";
                    result.Metrics["Status"] = "Failed";

                    _logger.LogWarning("Similarity search returned null result");
                }
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                result.Duration = stopwatch.Elapsed;
                result.Success = false;
                result.Message = "Error testing similarity search";
                result.ErrorMessage = ex.Message;
                result.Metrics["ExceptionType"] = ex.GetType().Name;
                result.Metrics["Status"] = "Error";

                _logger.LogError(ex, "Error testing similarity search");
            }

            return result;
        }
    }
}
