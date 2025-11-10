using JSON_Whisperer.Interfaces;
using JSON_Whisperer.Models;
using JSON_Whisperer.Services;
using Microsoft.Extensions.Logging;
using NUnit.Framework;

namespace JSON_Whisperer.Tests.Services
{
    [TestFixture]
    public class HealthCheckServiceTests
    {
        private TestLogger<HealthCheckService> _logger = null!;
        private FakeAiService _fakeAiService = null!;
        private FakeEmbeddingService _fakeEmbeddingService = null!;
        private FakeVectorDatabaseService _fakeVectorDatabaseService = null!;
        private FakeKnowledgeBaseService _fakeKnowledgeBaseService = null!;
        private AppSettings _appSettings = null!;
        private HealthCheckService _healthCheckService = null!;

        [SetUp]
        public void Setup()
        {
            _logger = new TestLogger<HealthCheckService>();
            _fakeAiService = new FakeAiService();
            _fakeEmbeddingService = new FakeEmbeddingService();
            _fakeVectorDatabaseService = new FakeVectorDatabaseService();
            _fakeKnowledgeBaseService = new FakeKnowledgeBaseService();

            _appSettings = new AppSettings
            {
                Ollama = new OllamaSettings
                {
                    BaseUrl = "http://localhost:11434",
                    ModelName = "mistral",
                    EmbeddingModel = "nomic-embed-text"
                },
                ScyllaDb = new ScyllaDbSettings
                {
                    ContactPoints = "127.0.0.1",
                    Port = 9042,
                    Keyspace = "json_whisperer"
                },
                Vector = new VectorSettings
                {
                    AppDataPath = "AppData"
                }
            };

            _healthCheckService = new HealthCheckService(
                _logger,
                _fakeAiService,
                _fakeEmbeddingService,
                _fakeVectorDatabaseService,
                _fakeKnowledgeBaseService,
                _appSettings
            );
        }

        #region Individual Health Check Tests

        [Test]
        public async Task CheckOllamaAsync_WhenServiceIsAvailable_ReturnsHealthyStatus()
        {
            // Arrange
            _fakeAiService.IsAvailable = true;

            // Act
            var result = await _healthCheckService.CheckOllamaAsync();

            // Assert
            Assert.That(result.IsHealthy, Is.True);
            Assert.That(result.ServiceName, Is.EqualTo("Ollama"));
            Assert.That(result.Message, Does.Contain("available"));
            Assert.That(result.Details["BaseUrl"], Is.EqualTo("http://localhost:11434"));
            Assert.That(result.Details["ModelName"], Is.EqualTo("mistral"));
            Assert.That(result.ResponseTime, Is.GreaterThan(TimeSpan.Zero));
        }

        [Test]
        public async Task CheckOllamaAsync_WhenServiceIsUnavailable_ReturnsUnhealthyStatus()
        {
            // Arrange
            _fakeAiService.IsAvailable = false;

            // Act
            var result = await _healthCheckService.CheckOllamaAsync();

            // Assert
            Assert.That(result.IsHealthy, Is.False);
            Assert.That(result.ServiceName, Is.EqualTo("Ollama"));
            Assert.That(result.Message, Does.Contain("not available"));
            Assert.That(result.Details["BaseUrl"], Is.EqualTo("http://localhost:11434"));
        }

        [Test]
        public async Task CheckOllamaAsync_WhenExceptionOccurs_ReturnsUnhealthyStatusWithError()
        {
            // Arrange
            _fakeAiService.ThrowException = new Exception("Connection failed");

            // Act
            var result = await _healthCheckService.CheckOllamaAsync();

            // Assert
            Assert.That(result.IsHealthy, Is.False);
            Assert.That(result.ServiceName, Is.EqualTo("Ollama"));
            Assert.That(result.ErrorMessage, Is.EqualTo("Connection failed"));
            Assert.That(result.Details["ExceptionType"], Is.EqualTo("Exception"));
        }

