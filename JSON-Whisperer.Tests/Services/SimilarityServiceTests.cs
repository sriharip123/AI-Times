using NUnit.Framework;
using Microsoft.Extensions.Logging;
using JSON_Whisperer.Services;
using JSON_Whisperer.Models;
using JSON_Whisperer.Interfaces;

namespace JSON_Whisperer.Tests.Services
{
    [TestFixture]
    public class SimilarityServiceTests
    {
        private ILogger<SimilarityService> _logger;
        private MockEmbeddingService _mockEmbeddingService;
        private MockVectorDatabaseService _mockVectorDatabaseService;
        private AppSettings _appSettings;
        private SimilarityService _similarityService;

        [SetUp]
        public void Setup()
        {
            _logger = new TestLogger<SimilarityService>();
            _mockEmbeddingService = new MockEmbeddingService();
            _mockVectorDatabaseService = new MockVectorDatabaseService();
            
            _appSettings = new AppSettings
            {
                Vector = new VectorSettings
                {
                    EnableSimilarityMatching = true,
                    SimilarityThreshold = 0.7f,
                    MaxSimilarResults = 5
                },
                Application = new ApplicationSettings
                {
                    VerboseMode = false
                }
            };

            _similarityService = new SimilarityService(
                _logger,
                _mockEmbeddingService,
                _mockVectorDatabaseService,
                _appSettings);
        }

        [Test]
        public async Task FindSimilarJsonAsync_ValidInput_ReturnsMatches()
        {
            // Arrange
            var inputJson = """{"name": "test", "value": 123}""";
            var queryEmbedding = new float[] { 0.1f, 0.2f, 0.3f };
            var expectedMatches = new List<SimilarityMatch>
            {
                new SimilarityMatch
                {
                    Id = "match1",
                    JsonContent = """{"name": "example", "value": 456}""",
                    Description = "Example JSON",
                    SimilarityScore = 0.85f
                }
            };

            _mockEmbeddingService.SetupEmbedding(inputJson, queryEmbedding);
            _mockVectorDatabaseService.SetupSimilarMatches(queryEmbedding, expectedMatches);

            // Act
            var result = await _similarityService.FindSimilarJsonAsync(inputJson);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Matches.Count, Is.EqualTo(1));
            Assert.That(result.Matches[0].Id, Is.EqualTo("match1"));
            Assert.That(result.HighestScore, Is.EqualTo(0.85f));
            Assert.That(result.TotalMatches, Is.EqualTo(1));
            Assert.That(result.ThresholdUsed, Is.EqualTo(0.7f));
        }

