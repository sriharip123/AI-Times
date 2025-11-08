using NUnit.Framework;
using Microsoft.Extensions.Logging;
using JSON_Whisperer.Services;
using JSON_Whisperer.Models;
using JSON_Whisperer.Interfaces;
using JSON_Whisperer.Tests;
using System.Text.Json;

namespace JSON_Whisperer.Tests.Services
{
    [TestFixture]
    public class KnowledgeBaseServiceTests
    {
        private ILogger<KnowledgeBaseService> _logger;
        private JSON_Whisperer.Tests.Services.MockEmbeddingService _mockEmbeddingService;
        private JSON_Whisperer.Tests.Services.MockVectorDatabaseService _mockVectorDatabaseService;
        private AppSettings _appSettings;
        private KnowledgeBaseService _knowledgeBaseService;
        private string _testAppDataPath;

        [SetUp]
        public void Setup()
        {
            _logger = new TestLogger<KnowledgeBaseService>();
            _mockEmbeddingService = new JSON_Whisperer.Tests.Services.MockEmbeddingService();
            _mockVectorDatabaseService = new JSON_Whisperer.Tests.Services.MockVectorDatabaseService();
            
            // Create temporary test directory
            _testAppDataPath = Path.Combine(Path.GetTempPath(), "KnowledgeBaseTests", Guid.NewGuid().ToString());
            Directory.CreateDirectory(_testAppDataPath);

            _appSettings = new AppSettings
            {
                Vector = new VectorSettings
                {
                    AppDataPath = _testAppDataPath,
                    EnableSimilarityMatching = true,
                    SimilarityThreshold = 0.7f,
                    MaxSimilarResults = 5
                }
            };

            _knowledgeBaseService = new KnowledgeBaseService(
                _logger,
                _mockEmbeddingService,
                _mockVectorDatabaseService,
                _appSettings);
        }

        [TearDown]
        public void TearDown()
        {
            // Clean up test directory
            if (Directory.Exists(_testAppDataPath))
            {
                Directory.Delete(_testAppDataPath, true);
            }
        }

        [Test]
        public async Task LoadExamplesAsync_EmptyDirectory_ReturnsEmptyList()
        {
            // Act
            var result = await _knowledgeBaseService.LoadExamplesAsync();

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result, Is.Empty);
        }