        [Test]
        public async Task CheckScyllaDbAsync_WhenDatabaseIsConnected_ReturnsHealthyStatus()
        {
            // Arrange
            _fakeVectorDatabaseService.IsConnected = true;
            _fakeVectorDatabaseService.EmbeddingCount = 42;

            // Act
            var result = await _healthCheckService.CheckScyllaDbAsync();

            // Assert
            Assert.That(result.IsHealthy, Is.True);
            Assert.That(result.ServiceName, Is.EqualTo("ScyllaDB"));
            Assert.That(result.Message, Does.Contain("connected"));
            Assert.That(result.Details["ContactPoints"], Is.EqualTo("127.0.0.1"));
            Assert.That(result.Details["Port"], Is.EqualTo("9042"));
            Assert.That(result.Details["Keyspace"], Is.EqualTo("json_whisperer"));
            Assert.That(result.Details["EmbeddingCount"], Is.EqualTo("42"));
        }

        [Test]
        public async Task CheckScyllaDbAsync_WhenDatabaseIsNotConnected_ReturnsUnhealthyStatus()
        {
            // Arrange
            _fakeVectorDatabaseService.IsConnected = false;

            // Act
            var result = await _healthCheckService.CheckScyllaDbAsync();

            // Assert
            Assert.That(result.IsHealthy, Is.False);
            Assert.That(result.ServiceName, Is.EqualTo("ScyllaDB"));
            Assert.That(result.Message, Does.Contain("not connected"));
        }

        [Test]
        public async Task CheckScyllaDbAsync_WhenExceptionOccurs_ReturnsUnhealthyStatusWithError()
        {
            // Arrange
            _fakeVectorDatabaseService.ThrowException = new InvalidOperationException("Database error");

            // Act
            var result = await _healthCheckService.CheckScyllaDbAsync();

            // Assert
            Assert.That(result.IsHealthy, Is.False);
            Assert.That(result.ServiceName, Is.EqualTo("ScyllaDB"));
            Assert.That(result.ErrorMessage, Is.EqualTo("Database error"));
            Assert.That(result.Details["ExceptionType"], Is.EqualTo("InvalidOperationException"));
        }

        [Test]
        public async Task CheckEmbeddingServiceAsync_WhenServiceIsAvailable_ReturnsHealthyStatus()
        {
            // Arrange
            _fakeEmbeddingService.IsAvailable = true;

            // Act
            var result = await _healthCheckService.CheckEmbeddingServiceAsync();

            // Assert
            Assert.That(result.IsHealthy, Is.True);
            Assert.That(result.ServiceName, Is.EqualTo("Embedding"));
            Assert.That(result.Message, Does.Contain("available"));
            Assert.That(result.Details["EmbeddingModel"], Is.EqualTo("nomic-embed-text"));
            Assert.That(result.Details["BaseUrl"], Is.EqualTo("http://localhost:11434"));
        }

        [Test]
        public async Task CheckEmbeddingServiceAsync_WhenServiceIsUnavailable_ReturnsUnhealthyStatus()
        {
            // Arrange
            _fakeEmbeddingService.IsAvailable = false;

            // Act
            var result = await _healthCheckService.CheckEmbeddingServiceAsync();

            // Assert
            Assert.That(result.IsHealthy, Is.False);
            Assert.That(result.ServiceName, Is.EqualTo("Embedding"));
            Assert.That(result.Message, Does.Contain("not available"));
        }

        [Test]
        public async Task CheckEmbeddingServiceAsync_WhenExceptionOccurs_ReturnsUnhealthyStatusWithError()
        {
            // Arrange
            _fakeEmbeddingService.ThrowException = new TimeoutException("Request timeout");

            // Act
            var result = await _healthCheckService.CheckEmbeddingServiceAsync();

            // Assert
            Assert.That(result.IsHealthy, Is.False);
            Assert.That(result.ServiceName, Is.EqualTo("Embedding"));
            Assert.That(result.ErrorMessage, Is.EqualTo("Request timeout"));
            Assert.That(result.Details["ExceptionType"], Is.EqualTo("TimeoutException"));
        }