        [Test]
        public async Task FindSimilarJsonAsync_EmptyInput_ReturnsEmptyResult()
        {
            // Act
            var result = await _similarityService.FindSimilarJsonAsync("");

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Matches, Is.Empty);
            Assert.That(result.TotalMatches, Is.EqualTo(0));
            Assert.That(result.HighestScore, Is.EqualTo(0.0f));
        }

        [Test]
        public async Task FindSimilarJsonAsync_SimilarityDisabled_ReturnsEmptyResult()
        {
            // Arrange
            _appSettings.Vector.EnableSimilarityMatching = false;
            var inputJson = """{"name": "test"}""";

            // Act
            var result = await _similarityService.FindSimilarJsonAsync(inputJson);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Matches, Is.Empty);
        }

        [Test]
        public async Task FindSimilarJsonAsync_EmbeddingServiceUnavailable_ReturnsEmptyResult()
        {
            // Arrange
            var inputJson = """{"name": "test"}""";
            _mockEmbeddingService.SetAvailable(false);

            // Act
            var result = await _similarityService.FindSimilarJsonAsync(inputJson);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Matches, Is.Empty);
        }

        [Test]
        public void CalculateCosineSimilarity_IdenticalVectors_ReturnsOne()
        {
            // Arrange
            var vector1 = new float[] { 1.0f, 2.0f, 3.0f };
            var vector2 = new float[] { 1.0f, 2.0f, 3.0f };

            // Act
            var result = _similarityService.CalculateCosineSimilarity(vector1, vector2);

            // Assert
            Assert.That(result, Is.EqualTo(1.0f).Within(0.001f));
        }

        [Test]
        public void CalculateCosineSimilarity_OrthogonalVectors_ReturnsZero()
        {
            // Arrange
            var vector1 = new float[] { 1.0f, 0.0f };
            var vector2 = new float[] { 0.0f, 1.0f };

            // Act
            var result = _similarityService.CalculateCosineSimilarity(vector1, vector2);

            // Assert
            Assert.That(result, Is.EqualTo(0.0f).Within(0.001f));
        }

        [Test]
        public void CalculateCosineSimilarity_DifferentLengths_ThrowsArgumentException()
        {
            // Arrange
            var vector1 = new float[] { 1.0f, 2.0f };
            var vector2 = new float[] { 1.0f, 2.0f, 3.0f };

            // Act & Assert
            Assert.Throws<ArgumentException>(() => 
                _similarityService.CalculateCosineSimilarity(vector1, vector2));
        }

        [Test]
        public void CalculateCosineSimilarity_NullVectors_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => 
                _similarityService.CalculateCosineSimilarity(null, new float[] { 1.0f }));
            
            Assert.Throws<ArgumentNullException>(() => 
                _similarityService.CalculateCosineSimilarity(new float[] { 1.0f }, null));
        }

        [Test]
        public void CalculateCosineSimilarity_ZeroVectors_ReturnsZero()
        {
            // Arrange
            var vector1 = new float[] { 0.0f, 0.0f, 0.0f };
            var vector2 = new float[] { 1.0f, 2.0f, 3.0f };

            // Act
            var result = _similarityService.CalculateCosineSimilarity(vector1, vector2);

            // Assert
            Assert.That(result, Is.EqualTo(0.0f));
        }

        [Test]
        public async Task IsAvailableAsync_AllServicesAvailable_ReturnsTrue()
        {
            // Arrange
            _mockEmbeddingService.SetAvailable(true);
            _mockVectorDatabaseService.SetConnected(true);

            // Act
            var result = await _similarityService.IsAvailableAsync();

            // Assert
            Assert.That(result, Is.True);
        }

        [Test]
        public async Task IsAvailableAsync_EmbeddingServiceUnavailable_ReturnsFalse()
        {
            // Arrange
            _mockEmbeddingService.SetAvailable(false);
            _mockVectorDatabaseService.SetConnected(true);

            // Act
            var result = await _similarityService.IsAvailableAsync();

            // Assert
            Assert.That(result, Is.False);
        }

        [Test]
        public void GetConfiguration_ReturnsCurrentSettings()
        {
            // Arrange
            _mockVectorDatabaseService.SetEmbeddingCount(100);

            // Act
            var config = _similarityService.GetConfiguration();

            // Assert
            Assert.That(config, Is.Not.Null);
            Assert.That(config.Threshold, Is.EqualTo(0.7f));
            Assert.That(config.MaxResults, Is.EqualTo(5));
            Assert.That(config.IsEnabled, Is.True);
            Assert.That(config.KnowledgeBaseSize, Is.EqualTo(100));
        }
    }

    // Mock implementations for testing
    public class MockEmbeddingService : IEmbeddingService
    {
        private readonly Dictionary<string, float[]> _embeddings = new();
        private bool _isAvailable = true;

        public void SetupEmbedding(string input, float[] embedding)
        {
            _embeddings[input] = embedding;
        }

        public void SetAvailable(bool available)
        {
            _isAvailable = available;
        }

        public Task<float[]> GenerateEmbeddingAsync(string jsonContent)
        {
            if (_embeddings.TryGetValue(jsonContent, out var embedding))
            {
                return Task.FromResult(embedding);
            }
            throw new InvalidOperationException("No embedding configured for input");
        }

        public Task<bool> IsEmbeddingServiceAvailableAsync()
        {
            return Task.FromResult(_isAvailable);
        }

        public string GetEmbeddingModelName()
        {
            return "test-model";
        }
    }

    public class MockVectorDatabaseService : IVectorDatabaseService
    {
        private readonly Dictionary<float[], List<SimilarityMatch>> _similarMatches = new();
        private readonly Dictionary<string, bool> _existingEmbeddings = new();
        private bool _isConnected = true;
        private long _embeddingCount = 0;

        public List<VectorEmbedding> StoredEmbeddings { get; } = new();

        public void SetupSimilarMatches(float[] queryEmbedding, List<SimilarityMatch> matches)
        {
            _similarMatches[queryEmbedding] = matches;
        }

        public void SetConnected(bool connected)
        {
            _isConnected = connected;
        }

        public void SetEmbeddingCount(long count)
        {
            _embeddingCount = count;
        }

        public void SetEmbeddingExists(string idPrefix, bool exists)
        {
            _existingEmbeddings[idPrefix] = exists;
        }

        public Task<bool> InitializeAsync() => Task.FromResult(true);

        public Task<bool> StoreEmbeddingAsync(string id, float[] embedding, string jsonContent, string description, Dictionary<string, string>? metadata = null)
        {
            StoredEmbeddings.Add(new VectorEmbedding
            {
                Id = id,
                Embedding = embedding,
                JsonContent = jsonContent,
                Description = description,
                CreatedAt = DateTime.UtcNow
            });
            return Task.FromResult(true);
        }

        public Task<List<SimilarityMatch>> FindSimilarAsync(float[] queryEmbedding, int maxResults = 5, float threshold = 0.7f)
        {
            if (_similarMatches.TryGetValue(queryEmbedding, out var matches))
            {
                return Task.FromResult(matches);
            }
            return Task.FromResult(new List<SimilarityMatch>());
        }

        public Task<bool> IsConnectedAsync() => Task.FromResult(_isConnected);

        public Task<long> GetEmbeddingCountAsync() => Task.FromResult(_embeddingCount);

        public Task<bool> EmbeddingExistsAsync(string id)
        {
            var exists = _existingEmbeddings.Any(kvp => id.StartsWith(kvp.Key) && kvp.Value);
            return Task.FromResult(exists);
        }

        public Task<bool> DeleteEmbeddingAsync(string id) => Task.FromResult(true);

        public Task DisposeAsync() => Task.CompletedTask;
    }
}