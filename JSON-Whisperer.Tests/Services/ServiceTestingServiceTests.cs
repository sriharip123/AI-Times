using JSON_Whisperer.Interfaces;
using JSON_Whisperer.Models;
using JSON_Whisperer.Services;
using Microsoft.Extensions.Logging;
using NUnit.Framework;

namespace JSON_Whisperer.Tests.Services
{
    [TestFixture]
    public class ServiceTestingServiceTests
    {
        private TestLogger<ServiceTestingService> _logger = null!;
        private FakeAiService _fakeAiService = null!;
        private FakeEmbeddingService _fakeEmbeddingService = null!;
        private FakeVectorDatabaseService _fakeVectorDatabaseService = null!;
        private FakeSimilarityService _fakeSimilarityService = null!;
        private AppSettings _appSettings = null!;
        private ServiceTestingService _serviceTestingService = null!;

        [SetUp]
        public void Setup()
        {
            _logger = new TestLogger<ServiceTestingService>();
            _fakeAiService = new FakeAiService();
            _fakeEmbeddingService = new FakeEmbeddingService();
            _fakeVectorDatabaseService = new FakeVectorDatabaseService();
            _fakeSimilarityService = new FakeSimilarityService();
            
            _appSettings = new AppSettings
            {
                Ollama = new OllamaSettings
                {
                    BaseUrl = "http://localhost:11434",
                    ModelName = "mistral",
                    EmbeddingModel = "nomic-embed-text",
                    TimeoutSeconds = 30,
                    RetryAttempts = 3,
                    RetryDelaySeconds = 5
                },
                ScyllaDb = new ScyllaDbSettings
                {
                    ContactPoints = "127.0.0.1",
                    Port = 9042,
                    Keyspace = "json_whisperer",
                    DataCenter = "datacenter1",
                    ConnectionTimeoutSeconds = 30,
                    QueryTimeoutSeconds = 30
                },
                Vector = new VectorSettings
                {
                    SimilarityThreshold = 0.7f,
                    MaxSimilarResults = 5,
                    AppDataPath = "AppData",
                    EnableSimilarityMatching = true,
                    InitializeKnowledgeBase = true
                }
            };

            _serviceTestingService = new ServiceTestingService(
                _logger,
                _fakeAiService,
                _fakeEmbeddingService,
                _fakeVectorDatabaseService,
                _fakeSimilarityService,
                _appSettings
            );
        }

        #region TestOllamaAsync Tests

        [Test]
        public async Task TestOllamaAsync_WhenServiceIsAvailable_ReturnsSuccessResult()
        {
            // Arrange
            _fakeAiService.IsAvailable = true;

            // Act
            var result = await _serviceTestingService.TestOllamaAsync();

            // Assert
            Assert.That(result.Success, Is.True);
            Assert.That(result.TestName, Is.EqualTo("Ollama Service Test"));
            Assert.That(result.Message, Does.Contain("available"));
            Assert.That(result.Message, Does.Contain("mistral"));
            Assert.That(result.Duration, Is.GreaterThan(TimeSpan.Zero));
            Assert.That(result.Metrics, Does.ContainKey("BaseUrl"));
            Assert.That(result.Metrics, Does.ContainKey("ModelName"));
            Assert.That(result.Metrics, Does.ContainKey("Status"));
            Assert.That(result.Metrics["Status"], Is.EqualTo("Connected"));
            Assert.That(result.ErrorMessage, Is.Null);
        }

        [Test]
        public async Task TestOllamaAsync_WhenServiceIsUnavailable_ReturnsFailureResult()
        {
            // Arrange
            _fakeAiService.IsAvailable = false;

            // Act
            var result = await _serviceTestingService.TestOllamaAsync();

            // Assert
            Assert.That(result.Success, Is.False);
            Assert.That(result.TestName, Is.EqualTo("Ollama Service Test"));
            Assert.That(result.Message, Does.Contain("not available"));
            Assert.That(result.ErrorMessage, Is.Not.Null);
            Assert.That(result.Metrics, Does.ContainKey("Status"));
            Assert.That(result.Metrics["Status"], Is.EqualTo("Unavailable"));
        }