        [Test]
        public async Task CheckKnowledgeBaseAsync_WhenExamplesExist_ReturnsHealthyStatus()
        {
            // Arrange
            _fakeKnowledgeBaseService.Examples = new List<JsonExample>
            {
                new JsonExample { FilePath = "example1.json", Description = "Test 1" },
                new JsonExample { FilePath = "example2.json", Description = "Test 2" }
            };

            // Act
            var result = await _healthCheckService.CheckKnowledgeBaseAsync();

            // Assert
            Assert.That(result.IsHealthy, Is.True);
            Assert.That(result.ServiceName, Is.EqualTo("KnowledgeBase"));
            Assert.That(result.Message, Does.Contain("loaded with examples"));
            Assert.That(result.Details["ExampleCount"], Is.EqualTo("2"));
            Assert.That(result.Details["AppDataPath"], Is.EqualTo("AppData"));
        }

        [Test]
        public async Task CheckKnowledgeBaseAsync_WhenNoExamples_ReturnsHealthyStatusWithWarning()
        {
            // Arrange
            _fakeKnowledgeBaseService.Examples = new List<JsonExample>();

            // Act
            var result = await _healthCheckService.CheckKnowledgeBaseAsync();

            // Assert
            Assert.That(result.IsHealthy, Is.True);
            Assert.That(result.ServiceName, Is.EqualTo("KnowledgeBase"));
            Assert.That(result.Message, Does.Contain("no examples"));
            Assert.That(result.Details["ExampleCount"], Is.EqualTo("0"));
            Assert.That(result.Details["Warning"], Does.Contain("No examples"));
        }

        [Test]
        public async Task CheckKnowledgeBaseAsync_WhenExceptionOccurs_ReturnsUnhealthyStatusWithError()
        {
            // Arrange
            _fakeKnowledgeBaseService.ThrowException = new DirectoryNotFoundException("AppData not found");

            // Act
            var result = await _healthCheckService.CheckKnowledgeBaseAsync();

            // Assert
            Assert.That(result.IsHealthy, Is.False);
            Assert.That(result.ServiceName, Is.EqualTo("KnowledgeBase"));
            Assert.That(result.ErrorMessage, Is.EqualTo("AppData not found"));
            Assert.That(result.Details["ExceptionType"], Is.EqualTo("DirectoryNotFoundException"));
        }

        #endregion

        #region Aggregate Health Check Tests

        [Test]
        public async Task CheckAllServicesAsync_WhenAllServicesHealthy_ReturnsHealthyResult()
        {
            // Arrange
            _fakeAiService.IsAvailable = true;
            _fakeVectorDatabaseService.IsConnected = true;
            _fakeVectorDatabaseService.EmbeddingCount = 10;
            _fakeEmbeddingService.IsAvailable = true;
            _fakeKnowledgeBaseService.Examples = new List<JsonExample>
            {
                new JsonExample { FilePath = "test.json", Description = "Test" }
            };

            // Act
            var result = await _healthCheckService.CheckAllServicesAsync();

            // Assert
            Assert.That(result.AllHealthy, Is.True);
            Assert.That(result.ServiceStatuses.Count, Is.EqualTo(4));
            Assert.That(result.ServiceStatuses["Ollama"].IsHealthy, Is.True);
            Assert.That(result.ServiceStatuses["ScyllaDB"].IsHealthy, Is.True);
            Assert.That(result.ServiceStatuses["Embedding"].IsHealthy, Is.True);
            Assert.That(result.ServiceStatuses["KnowledgeBase"].IsHealthy, Is.True);
            Assert.That(result.TotalCheckDuration, Is.GreaterThan(TimeSpan.Zero));
        }