        [Test]
        public async Task LoadExamplesAsync_NonExistentDirectory_ReturnsEmptyList()
        {
            // Arrange
            _appSettings.Vector.AppDataPath = Path.Combine(Path.GetTempPath(), "NonExistentDirectory");

            // Act
            var result = await _knowledgeBaseService.LoadExamplesAsync();

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result, Is.Empty);
        }

        [Test]
        public async Task LoadExamplesAsync_ValidJsonFiles_ReturnsExamples()
        {
            // Arrange
            var testJson1 = "{\"name\": \"John\", \"age\": 30, \"_description\": \"User profile data\"}";
            var testJson2 = "{\"product\": \"Widget\", \"price\": 19.99}";
            
            var file1Path = Path.Combine(_testAppDataPath, "user_profile.json");
            var file2Path = Path.Combine(_testAppDataPath, "product-data.json");
            
            await File.WriteAllTextAsync(file1Path, testJson1);
            await File.WriteAllTextAsync(file2Path, testJson2);

            // Act
            var result = await _knowledgeBaseService.LoadExamplesAsync();

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Count, Is.EqualTo(2));
            
            var userExample = result.FirstOrDefault(e => e.FilePath == file1Path);
            Assert.That(userExample, Is.Not.Null);
            Assert.That(userExample.JsonContent, Is.EqualTo(testJson1));
            Assert.That(userExample.Description, Contains.Substring("User profile data"));
            Assert.That(userExample.Id, Is.Not.Null.And.Not.Empty);

            var productExample = result.FirstOrDefault(e => e.FilePath == file2Path);
            Assert.That(productExample, Is.Not.Null);
            Assert.That(productExample.JsonContent, Is.EqualTo(testJson2));
            Assert.That(productExample.Description, Contains.Substring("Product Data"));
        }

        [Test]
        public async Task LoadExamplesAsync_InvalidJsonFile_SkipsInvalidFile()
        {
            // Arrange
            var validJson = "{\"name\": \"test\"}";
            var invalidJson = "{invalid json}";
            
            var validFilePath = Path.Combine(_testAppDataPath, "valid.json");
            var invalidFilePath = Path.Combine(_testAppDataPath, "invalid.json");
            
            await File.WriteAllTextAsync(validFilePath, validJson);
            await File.WriteAllTextAsync(invalidFilePath, invalidJson);

            // Act
            var result = await _knowledgeBaseService.LoadExamplesAsync();

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Count, Is.EqualTo(1));
            Assert.That(result[0].FilePath, Is.EqualTo(validFilePath));
        }

        [Test]
        public async Task LoadExamplesAsync_NestedDirectories_LoadsAllJsonFiles()
        {
            // Arrange
            var subDir = Path.Combine(_testAppDataPath, "users");
            Directory.CreateDirectory(subDir);
            
            var json1 = "{\"type\": \"admin\"}";
            var json2 = "{\"type\": \"user\"}";
            
            var file1Path = Path.Combine(_testAppDataPath, "config.json");
            var file2Path = Path.Combine(subDir, "user.json");
            
            await File.WriteAllTextAsync(file1Path, json1);
            await File.WriteAllTextAsync(file2Path, json2);

            // Act
            var result = await _knowledgeBaseService.LoadExamplesAsync();

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Count, Is.EqualTo(2));
            
            var configExample = result.FirstOrDefault(e => e.FilePath == file1Path);
            var userExample = result.FirstOrDefault(e => e.FilePath == file2Path);
            
            Assert.That(configExample, Is.Not.Null);
            Assert.That(userExample, Is.Not.Null);
            Assert.That(userExample.Description, Contains.Substring("Users"));
        }

        [Test]
        public async Task LoadExamplesAsync_JsonWithMetadata_ExtractsDescription()
        {
            // Arrange
            var jsonWithMetadata = @"{
                ""data"": {""value"": 123},
                ""_metadata"": {
                    ""description"": ""Test data with metadata"",
                    ""version"": ""1.0""
                }
            }";
            
            var filePath = Path.Combine(_testAppDataPath, "metadata_test.json");
            await File.WriteAllTextAsync(filePath, jsonWithMetadata);

            // Act
            var result = await _knowledgeBaseService.LoadExamplesAsync();

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Count, Is.EqualTo(1));
            Assert.That(result[0].Description, Contains.Substring("Test data with metadata"));
        }

        [Test]
        public async Task InitializeVectorDatabaseAsync_DatabaseNotConnected_SkipsInitialization()
        {
            // Arrange
            _mockVectorDatabaseService.SetConnected(false);

            // Act & Assert
            Assert.DoesNotThrowAsync(async () => await _knowledgeBaseService.InitializeVectorDatabaseAsync());
        }

        [Test]
        public async Task InitializeVectorDatabaseAsync_EmbeddingServiceUnavailable_SkipsInitialization()
        {
            // Arrange
            _mockVectorDatabaseService.SetConnected(true);
            _mockEmbeddingService.SetAvailable(false);

            // Act & Assert
            Assert.DoesNotThrowAsync(async () => await _knowledgeBaseService.InitializeVectorDatabaseAsync());
        }

        [Test]
        public async Task InitializeVectorDatabaseAsync_NoExamples_CompletesSuccessfully()
        {
            // Arrange
            _mockVectorDatabaseService.SetConnected(true);
            _mockEmbeddingService.SetAvailable(true);

            // Act & Assert
            Assert.DoesNotThrowAsync(async () => await _knowledgeBaseService.InitializeVectorDatabaseAsync());
        }

        [Test]
        public async Task InitializeVectorDatabaseAsync_WithExamples_StoresEmbeddings()
        {
            // Arrange
            var testJson = "{\"name\": \"test\", \"value\": 123}";
            var filePath = Path.Combine(_testAppDataPath, "test.json");
            await File.WriteAllTextAsync(filePath, testJson);

            var testEmbedding = new float[] { 0.1f, 0.2f, 0.3f };
            
            _mockVectorDatabaseService.SetConnected(true);
            _mockEmbeddingService.SetAvailable(true);
            _mockEmbeddingService.SetupEmbedding(testJson, testEmbedding);

            // Act
            await _knowledgeBaseService.InitializeVectorDatabaseAsync();

            // Assert
            Assert.That(_mockVectorDatabaseService.StoredEmbeddings.Count, Is.EqualTo(1));
            var storedEmbedding = _mockVectorDatabaseService.StoredEmbeddings.First();
            Assert.That(storedEmbedding.JsonContent, Is.EqualTo(testJson));
            Assert.That(storedEmbedding.Embedding, Is.EqualTo(testEmbedding));
        }

        [Test]
        public async Task InitializeVectorDatabaseAsync_ExistingEmbedding_SkipsExisting()
        {
            // Arrange
            var testJson = "{\"name\": \"test\"}";
            var filePath = Path.Combine(_testAppDataPath, "test.json");
            await File.WriteAllTextAsync(filePath, testJson);

            _mockVectorDatabaseService.SetConnected(true);
            _mockEmbeddingService.SetAvailable(true);
            
            // Simulate existing embedding
            _mockVectorDatabaseService.SetEmbeddingExists("test.json_", true);

            // Act
            await _knowledgeBaseService.InitializeVectorDatabaseAsync();

            // Assert
            Assert.That(_mockVectorDatabaseService.StoredEmbeddings.Count, Is.EqualTo(0));
        }

        [Test]
        public async Task InitializeVectorDatabaseAsync_EmbeddingGenerationFails_ContinuesWithOthers()
        {
            // Arrange
            var testJson1 = "{\"name\": \"test1\"}";
            var testJson2 = "{\"name\": \"test2\"}";
            
            var file1Path = Path.Combine(_testAppDataPath, "test1.json");
            var file2Path = Path.Combine(_testAppDataPath, "test2.json");
            
            await File.WriteAllTextAsync(file1Path, testJson1);
            await File.WriteAllTextAsync(file2Path, testJson2);

            var testEmbedding = new float[] { 0.1f, 0.2f, 0.3f };
            
            _mockVectorDatabaseService.SetConnected(true);
            _mockEmbeddingService.SetAvailable(true);
            
            // Setup embedding to fail for first file, succeed for second
            _mockEmbeddingService.SetupEmbedding(testJson2, testEmbedding);

            // Act
            await _knowledgeBaseService.InitializeVectorDatabaseAsync();

            // Assert
            Assert.That(_mockVectorDatabaseService.StoredEmbeddings.Count, Is.EqualTo(1));
            Assert.That(_mockVectorDatabaseService.StoredEmbeddings[0].JsonContent, Is.EqualTo(testJson2));
        }

        [Test]
        public void Constructor_NullLogger_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new KnowledgeBaseService(
                null, _mockEmbeddingService, _mockVectorDatabaseService, _appSettings));
        }

        [Test]
        public void Constructor_NullEmbeddingService_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new KnowledgeBaseService(
                _logger, null, _mockVectorDatabaseService, _appSettings));
        }

        [Test]
        public void Constructor_NullVectorDatabaseService_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new KnowledgeBaseService(
                _logger, _mockEmbeddingService, null, _appSettings));
        }

        [Test]
        public void Constructor_NullAppSettings_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new KnowledgeBaseService(
                _logger, _mockEmbeddingService, _mockVectorDatabaseService, null));
        }
    }


}