        [Test]
        public async Task TestOllamaAsync_WhenExceptionOccurs_ReturnsErrorResult()
        {
            // Arrange
            var exceptionMessage = "Connection refused";
            _fakeAiService.ThrowException = new HttpRequestException(exceptionMessage);

            // Act
            var result = await _serviceTestingService.TestOllamaAsync();

            // Assert
            Assert.That(result.Success, Is.False);
            Assert.That(result.TestName, Is.EqualTo("Ollama Service Test"));
            Assert.That(result.Message, Does.Contain("Error"));
            Assert.That(result.ErrorMessage, Does.Contain(exceptionMessage));
            Assert.That(result.Metrics, Does.ContainKey("ExceptionType"));
            Assert.That(result.Metrics["Status"], Is.EqualTo("Error"));
        }

        [Test]
        public async Task TestOllamaAsync_IncludesConfigurationInMetrics()
        {
            // Arrange
            _fakeAiService.IsAvailable = true;

            // Act
            var result = await _serviceTestingService.TestOllamaAsync();

            // Assert
            Assert.That(result.Metrics["BaseUrl"], Is.EqualTo("http://localhost:11434"));
            Assert.That(result.Metrics["ModelName"], Is.EqualTo("mistral"));
            Assert.That(result.Metrics, Does.ContainKey("ResponseTimeMs"));
        }

        #endregion

        #region TestScyllaDbAsync Tests

        [Test]
        public async Task TestScyllaDbAsync_WhenDatabaseIsConnected_ReturnsSuccessResult()
        {
            // Arrange
            _fakeVectorDatabaseService.IsConnected = true;
            _fakeVectorDatabaseService.EmbeddingCount = 42;

            // Act
            var result = await _serviceTestingService.TestScyllaDbAsync();

            // Assert
            Assert.That(result.Success, Is.True);
            Assert.That(result.TestName, Is.EqualTo("ScyllaDB Service Test"));
            Assert.That(result.Message, Does.Contain("connected"));
            Assert.That(result.Message, Does.Contain("json_whisperer"));
            Assert.That(result.Metrics, Does.ContainKey("EmbeddingCount"));
            Assert.That(result.Metrics["EmbeddingCount"], Is.EqualTo(42L));
            Assert.That(result.Metrics["Status"], Is.EqualTo("Connected"));
            Assert.That(result.Metrics["KeyspaceVerified"], Is.EqualTo(true));
        }

        [Test]
        public async Task TestScyllaDbAsync_WhenDatabaseIsNotConnectedButInitializes_ReturnsSuccessResult()
        {
            // Arrange
            _fakeVectorDatabaseService.IsConnected = false;
            _fakeVectorDatabaseService.InitializeSuccess = true;
            _fakeVectorDatabaseService.IsConnectedAfterInit = true;
            _fakeVectorDatabaseService.EmbeddingCount = 0;

            // Act
            var result = await _serviceTestingService.TestScyllaDbAsync();

            // Assert
            Assert.That(result.Success, Is.True);
            Assert.That(result.Metrics["Status"], Is.EqualTo("Connected"));
        }

        [Test]
        public async Task TestScyllaDbAsync_WhenDatabaseCannotConnect_ReturnsFailureResult()
        {
            // Arrange
            _fakeVectorDatabaseService.IsConnected = false;
            _fakeVectorDatabaseService.InitializeSuccess = false;

            // Act
            var result = await _serviceTestingService.TestScyllaDbAsync();

            // Assert
            Assert.That(result.Success, Is.False);
            Assert.That(result.Message, Does.Contain("not connected"));
            Assert.That(result.ErrorMessage, Is.Not.Null);
            Assert.That(result.Metrics["Status"], Is.EqualTo("Disconnected"));
            Assert.That(result.Metrics["KeyspaceVerified"], Is.EqualTo(false));
        }

        [Test]
        public async Task TestScyllaDbAsync_WhenExceptionOccurs_ReturnsErrorResult()
        {
            // Arrange
            var exceptionMessage = "Cassandra connection timeout";
            _fakeVectorDatabaseService.ThrowException = new TimeoutException(exceptionMessage);

            // Act
            var result = await _serviceTestingService.TestScyllaDbAsync();

            // Assert
            Assert.That(result.Success, Is.False);
            Assert.That(result.Message, Does.Contain("Error"));
            Assert.That(result.ErrorMessage, Does.Contain(exceptionMessage));
            Assert.That(result.Metrics["ExceptionType"], Is.EqualTo("TimeoutException"));
            Assert.That(result.Metrics["Status"], Is.EqualTo("Error"));
        }