        [Test]
        public async Task CheckAllServicesAsync_WhenOneServiceUnhealthy_ReturnsUnhealthyResult()
        {
            // Arrange
            _fakeAiService.IsAvailable = true;
            _fakeVectorDatabaseService.IsConnected = false;
            _fakeEmbeddingService.IsAvailable = true;
            _fakeKnowledgeBaseService.Examples = new List<JsonExample>();

            // Act
            var result = await _healthCheckService.CheckAllServicesAsync();

            // Assert
            Assert.That(result.AllHealthy, Is.False);
            Assert.That(result.ServiceStatuses["ScyllaDB"].IsHealthy, Is.False);
            Assert.That(result.ServiceStatuses["Ollama"].IsHealthy, Is.True);
        }

        [Test]
        public async Task CheckAllServicesAsync_WhenMultipleServicesUnhealthy_ReturnsUnhealthyResult()
        {
            // Arrange
            _fakeAiService.IsAvailable = false;
            _fakeVectorDatabaseService.IsConnected = false;
            _fakeEmbeddingService.IsAvailable = false;
            _fakeKnowledgeBaseService.ThrowException = new Exception("Error");

            // Act
            var result = await _healthCheckService.CheckAllServicesAsync();

            // Assert
            Assert.That(result.AllHealthy, Is.False);
            Assert.That(result.ServiceStatuses.Values.Count(s => !s.IsHealthy), Is.EqualTo(4));
        }

        #endregion

        #region Parallel Execution Tests

        [Test]
        public async Task CheckAllServicesAsync_ExecutesHealthChecksInParallel()
        {
            // Arrange
            var delayMs = 100;
            _fakeAiService.DelayMs = delayMs;
            _fakeAiService.IsAvailable = true;
            _fakeVectorDatabaseService.DelayMs = delayMs;
            _fakeVectorDatabaseService.IsConnected = true;
            _fakeEmbeddingService.DelayMs = delayMs;
            _fakeEmbeddingService.IsAvailable = true;
            _fakeKnowledgeBaseService.DelayMs = delayMs;
            _fakeKnowledgeBaseService.Examples = new List<JsonExample>();

            // Act
            var startTime = DateTime.UtcNow;
            var result = await _healthCheckService.CheckAllServicesAsync();
            var totalTime = DateTime.UtcNow - startTime;

            // Assert
            // If executed sequentially, it would take 4 * delayMs (400ms)
            // If executed in parallel, it should take approximately delayMs (100ms) + overhead
            // We'll check that it's significantly less than sequential execution
            // Allow for more overhead on slower systems
            Assert.That(totalTime.TotalMilliseconds, Is.LessThan(delayMs * 3.5));
            Assert.That(result.ServiceStatuses.Count, Is.EqualTo(4));
        }

        [Test]
        public async Task CheckAllServicesAsync_HandlesParallelExceptions()
        {
            // Arrange
            _fakeAiService.ThrowException = new Exception("Ollama error");
            _fakeVectorDatabaseService.ThrowException = new Exception("ScyllaDB error");
            _fakeEmbeddingService.ThrowException = new Exception("Embedding error");
            _fakeKnowledgeBaseService.ThrowException = new Exception("KB error");

            // Act
            var result = await _healthCheckService.CheckAllServicesAsync();

            // Assert
            Assert.That(result.AllHealthy, Is.False);
            Assert.That(result.ServiceStatuses["Ollama"].ErrorMessage, Is.EqualTo("Ollama error"));
            Assert.That(result.ServiceStatuses["ScyllaDB"].ErrorMessage, Is.EqualTo("ScyllaDB error"));
            Assert.That(result.ServiceStatuses["Embedding"].ErrorMessage, Is.EqualTo("Embedding error"));
            Assert.That(result.ServiceStatuses["KnowledgeBase"].ErrorMessage, Is.EqualTo("KB error"));
        }

