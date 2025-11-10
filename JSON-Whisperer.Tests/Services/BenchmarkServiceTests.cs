using JSON_Whisperer.Interfaces;
using JSON_Whisperer.Models;
using JSON_Whisperer.Services;
using Microsoft.Extensions.Logging;
using NUnit.Framework;

namespace JSON_Whisperer.Tests.Services
{
    [TestFixture]
    public class BenchmarkServiceTests
    {
        private TestLogger<BenchmarkService> _logger = null!;
        private FakeEmbeddingService _fakeEmbeddingService = null!;
        private FakeVectorDatabaseService _fakeVectorDatabaseService = null!;
        private FakeSimilarityService _fakeSimilarityService = null!;
        private AppSettings _appSettings = null!;
        private BenchmarkService _benchmarkService = null!;

        [SetUp]
        public void Setup()
        {
            _logger = new TestLogger<BenchmarkService>();
            _fakeEmbeddingService = new FakeEmbeddingService();
            _fakeVectorDatabaseService = new FakeVectorDatabaseService();
            _fakeSimilarityService = new FakeSimilarityService();

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
                    SimilarityThreshold = 0.7f,
                    MaxSimilarResults = 5
                }
            };

            _benchmarkService = new BenchmarkService(
                _logger,
                _fakeEmbeddingService,
                _fakeVectorDatabaseService,
                _fakeSimilarityService,
                _appSettings
            );
        }

        #region BenchmarkEmbeddingAsync Tests

        [Test]
        public async Task BenchmarkEmbeddingAsync_WhenServiceIsAvailable_ReturnsSuccessResult()
        {
            // Arrange
            _fakeEmbeddingService.IsAvailable = true;
            _fakeEmbeddingService.Embedding = new float[768];

            // Act
            var result = await _benchmarkService.BenchmarkEmbeddingAsync();

            // Assert
            Assert.That(result.Success, Is.True);
            Assert.That(result.BenchmarkName, Is.EqualTo("Embedding Generation"));
            Assert.That(result.Iterations, Is.EqualTo(20));
            Assert.That(result.TotalDuration, Is.GreaterThan(TimeSpan.Zero));
            Assert.That(result.AverageDurationMs, Is.GreaterThan(0));
            Assert.That(result.OperationsPerSecond, Is.GreaterThan(0));
            Assert.That(result.ExecutedAt, Is.Not.EqualTo(default(DateTime)));
            Assert.That(result.ErrorMessage, Is.Null);
        }

        [Test]
        public async Task BenchmarkEmbeddingAsync_CollectsMetrics()
        {
            // Arrange
            _fakeEmbeddingService.IsAvailable = true;
            _fakeEmbeddingService.Embedding = new float[768];

            // Act
            var result = await _benchmarkService.BenchmarkEmbeddingAsync();

            // Assert
            Assert.That(result.AdditionalMetrics, Does.ContainKey("MinDurationMs"));
            Assert.That(result.AdditionalMetrics, Does.ContainKey("MaxDurationMs"));
            Assert.That(result.AdditionalMetrics, Does.ContainKey("MedianDurationMs"));
            Assert.That(result.AdditionalMetrics, Does.ContainKey("AverageTextLength"));
            Assert.That(result.AdditionalMetrics, Does.ContainKey("EmbeddingDimensions"));
            Assert.That(result.AdditionalMetrics, Does.ContainKey("CharactersPerSecond"));
            Assert.That(result.AdditionalMetrics["EmbeddingDimensions"], Is.EqualTo(768));
        }

        [Test]
        public async Task BenchmarkEmbeddingAsync_WhenServiceIsUnavailable_ReturnsFailureResult()
        {
            // Arrange
            _fakeEmbeddingService.IsAvailable = false;

            // Act
            var result = await _benchmarkService.BenchmarkEmbeddingAsync();

            // Assert
            Assert.That(result.Success, Is.False);
            Assert.That(result.ErrorMessage, Is.EqualTo("Embedding service is not available"));
            Assert.That(result.TotalDuration, Is.EqualTo(TimeSpan.Zero));
            Assert.That(result.AverageDurationMs, Is.EqualTo(0));
        }

        [Test]
        public async Task BenchmarkEmbeddingAsync_WhenExceptionOccurs_ReturnsErrorResult()
        {
            // Arrange
            _fakeEmbeddingService.IsAvailable = true;
            _fakeEmbeddingService.ThrowException = new Exception("Embedding generation failed");

            // Act
            var result = await _benchmarkService.BenchmarkEmbeddingAsync();

            // Assert
            Assert.That(result.Success, Is.False);
            Assert.That(result.ErrorMessage, Is.EqualTo("Embedding generation failed"));
        }

        #endregion

        #region BenchmarkVectorOperationsAsync Tests

        [Test]
        public async Task BenchmarkVectorOperationsAsync_WhenServicesAvailable_ReturnsSuccessResult()
        {
            // Arrange
            _fakeEmbeddingService.IsAvailable = true;
            _fakeEmbeddingService.Embedding = new float[768];
            _fakeVectorDatabaseService.IsConnected = true;
            _fakeVectorDatabaseService.StoreSuccess = true;

            // Act
            var result = await _benchmarkService.BenchmarkVectorOperationsAsync();

            // Assert
            Assert.That(result.Success, Is.True);
            Assert.That(result.BenchmarkName, Is.EqualTo("Vector Operations"));
            Assert.That(result.Iterations, Is.EqualTo(10));
            Assert.That(result.TotalDuration, Is.GreaterThan(TimeSpan.Zero));
            Assert.That(result.AverageDurationMs, Is.GreaterThan(0));
            Assert.That(result.OperationsPerSecond, Is.GreaterThan(0));
        }

        [Test]
        public async Task BenchmarkVectorOperationsAsync_CollectsMetrics()
        {
            // Arrange
            _fakeEmbeddingService.IsAvailable = true;
            _fakeEmbeddingService.Embedding = new float[768];
            _fakeVectorDatabaseService.IsConnected = true;
            _fakeVectorDatabaseService.StoreSuccess = true;

            // Act
            var result = await _benchmarkService.BenchmarkVectorOperationsAsync();

            // Assert
            Assert.That(result.AdditionalMetrics, Does.ContainKey("AvgEmbeddingGenerationMs"));
            Assert.That(result.AdditionalMetrics, Does.ContainKey("AvgStorageMs"));
            Assert.That(result.AdditionalMetrics, Does.ContainKey("MinEmbeddingMs"));
            Assert.That(result.AdditionalMetrics, Does.ContainKey("MaxEmbeddingMs"));
            Assert.That(result.AdditionalMetrics, Does.ContainKey("MinStorageMs"));
            Assert.That(result.AdditionalMetrics, Does.ContainKey("MaxStorageMs"));
            Assert.That(result.AdditionalMetrics, Does.ContainKey("EmbeddingDimensions"));
            Assert.That(result.AdditionalMetrics["EmbeddingDimensions"], Is.EqualTo(768));
        }

        [Test]
        public async Task BenchmarkVectorOperationsAsync_WhenEmbeddingServiceUnavailable_ReturnsFailureResult()
        {
            // Arrange
            _fakeEmbeddingService.IsAvailable = false;
            _fakeVectorDatabaseService.IsConnected = true;

            // Act
            var result = await _benchmarkService.BenchmarkVectorOperationsAsync();

            // Assert
            Assert.That(result.Success, Is.False);
            Assert.That(result.ErrorMessage, Does.Contain("not available"));
        }

        [Test]
        public async Task BenchmarkVectorOperationsAsync_WhenDatabaseNotConnected_ReturnsFailureResult()
        {
            // Arrange
            _fakeEmbeddingService.IsAvailable = true;
            _fakeVectorDatabaseService.IsConnected = false;

            // Act
            var result = await _benchmarkService.BenchmarkVectorOperationsAsync();

            // Assert
            Assert.That(result.Success, Is.False);
            Assert.That(result.ErrorMessage, Does.Contain("not available"));
        }

        [Test]
        public async Task BenchmarkVectorOperationsAsync_CleansUpTestData()
        {
            // Arrange
            _fakeEmbeddingService.IsAvailable = true;
            _fakeEmbeddingService.Embedding = new float[768];
            _fakeVectorDatabaseService.IsConnected = true;
            _fakeVectorDatabaseService.StoreSuccess = true;

            // Act
            var result = await _benchmarkService.BenchmarkVectorOperationsAsync();

            // Assert
            Assert.That(result.Success, Is.True);
            Assert.That(_fakeVectorDatabaseService.DeletedIds.Count, Is.EqualTo(10));
        }

        [Test]
        public async Task BenchmarkVectorOperationsAsync_WhenExceptionOccurs_ReturnsErrorResult()
        {
            // Arrange
            _fakeEmbeddingService.IsAvailable = true;
            _fakeEmbeddingService.ThrowException = new Exception("Vector operation failed");

            // Act
            var result = await _benchmarkService.BenchmarkVectorOperationsAsync();

            // Assert
            Assert.That(result.Success, Is.False);
            Assert.That(result.ErrorMessage, Is.EqualTo("Vector operation failed"));
        }

        #endregion

        #region BenchmarkSimilarityAsync Tests

        [Test]
        public async Task BenchmarkSimilarityAsync_WhenServiceIsAvailable_ReturnsSuccessResult()
        {
            // Arrange
            _fakeSimilarityService.IsAvailable = true;
            _fakeSimilarityService.SimilarityResult = new SimilarityResult
            {
                Matches = new List<SimilarityMatch>
                {
                    new SimilarityMatch { Id = "test-1", SimilarityScore = 0.9f }
                }
            };

            // Act
            var result = await _benchmarkService.BenchmarkSimilarityAsync();

            // Assert
            Assert.That(result.Success, Is.True);
            Assert.That(result.BenchmarkName, Is.EqualTo("Similarity Search"));
            Assert.That(result.Iterations, Is.EqualTo(15));
            Assert.That(result.TotalDuration, Is.GreaterThan(TimeSpan.Zero));
            Assert.That(result.AverageDurationMs, Is.GreaterThan(0));
            Assert.That(result.OperationsPerSecond, Is.GreaterThan(0));
        }

        [Test]
        public async Task BenchmarkSimilarityAsync_CollectsMetrics()
        {
            // Arrange
            _fakeSimilarityService.IsAvailable = true;
            _fakeSimilarityService.SimilarityResult = new SimilarityResult
            {
                Matches = new List<SimilarityMatch>
                {
                    new SimilarityMatch { Id = "test-1", SimilarityScore = 0.9f },
                    new SimilarityMatch { Id = "test-2", SimilarityScore = 0.8f }
                }
            };

            // Act
            var result = await _benchmarkService.BenchmarkSimilarityAsync();

            // Assert
            Assert.That(result.AdditionalMetrics, Does.ContainKey("MinDurationMs"));
            Assert.That(result.AdditionalMetrics, Does.ContainKey("MaxDurationMs"));
            Assert.That(result.AdditionalMetrics, Does.ContainKey("MedianDurationMs"));
            Assert.That(result.AdditionalMetrics, Does.ContainKey("AverageMatchesFound"));
            Assert.That(result.AdditionalMetrics, Does.ContainKey("TotalMatchesFound"));
            Assert.That(result.AdditionalMetrics, Does.ContainKey("SimilarityThreshold"));
            Assert.That(result.AdditionalMetrics, Does.ContainKey("KnowledgeBaseSize"));
            Assert.That(result.AdditionalMetrics["AverageMatchesFound"], Is.EqualTo(2));
            Assert.That(result.AdditionalMetrics["TotalMatchesFound"], Is.EqualTo(30)); // 15 iterations * 2 matches
        }

        [Test]
        public async Task BenchmarkSimilarityAsync_WhenServiceIsUnavailable_ReturnsFailureResult()
        {
            // Arrange
            _fakeSimilarityService.IsAvailable = false;

            // Act
            var result = await _benchmarkService.BenchmarkSimilarityAsync();

            // Assert
            Assert.That(result.Success, Is.False);
            Assert.That(result.ErrorMessage, Is.EqualTo("Similarity service is not available"));
        }

        [Test]
        public async Task BenchmarkSimilarityAsync_WhenExceptionOccurs_ReturnsErrorResult()
        {
            // Arrange
            _fakeSimilarityService.IsAvailable = true;
            _fakeSimilarityService.ThrowException = new Exception("Similarity search failed");

            // Act
            var result = await _benchmarkService.BenchmarkSimilarityAsync();

            // Assert
            Assert.That(result.Success, Is.False);
            Assert.That(result.ErrorMessage, Is.EqualTo("Similarity search failed"));
        }

        #endregion

        #region RunAllBenchmarksAsync Tests

        [Test]
        public async Task RunAllBenchmarksAsync_ExecutesAllBenchmarks()
        {
            // Arrange
            _fakeEmbeddingService.IsAvailable = true;
            _fakeEmbeddingService.Embedding = new float[768];
            _fakeVectorDatabaseService.IsConnected = true;
            _fakeVectorDatabaseService.StoreSuccess = true;
            _fakeSimilarityService.IsAvailable = true;
            _fakeSimilarityService.SimilarityResult = new SimilarityResult { Matches = new List<SimilarityMatch>() };

            // Act
            var results = await _benchmarkService.RunAllBenchmarksAsync();

            // Assert
            Assert.That(results.Count, Is.EqualTo(3));
            Assert.That(results[0].BenchmarkName, Is.EqualTo("Embedding Generation"));
            Assert.That(results[1].BenchmarkName, Is.EqualTo("Vector Operations"));
            Assert.That(results[2].BenchmarkName, Is.EqualTo("Similarity Search"));
        }

        [Test]
        public async Task RunAllBenchmarksAsync_WhenAllSucceed_ReturnsAllSuccessResults()
        {
            // Arrange
            _fakeEmbeddingService.IsAvailable = true;
            _fakeEmbeddingService.Embedding = new float[768];
            _fakeVectorDatabaseService.IsConnected = true;
            _fakeVectorDatabaseService.StoreSuccess = true;
            _fakeSimilarityService.IsAvailable = true;
            _fakeSimilarityService.SimilarityResult = new SimilarityResult { Matches = new List<SimilarityMatch>() };

            // Act
            var results = await _benchmarkService.RunAllBenchmarksAsync();

            // Assert
            Assert.That(results.All(r => r.Success), Is.True);
        }

        [Test]
        public async Task RunAllBenchmarksAsync_WhenSomeFail_ReturnsAllResults()
        {
            // Arrange
            _fakeEmbeddingService.IsAvailable = false;
            _fakeVectorDatabaseService.IsConnected = false;
            _fakeSimilarityService.IsAvailable = false;

            // Act
            var results = await _benchmarkService.RunAllBenchmarksAsync();

            // Assert
            Assert.That(results.Count, Is.EqualTo(3));
            Assert.That(results.All(r => !r.Success), Is.True);
        }

        #endregion

        #region Result Formatting Tests

        [Test]
        public async Task BenchmarkEmbeddingAsync_FormatsResultCorrectly()
        {
            // Arrange
            _fakeEmbeddingService.IsAvailable = true;
            _fakeEmbeddingService.Embedding = new float[768];

            // Act
            var result = await _benchmarkService.BenchmarkEmbeddingAsync();

            // Assert
            Assert.That(result.BenchmarkName, Is.Not.Empty);
            Assert.That(result.Iterations, Is.GreaterThan(0));
            Assert.That(result.ExecutedAt, Is.Not.EqualTo(default(DateTime)));
            Assert.That(result.AdditionalMetrics, Is.Not.Null);
        }

        [Test]
        public async Task BenchmarkVectorOperationsAsync_FormatsResultCorrectly()
        {
            // Arrange
            _fakeEmbeddingService.IsAvailable = true;
            _fakeEmbeddingService.Embedding = new float[768];
            _fakeVectorDatabaseService.IsConnected = true;
            _fakeVectorDatabaseService.StoreSuccess = true;

            // Act
            var result = await _benchmarkService.BenchmarkVectorOperationsAsync();

            // Assert
            Assert.That(result.BenchmarkName, Is.Not.Empty);
            Assert.That(result.Iterations, Is.GreaterThan(0));
            Assert.That(result.ExecutedAt, Is.Not.EqualTo(default(DateTime)));
            Assert.That(result.AdditionalMetrics, Is.Not.Null);
        }

        [Test]
        public async Task BenchmarkSimilarityAsync_FormatsResultCorrectly()
        {
            // Arrange
            _fakeSimilarityService.IsAvailable = true;
            _fakeSimilarityService.SimilarityResult = new SimilarityResult { Matches = new List<SimilarityMatch>() };

            // Act
            var result = await _benchmarkService.BenchmarkSimilarityAsync();

            // Assert
            Assert.That(result.BenchmarkName, Is.Not.Empty);
            Assert.That(result.Iterations, Is.GreaterThan(0));
            Assert.That(result.ExecutedAt, Is.Not.EqualTo(default(DateTime)));
            Assert.That(result.AdditionalMetrics, Is.Not.Null);
        }

        #endregion

        #region Resource Cleanup Tests

        [Test]
        public async Task BenchmarkVectorOperationsAsync_CleansUpEvenWhenDeleteFails()
        {
            // Arrange
            _fakeEmbeddingService.IsAvailable = true;
            _fakeEmbeddingService.Embedding = new float[768];
            _fakeVectorDatabaseService.IsConnected = true;
            _fakeVectorDatabaseService.StoreSuccess = true;
            _fakeVectorDatabaseService.DeleteThrowsException = true;

            // Act
            var result = await _benchmarkService.BenchmarkVectorOperationsAsync();

            // Assert
            Assert.That(result.Success, Is.True);
            Assert.That(_fakeVectorDatabaseService.DeleteAttempts, Is.EqualTo(10));
        }

        #endregion

        #region Fake Service Implementations

        private class FakeEmbeddingService : IEmbeddingService
        {
            public bool IsAvailable { get; set; }
            public float[]? Embedding { get; set; }
            public Exception? ThrowException { get; set; }

            public async Task<bool> IsEmbeddingServiceAvailableAsync()
            {
                await Task.Delay(1);
                if (ThrowException != null)
                    throw ThrowException;
                return IsAvailable;
            }

            public string GetEmbeddingModelName() => "nomic-embed-text";

            public async Task<float[]> GenerateEmbeddingAsync(string text)
            {
                await Task.Delay(1);
                if (ThrowException != null)
                    throw ThrowException;
                return Embedding ?? Array.Empty<float>();
            }
        }

        private class FakeVectorDatabaseService : IVectorDatabaseService
        {
            public bool IsConnected { get; set; }
            public bool StoreSuccess { get; set; }
            public bool DeleteThrowsException { get; set; }
            public List<string> DeletedIds { get; } = new List<string>();
            public int DeleteAttempts { get; set; }
            public Exception? ThrowException { get; set; }

            public async Task<bool> IsConnectedAsync()
            {
                await Task.Delay(1);
                if (ThrowException != null)
                    throw ThrowException;
                return IsConnected;
            }

            public async Task<bool> StoreEmbeddingAsync(string id, float[] embedding, string jsonContent, string description, Dictionary<string, string>? metadata = null)
            {
                await Task.Delay(1);
                return StoreSuccess;
            }

            public async Task<bool> DeleteEmbeddingAsync(string id)
            {
                await Task.Delay(1);
                DeleteAttempts++;
                if (DeleteThrowsException)
                    throw new Exception("Delete failed");
                DeletedIds.Add(id);
                return true;
            }

            public Task<bool> InitializeAsync() => throw new NotImplementedException();
            public Task<List<SimilarityMatch>> FindSimilarAsync(float[] queryEmbedding, int maxResults = 5, float threshold = 0.7f)
                => throw new NotImplementedException();
            public Task<bool> EmbeddingExistsAsync(string id) => throw new NotImplementedException();
            public Task<int> DeleteAllEmbeddingsAsync() => throw new NotImplementedException();
            public Task<List<string>> GetAllEmbeddingIdsAsync() => throw new NotImplementedException();
            public Task<long> GetEmbeddingCountAsync() => throw new NotImplementedException();
            public Task DisposeAsync() => throw new NotImplementedException();
        }

        private class FakeSimilarityService : ISimilarityService
        {
            public bool IsAvailable { get; set; }
            public SimilarityResult? SimilarityResult { get; set; }
            public Exception? ThrowException { get; set; }

            public async Task<bool> IsAvailableAsync()
            {
                await Task.Delay(1);
                if (ThrowException != null)
                    throw ThrowException;
                return IsAvailable;
            }

            public async Task<SimilarityResult> FindSimilarJsonAsync(string inputJson)
            {
                await Task.Delay(1);
                if (ThrowException != null)
                    throw ThrowException;
                return SimilarityResult!;
            }

            public float CalculateCosineSimilarity(float[] vector1, float[] vector2)
                => throw new NotImplementedException();

            public SimilarityConfiguration GetConfiguration() => new SimilarityConfiguration
            {
                Threshold = 0.7f,
                MaxResults = 5,
                IsEnabled = true,
                EmbeddingModel = "nomic-embed-text",
                KnowledgeBaseSize = 10
            };
        }

        #endregion
    }
}