        [Test]
        public async Task TestScyllaDbAsync_IncludesConfigurationInMetrics()
        {
            // Arrange
            _fakeVectorDatabaseService.IsConnected = true;
            _fakeVectorDatabaseService.EmbeddingCount = 10;

            // Act
            var result = await _serviceTestingService.TestScyllaDbAsync();

            // Assert
            Assert.That(result.Metrics["ContactPoints"], Is.EqualTo("127.0.0.1"));
            Assert.That(result.Metrics["Port"], Is.EqualTo(9042));
            Assert.That(result.Metrics["Keyspace"], Is.EqualTo("json_whisperer"));
            Assert.That(result.Metrics["DataCenter"], Is.EqualTo("datacenter1"));
        }

        #endregion

        #region TestEmbeddingAsync Tests

        [Test]
        public async Task TestEmbeddingAsync_WhenServiceIsAvailableAndGeneratesEmbedding_ReturnsSuccessResult()
        {
            // Arrange
            var testEmbedding = new float[768];
            for (int i = 0; i < 768; i++)
            {
                testEmbedding[i] = 0.1f;
            }

            _fakeEmbeddingService.IsAvailable = true;
            _fakeEmbeddingService.Embedding = testEmbedding;

            // Act
            var result = await _serviceTestingService.TestEmbeddingAsync();

            // Assert
            Assert.That(result.Success, Is.True);
            Assert.That(result.TestName, Is.EqualTo("Embedding Service Test"));
            Assert.That(result.Message, Does.Contain("working correctly"));
            Assert.That(result.Metrics["EmbeddingDimensions"], Is.EqualTo(768));
            Assert.That(result.Metrics["ExpectedDimensions"], Is.EqualTo(768));
            Assert.That(result.Metrics["DimensionsMatch"], Is.EqualTo(true));
            Assert.That(result.Metrics["Status"], Is.EqualTo("Working"));
        }

        [Test]
        public async Task TestEmbeddingAsync_WhenServiceIsUnavailable_ReturnsFailureResult()
        {
            // Arrange
            _fakeEmbeddingService.IsAvailable = false;

            // Act
            var result = await _serviceTestingService.TestEmbeddingAsync();

            // Assert
            Assert.That(result.Success, Is.False);
            Assert.That(result.Message, Does.Contain("not available"));
            Assert.That(result.ErrorMessage, Is.Not.Null);
            Assert.That(result.Metrics["Status"], Is.EqualTo("Unavailable"));
        }

        [Test]
        public async Task TestEmbeddingAsync_WhenEmbeddingIsNull_ReturnsFailureResult()
        {
            // Arrange
            _fakeEmbeddingService.IsAvailable = true;
            _fakeEmbeddingService.Embedding = null;

            // Act
            var result = await _serviceTestingService.TestEmbeddingAsync();

            // Assert
            Assert.That(result.Success, Is.False);
            Assert.That(result.Message, Does.Contain("null or empty"));
            Assert.That(result.ErrorMessage, Does.Contain("null or has zero dimensions"));
            Assert.That(result.Metrics["Status"], Is.EqualTo("Failed"));
        }

        [Test]
        public async Task TestEmbeddingAsync_WhenEmbeddingIsEmpty_ReturnsFailureResult()
        {
            // Arrange
            _fakeEmbeddingService.IsAvailable = true;
            _fakeEmbeddingService.Embedding = Array.Empty<float>();

            // Act
            var result = await _serviceTestingService.TestEmbeddingAsync();

            // Assert
            Assert.That(result.Success, Is.False);
            Assert.That(result.Message, Does.Contain("null or empty"));
            Assert.That(result.Metrics["Status"], Is.EqualTo("Failed"));
        }

        [Test]
        public async Task TestEmbeddingAsync_WhenDimensionsMismatch_ReturnsSuccessWithWarning()
        {
            // Arrange
            var testEmbedding = new float[512]; // Wrong dimensions
            _fakeEmbeddingService.IsAvailable = true;
            _fakeEmbeddingService.Embedding = testEmbedding;

            // Act
            var result = await _serviceTestingService.TestEmbeddingAsync();

            // Assert
            Assert.That(result.Success, Is.True);
            Assert.That(result.Metrics["EmbeddingDimensions"], Is.EqualTo(512));
            Assert.That(result.Metrics["ExpectedDimensions"], Is.EqualTo(768));
            Assert.That(result.Metrics["DimensionsMatch"], Is.EqualTo(false));
            Assert.That(result.Metrics, Does.ContainKey("Warning"));
        }