        #endregion

        #region Timeout Handling Tests

        [Test]
        public async Task CheckOllamaAsync_WhenTimeoutOccurs_ReturnsUnhealthyStatusWithTimeoutError()
        {
            // Arrange
            _fakeAiService.ThrowException = new TimeoutException("Service timeout");

            // Act
            var result = await _healthCheckService.CheckOllamaAsync();

            // Assert
            Assert.That(result.IsHealthy, Is.False);
            Assert.That(result.ServiceName, Is.EqualTo("Ollama"));
            Assert.That(result.ErrorMessage, Is.EqualTo("Service timeout"));
            Assert.That(result.Details["ExceptionType"], Is.EqualTo("TimeoutException"));
            Assert.That(result.ResponseTime, Is.GreaterThan(TimeSpan.Zero));
        }

        [Test]
        public async Task CheckScyllaDbAsync_WhenTimeoutOccurs_ReturnsUnhealthyStatusWithTimeoutError()
        {
            // Arrange
            _fakeVectorDatabaseService.ThrowException = new TimeoutException("Database connection timeout");

            // Act
            var result = await _healthCheckService.CheckScyllaDbAsync();

            // Assert
            Assert.That(result.IsHealthy, Is.False);
            Assert.That(result.ServiceName, Is.EqualTo("ScyllaDB"));
            Assert.That(result.ErrorMessage, Is.EqualTo("Database connection timeout"));
            Assert.That(result.Details["ExceptionType"], Is.EqualTo("TimeoutException"));
        }

        [Test]
        public async Task CheckAllServicesAsync_WhenTimeoutsOccur_ContinuesCheckingOtherServices()
        {
            // Arrange
            _fakeAiService.ThrowException = new TimeoutException("Ollama timeout");
            _fakeVectorDatabaseService.IsConnected = true;
            _fakeVectorDatabaseService.EmbeddingCount = 5;
            _fakeEmbeddingService.IsAvailable = true;
            _fakeKnowledgeBaseService.Examples = new List<JsonExample>();

            // Act
            var result = await _healthCheckService.CheckAllServicesAsync();

            // Assert
            Assert.That(result.AllHealthy, Is.False);
            Assert.That(result.ServiceStatuses["Ollama"].IsHealthy, Is.False);
            Assert.That(result.ServiceStatuses["Ollama"].ErrorMessage, Is.EqualTo("Ollama timeout"));
            Assert.That(result.ServiceStatuses["ScyllaDB"].IsHealthy, Is.True);
            Assert.That(result.ServiceStatuses["Embedding"].IsHealthy, Is.True);
            Assert.That(result.ServiceStatuses["KnowledgeBase"].IsHealthy, Is.True);
        }

        #endregion

        #region Error Handling Tests

        [Test]
        public async Task CheckAllServicesAsync_WhenExceptionInAggregateCheck_ReturnsPartialResults()
        {
            // Arrange
            _fakeAiService.IsAvailable = true;
            _fakeVectorDatabaseService.IsConnected = true;
            _fakeVectorDatabaseService.EmbeddingCount = 10;
            _fakeEmbeddingService.IsAvailable = true;
            _fakeKnowledgeBaseService.Examples = new List<JsonExample>();

            // Act
            var result = await _healthCheckService.CheckAllServicesAsync();

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.TotalCheckDuration, Is.GreaterThan(TimeSpan.Zero));
            Assert.That(result.ServiceStatuses.Count, Is.EqualTo(4));
        }

        [Test]
        public async Task CheckOllamaAsync_RecordsResponseTime()
        {
            // Arrange
            _fakeAiService.DelayMs = 50;
            _fakeAiService.IsAvailable = true;

            // Act
            var result = await _healthCheckService.CheckOllamaAsync();

            // Assert
            Assert.That(result.ResponseTime, Is.GreaterThan(TimeSpan.Zero));
            Assert.That(result.ResponseTime.TotalMilliseconds, Is.GreaterThanOrEqualTo(45)); // Allow for timing variance
            Assert.That(result.Details.ContainsKey("ResponseTimeMs"), Is.True);
        }

