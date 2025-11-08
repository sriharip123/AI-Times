using NUnit.Framework;
using Microsoft.Extensions.Logging;
using JSON_Whisperer.Services;
using JSON_Whisperer.Models;

namespace JSON_Whisperer.Tests.Services
{
    [TestFixture]
    public class ScyllaDbVectorServiceTests
    {
        private ILogger<ScyllaDbVectorService> _logger;
        private AppSettings _appSettings;
        private ScyllaDbVectorService _vectorService;

        [SetUp]
        public void Setup()
        {
            _logger = new TestLogger<ScyllaDbVectorService>();
            _appSettings = new AppSettings
            {
                ScyllaDb = new ScyllaDbSettings
                {
                    ContactPoints = "127.0.0.1",
                    Port = 9042,
                    Keyspace = "test_keyspace",
                    ConnectionTimeoutSeconds = 5,
                    QueryTimeoutSeconds = 10,
                    CreateKeyspaceIfNotExists = true
                }
            };

            _vectorService = new ScyllaDbVectorService(_logger, _appSettings);
        }

        [Test]
        public void Constructor_ValidParameters_DoesNotThrow()
        {
            // Act & Assert
            Assert.DoesNotThrow(() => new ScyllaDbVectorService(_logger, _appSettings));
        }

        [Test]
        public void Constructor_NullLogger_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => 
                new ScyllaDbVectorService(null, _appSettings));
        }

        [Test]
        public void Constructor_NullAppSettings_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => 
                new ScyllaDbVectorService(_logger, null));
        }

        [Test]
        public async Task InitializeAsync_WithoutDatabase_ReturnsFalse()
        {
            // Note: This test will fail to connect to ScyllaDB since we don't have one running
            // This is expected behavior and tests the error handling path
            
            // Act
            var result = await _vectorService.InitializeAsync();

            // Assert
            Assert.That(result, Is.False);
        }

        [Test]
        public async Task IsConnectedAsync_WithoutInitialization_ReturnsFalse()
        {
            // Act
            var result = await _vectorService.IsConnectedAsync();

            // Assert
            Assert.That(result, Is.False);
        }

        [Test]
        public async Task StoreEmbeddingAsync_WithoutInitialization_ReturnsFalse()
        {
            // Arrange
            var embedding = new float[] { 0.1f, 0.2f, 0.3f };
            var jsonContent = """{"test": "data"}""";
            var description = "Test embedding";

            // Act
            var result = await _vectorService.StoreEmbeddingAsync("test-id", embedding, jsonContent, description);

            // Assert
            Assert.That(result, Is.False);
        }

        [Test]
        public async Task FindSimilarAsync_WithoutInitialization_ReturnsEmptyList()
        {
            // Arrange
            var queryEmbedding = new float[] { 0.1f, 0.2f, 0.3f };

            // Act
            var result = await _vectorService.FindSimilarAsync(queryEmbedding);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result, Is.Empty);
        }

        [Test]
        public async Task GetEmbeddingCountAsync_WithoutInitialization_ReturnsZero()
        {
            // Act
            var result = await _vectorService.GetEmbeddingCountAsync();

            // Assert
            Assert.That(result, Is.EqualTo(0));
        }

        [Test]
        public async Task EmbeddingExistsAsync_WithoutInitialization_ReturnsFalse()
        {
            // Act
            var result = await _vectorService.EmbeddingExistsAsync("test-id");

            // Assert
            Assert.That(result, Is.False);
        }

        [Test]
        public async Task DeleteEmbeddingAsync_WithoutInitialization_ReturnsFalse()
        {
            // Act
            var result = await _vectorService.DeleteEmbeddingAsync("test-id");

            // Assert
            Assert.That(result, Is.False);
        }

        [Test]
        public async Task DisposeAsync_DoesNotThrow()
        {
            // Act & Assert
            Assert.DoesNotThrowAsync(async () => await _vectorService.DisposeAsync());
        }

        [Test]
        public void AppSettings_Validation_ValidSettings_DoesNotThrow()
        {
            // Act & Assert
            Assert.DoesNotThrow(() => _appSettings.ScyllaDb.Validate());
        }

        [Test]
        public void AppSettings_Validation_EmptyContactPoints_ThrowsArgumentException()
        {
            // Arrange
            _appSettings.ScyllaDb.ContactPoints = "";

            // Act & Assert
            Assert.Throws<ArgumentException>(() => _appSettings.ScyllaDb.Validate());
        }

        [Test]
        public void AppSettings_Validation_EmptyKeyspace_ThrowsArgumentException()
        {
            // Arrange
            _appSettings.ScyllaDb.Keyspace = "";

            // Act & Assert
            Assert.Throws<ArgumentException>(() => _appSettings.ScyllaDb.Validate());
        }

        [Test]
        public void AppSettings_Validation_InvalidPort_ThrowsArgumentException()
        {
            // Arrange
            _appSettings.ScyllaDb.Port = 0;

            // Act & Assert
            Assert.Throws<ArgumentException>(() => _appSettings.ScyllaDb.Validate());
        }

        // Test the cosine similarity calculation using reflection since it's private
        [Test]
        public void CosineSimilarity_IdenticalVectors_ReturnsOne()
        {
            // Arrange
            var vector1 = new float[] { 1.0f, 2.0f, 3.0f };
            var vector2 = new float[] { 1.0f, 2.0f, 3.0f };

            // Act - Using reflection to test private method
            var method = typeof(ScyllaDbVectorService).GetMethod("CalculateCosineSimilarity", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            var result = (float)method.Invoke(null, new object[] { vector1, vector2 });

            // Assert
            Assert.That(result, Is.EqualTo(1.0f).Within(0.001f));
        }

        [Test]
        public void CosineSimilarity_OrthogonalVectors_ReturnsZero()
        {
            // Arrange
            var vector1 = new float[] { 1.0f, 0.0f };
            var vector2 = new float[] { 0.0f, 1.0f };

            // Act - Using reflection to test private method
            var method = typeof(ScyllaDbVectorService).GetMethod("CalculateCosineSimilarity", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            var result = (float)method.Invoke(null, new object[] { vector1, vector2 });

            // Assert
            Assert.That(result, Is.EqualTo(0.0f).Within(0.001f));
        }

        [Test]
        public void CosineSimilarity_DifferentLengths_ThrowsArgumentException()
        {
            // Arrange
            var vector1 = new float[] { 1.0f, 2.0f };
            var vector2 = new float[] { 1.0f, 2.0f, 3.0f };

            // Act & Assert - Using reflection to test private method
            var method = typeof(ScyllaDbVectorService).GetMethod("CalculateCosineSimilarity", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            
            Assert.Throws<System.Reflection.TargetInvocationException>(() => 
                method.Invoke(null, new object[] { vector1, vector2 }));
        }
    }
}