        [Test]
        public async Task TestEmbeddingAsync_WhenExceptionOccurs_ReturnsErrorResult()
        {
            // Arrange
            var exceptionMessage = "Embedding generation failed";
            _fakeEmbeddingService.IsAvailable = true;
            _fakeEmbeddingService.ThrowException = new InvalidOperationException(exceptionMessage);

            // Act
            var result = await _serviceTestingService.TestEmbeddingAsync();

            // Assert
            Assert.That(result.Success, Is.False);
            Assert.That(result.Message, Does.Contain("Error"));
            Assert.That(result.ErrorMessage, Does.Contain(exceptionMessage));
            Assert.That(result.Metrics["ExceptionType"], Is.EqualTo("InvalidOperationException"));
            Assert.That(result.Metrics["Status"], Is.EqualTo("Error"));
        }

        [Test]
        public async Task TestEmbeddingAsync_VerifiesModelName()
        {
            // Arrange
            var testEmbedding = new float[768];
            _fakeEmbeddingService.IsAvailable = true;
            _fakeEmbeddingService.Embedding = testEmbedding;

            // Act
            var result = await _serviceTestingService.TestEmbeddingAsync();

            // Assert
            Assert.That(result.Metrics["EmbeddingModel"], Is.EqualTo("nomic-embed-text"));
        }

        #endregion

        #region TestSimilarityAsync Tests

        [Test]
        public async Task TestSimilarityAsync_WhenServiceIsAvailableWithMatches_ReturnsSuccessResult()
        {
            // Arrange
            var similarityResult = new SimilarityResult
            {
                Matches = new List<SimilarityMatch>
                {
                    new SimilarityMatch
                    {
                        Id = "test-1",
                        SimilarityScore = 0.95f,
                        JsonContent = "{}",
                        Description = "Test match"
                    }
                }
            };

            _fakeSimilarityService.IsAvailable = true;
            _fakeSimilarityService.SimilarityResult = similarityResult;

            // Act
            var result = await _serviceTestingService.TestSimilarityAsync();

            // Assert
            Assert.That(result.Success, Is.True);
            Assert.That(result.TestName, Is.EqualTo("Similarity Search Test"));
            Assert.That(result.Message, Does.Contain("completed successfully"));
            Assert.That(result.Metrics["Status"], Is.EqualTo("Working"));
            Assert.That(result.Metrics["MatchesFound"], Is.EqualTo(1));
            Assert.That(result.Metrics["TopMatchScore"], Is.EqualTo(0.95f));
            Assert.That(result.Metrics["TopMatchId"], Is.EqualTo("test-1"));
        }

        [Test]
        public async Task TestSimilarityAsync_WhenServiceIsAvailableWithNoMatches_ReturnsSuccessWithWarning()
        {
            // Arrange
            var similarityResult = new SimilarityResult
            {
                Matches = new List<SimilarityMatch>()
            };

            _fakeSimilarityService.IsAvailable = true;
            _fakeSimilarityService.SimilarityResult = similarityResult;
            _fakeSimilarityService.Configuration = new SimilarityConfiguration
            {
                KnowledgeBaseSize = 0
            };

            // Act
            var result = await _serviceTestingService.TestSimilarityAsync();

            // Assert
            Assert.That(result.Success, Is.True);
            Assert.That(result.Metrics["MatchesFound"], Is.EqualTo(0));
            Assert.That(result.Metrics, Does.ContainKey("Warning"));
            Assert.That(result.Metrics["Warning"], Does.Contain("No matches found"));
        }

        [Test]
        public async Task TestSimilarityAsync_WhenServiceIsUnavailable_ReturnsFailureResult()
        {
            // Arrange
            _fakeSimilarityService.IsAvailable = false;

            // Act
            var result = await _serviceTestingService.TestSimilarityAsync();

            // Assert
            Assert.That(result.Success, Is.False);
            Assert.That(result.Message, Does.Contain("not available"));
            Assert.That(result.ErrorMessage, Is.Not.Null);
            Assert.That(result.Metrics["Status"], Is.EqualTo("Unavailable"));
        }