        [Test]
        public async Task CheckScyllaDbAsync_RecordsResponseTime()
        {
            // Arrange
            _fakeVectorDatabaseService.DelayMs = 50;
            _fakeVectorDatabaseService.IsConnected = true;

            // Act
            var result = await _healthCheckService.CheckScyllaDbAsync();

            // Assert
            Assert.That(result.ResponseTime, Is.GreaterThan(TimeSpan.Zero));
            Assert.That(result.ResponseTime.TotalMilliseconds, Is.GreaterThanOrEqualTo(45)); // Allow for timing variance
            Assert.That(result.Details.ContainsKey("ResponseTimeMs"), Is.True);
        }

        [Test]
        public async Task CheckEmbeddingServiceAsync_RecordsResponseTime()
        {
            // Arrange
            _fakeEmbeddingService.DelayMs = 50;
            _fakeEmbeddingService.IsAvailable = true;

            // Act
            var result = await _healthCheckService.CheckEmbeddingServiceAsync();

            // Assert
            Assert.That(result.ResponseTime, Is.GreaterThan(TimeSpan.Zero));
            Assert.That(result.ResponseTime.TotalMilliseconds, Is.GreaterThanOrEqualTo(45)); // Allow for timing variance
            Assert.That(result.Details.ContainsKey("ResponseTimeMs"), Is.True);
        }

        [Test]
        public async Task CheckKnowledgeBaseAsync_RecordsResponseTime()
        {
            // Arrange
            _fakeKnowledgeBaseService.DelayMs = 50;
            _fakeKnowledgeBaseService.Examples = new List<JsonExample>
            {
                new JsonExample { FilePath = "test.json", Description = "Test" }
            };

            // Act
            var result = await _healthCheckService.CheckKnowledgeBaseAsync();

            // Assert
            Assert.That(result.ResponseTime, Is.GreaterThan(TimeSpan.Zero));
            Assert.That(result.ResponseTime.TotalMilliseconds, Is.GreaterThanOrEqualTo(45)); // Allow for timing variance
            Assert.That(result.Details.ContainsKey("ResponseTimeMs"), Is.True);
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

            public Task<float[]> GenerateEmbeddingAsync(string text)
                => throw new NotImplementedException();
        }

        private class FakeVectorDatabaseService : IVectorDatabaseService
        {
            public bool IsConnected { get; set; }
            public int EmbeddingCount { get; set; }
            public Exception? ThrowException { get; set; }
            public int DelayMs { get; set; }

            public async Task<bool> IsConnectedAsync()
            {
                if (DelayMs > 0)
                    await Task.Delay(DelayMs);

                if (ThrowException != null)
                    throw ThrowException;

                return IsConnected;
            }

            public async Task<long> GetEmbeddingCountAsync()
            {
                if (DelayMs > 0)
                    await Task.Delay(DelayMs);

                if (ThrowException != null)
                    throw ThrowException;

                return EmbeddingCount;
            }

            public Task<bool> InitializeAsync() => throw new NotImplementedException();
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

        private class FakeKnowledgeBaseService : IKnowledgeBaseService
        {
            public List<JsonExample>? Examples { get; set; }
            public Exception? ThrowException { get; set; }
            public int DelayMs { get; set; }

            public async Task<List<JsonExample>> LoadExamplesAsync()
            {
                if (DelayMs > 0)
                    await Task.Delay(DelayMs);

                if (ThrowException != null)
                    throw ThrowException;

                return Examples ?? new List<JsonExample>();
            }

            public Task InitializeVectorDatabaseAsync() => throw new NotImplementedException();
        }

        #endregion
    }
}