        [Test]
        public async Task TestSimilarityAsync_WhenResultIsNull_ReturnsFailureResult()
        {
            // Arrange
            _fakeSimilarityService.IsAvailable = true;
            _fakeSimilarityService.SimilarityResult = null;

            // Act
            var result = await _serviceTestingService.TestSimilarityAsync();

            // Assert
            Assert.That(result.Success, Is.False);
            Assert.That(result.Message, Does.Contain("null result"));
            Assert.That(result.ErrorMessage, Does.Contain("returned null"));
            Assert.That(result.Metrics["Status"], Is.EqualTo("Failed"));
        }

        [Test]
        public async Task TestSimilarityAsync_WhenExceptionOccurs_ReturnsErrorResult()
        {
            // Arrange
            var exceptionMessage = "Similarity search failed";
            _fakeSimilarityService.IsAvailable = true;
            _fakeSimilarityService.ThrowException = new Exception(exceptionMessage);

            // Act
            var result = await _serviceTestingService.TestSimilarityAsync();

            // Assert
            Assert.That(result.Success, Is.False);
            Assert.That(result.Message, Does.Contain("Error"));
            Assert.That(result.ErrorMessage, Does.Contain(exceptionMessage));
            Assert.That(result.Metrics["ExceptionType"], Is.EqualTo("Exception"));
            Assert.That(result.Metrics["Status"], Is.EqualTo("Error"));
        }

        [Test]
        public async Task TestSimilarityAsync_IncludesConfigurationInMetrics()
        {
            // Arrange
            var config = new SimilarityConfiguration
            {
                Threshold = 0.8f,
                MaxResults = 10,
                IsEnabled = true,
                EmbeddingModel = "custom-model",
                KnowledgeBaseSize = 25
            };

            _fakeSimilarityService.IsAvailable = true;
            _fakeSimilarityService.Configuration = config;
            _fakeSimilarityService.SimilarityResult = new SimilarityResult { Matches = new List<SimilarityMatch>() };

            // Act
            var result = await _serviceTestingService.TestSimilarityAsync();

            // Assert
            Assert.That(result.Metrics["SimilarityThreshold"], Is.EqualTo(0.8f));
            Assert.That(result.Metrics["MaxResults"], Is.EqualTo(10));
            Assert.That(result.Metrics["IsEnabled"], Is.EqualTo(true));
            Assert.That(result.Metrics["EmbeddingModel"], Is.EqualTo("custom-model"));
            Assert.That(result.Metrics["KnowledgeBaseSize"], Is.EqualTo(25L));
        }

        #endregion

        #region Common Test Patterns

        [Test]
        public async Task AllTests_SetExecutedAtTimestamp()
        {
            // Arrange
            _fakeAiService.IsAvailable = true;
            var beforeTest = DateTime.UtcNow.AddSeconds(-1);

            // Act
            var result = await _serviceTestingService.TestOllamaAsync();
            var afterTest = DateTime.UtcNow.AddSeconds(1);

            // Assert
            Assert.That(result.ExecutedAt, Is.GreaterThan(beforeTest));
            Assert.That(result.ExecutedAt, Is.LessThan(afterTest));
        }

        [Test]
        public async Task AllTests_MeasureDuration()
        {
            // Arrange
            _fakeEmbeddingService.IsAvailable = true;
            _fakeEmbeddingService.Embedding = new float[768];

            // Act
            var result = await _serviceTestingService.TestEmbeddingAsync();

            // Assert
            Assert.That(result.Duration, Is.GreaterThan(TimeSpan.Zero));
            Assert.That(result.Metrics, Does.ContainKey("ResponseTimeMs"));
        }

        #endregion

        #region Fake Service Implementations

        private class FakeAiService : IAiService
        {
            public bool IsAvailable { get; set; }
            public Exception? ThrowException { get; set; }
            public int DelayMs { get; set; }

            public async Task<bool> IsAvailableAsync()
            {
                if (DelayMs > 0)
                    await Task.Delay(DelayMs);

                if (ThrowException != null)
                    throw ThrowException;

                return IsAvailable;
            }

            public Task<string> GenerateSummaryAsync(JsonAnalysisResult analysis, string originalJson, SimilarityResult? similarityResult = null)
                => throw new NotImplementedException();
        }

        private class FakeEmbeddingService : IEmbeddingService
        {
            public bool IsAvailable { get; set; }
            public float[]? Embedding { get; set; }
            public Exception? ThrowException { get; set; }
            public int DelayMs { get; set; }

            public async Task<bool> IsEmbeddingServiceAvailableAsync()
            {
                if (DelayMs > 0)
                    await Task.Delay(DelayMs);

                if (ThrowException != null)
                    throw ThrowException;

                return IsAvailable;
            }

            public string GetEmbeddingModelName() => "nomic-embed-text";

            public async Task<float[]> GenerateEmbeddingAsync(string text)
            {
                if (DelayMs > 0)
                    await Task.Delay(DelayMs);

                if (ThrowException != null)
                    throw ThrowException;

                return Embedding ?? Array.Empty<float>();
            }
        }

        private class FakeVectorDatabaseService : IVectorDatabaseService
        {
            public bool IsConnected { get; set; }
            public bool InitializeSuccess { get; set; }
            public bool IsConnectedAfterInit { get; set; }
            public long EmbeddingCount { get; set; }
            public Exception? ThrowException { get; set; }
            public int DelayMs { get; set; }
            private int _isConnectedCallCount = 0;

            public async Task<bool> IsConnectedAsync()
            {
                if (DelayMs > 0)
                    await Task.Delay(DelayMs);

                if (ThrowException != null)
                    throw ThrowException;

                _isConnectedCallCount++;
                
                // First call returns IsConnected, second call (after init) returns IsConnectedAfterInit
                if (_isConnectedCallCount == 1)
                    return IsConnected;
                else
                    return IsConnectedAfterInit;
            }

            public async Task<bool> InitializeAsync()
            {
                if (DelayMs > 0)
                    await Task.Delay(DelayMs);

                if (ThrowException != null)
                    throw ThrowException;

                return InitializeSuccess;
            }

            public async Task<long> GetEmbeddingCountAsync()
            {
                if (DelayMs > 0)
                    await Task.Delay(DelayMs);

                if (ThrowException != null)
                    throw ThrowException;

                return EmbeddingCount;
            }

            public Task<bool> StoreEmbeddingAsync(string id, float[] embedding, string jsonContent, string description, Dictionary<string, string>? metadata = null)
                => throw new NotImplementedException();
            public Task<List<SimilarityMatch>> FindSimilarAsync(float[] queryEmbedding, int maxResults = 5, float threshold = 0.7f)
                => throw new NotImplementedException();
            public Task<bool> EmbeddingExistsAsync(string id) => throw new NotImplementedException();
            public Task<bool> DeleteEmbeddingAsync(string id) => throw new NotImplementedException();
            public Task<int> DeleteAllEmbeddingsAsync() => throw new NotImplementedException();
            public Task<List<string>> GetAllEmbeddingIdsAsync() => throw new NotImplementedException();
            public Task DisposeAsync() => throw new NotImplementedException();
        }

        private class FakeSimilarityService : ISimilarityService
        {
            public bool IsAvailable { get; set; }
            public SimilarityResult? SimilarityResult { get; set; }
            public SimilarityConfiguration Configuration { get; set; } = new SimilarityConfiguration
            {
                Threshold = 0.7f,
                MaxResults = 5,
                IsEnabled = true,
                EmbeddingModel = "nomic-embed-text",
                KnowledgeBaseSize = 10
            };
            public Exception? ThrowException { get; set; }
            public int DelayMs { get; set; }

            public async Task<bool> IsAvailableAsync()
            {
                if (DelayMs > 0)
                    await Task.Delay(DelayMs);

                if (ThrowException != null)
                    throw ThrowException;

                return IsAvailable;
            }

            public async Task<SimilarityResult> FindSimilarJsonAsync(string inputJson)
            {
                if (DelayMs > 0)
                    await Task.Delay(DelayMs);

                if (ThrowException != null)
                    throw ThrowException;

                return SimilarityResult!;
            }

            public float CalculateCosineSimilarity(float[] vector1, float[] vector2)
                => throw new NotImplementedException();

            public SimilarityConfiguration GetConfiguration() => Configuration;
        }

        #endregion
    }
}
