using JSON_Whisperer.Interfaces;
using JSON_Whisperer.Models;
using JSON_Whisperer.Services;
using Microsoft.Extensions.Logging;
using NUnit.Framework;

namespace JSON_Whisperer.Tests.Services
{
    [TestFixture]
    public class KnowledgeBaseManagementServiceTests
    {
        private TestLogger<KnowledgeBaseManagementService> _logger = null!;
        private FakeVectorDatabaseService _fakeVectorDatabaseService = null!;
        private FakeEmbeddingService _fakeEmbeddingService = null!;
        private FakeKnowledgeBaseService _fakeKnowledgeBaseService = null!;
        private AppSettings _appSettings = null!;
        private KnowledgeBaseManagementService _service = null!;
        private string _testAppDataPath = null!;

        [SetUp]
        public void Setup()
        {
            _logger = new TestLogger<KnowledgeBaseManagementService>();
            _fakeVectorDatabaseService = new FakeVectorDatabaseService();
            _fakeEmbeddingService = new FakeEmbeddingService();
            _fakeKnowledgeBaseService = new FakeKnowledgeBaseService();

            _testAppDataPath = Path.Combine(Path.GetTempPath(), "KBMgmtTests_" + Guid.NewGuid().ToString());
            Directory.CreateDirectory(_testAppDataPath);

            _appSettings = new AppSettings
            {
                Vector = new VectorSettings
                {
                    AppDataPath = _testAppDataPath
                }
            };

            _service = new KnowledgeBaseManagementService(
                _logger,
                _fakeVectorDatabaseService,
                _fakeEmbeddingService,
                _fakeKnowledgeBaseService,
                _appSettings
            );
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(_testAppDataPath))
            {
                Directory.Delete(_testAppDataPath, true);
            }
        }

        #region ReinitializeAsync Tests

        [Test]
        public async Task ReinitializeAsync_WhenVectorDatabaseNotConnected_ReturnsErrorResult()
        {
            // Arrange
            _fakeVectorDatabaseService.IsConnected = false;

            // Act
            var result = await _service.ReinitializeAsync();

            // Assert
            Assert.That(result.Errors, Is.GreaterThan(0));
            Assert.That(result.ErrorMessages, Has.Count.GreaterThan(0));
            Assert.That(result.ErrorMessages[0], Does.Contain("not connected"));
            Assert.That(result.Success, Is.False);
            Assert.That(result.Duration, Is.GreaterThan(TimeSpan.Zero));
        }

        [Test]
        public async Task ReinitializeAsync_WhenEmbeddingServiceNotAvailable_ReturnsErrorResult()
        {
            // Arrange
            _fakeVectorDatabaseService.IsConnected = true;
            _fakeEmbeddingService.IsAvailable = false;

            // Act
            var result = await _service.ReinitializeAsync();

            // Assert
            Assert.That(result.Errors, Is.GreaterThan(0));
            Assert.That(result.ErrorMessages, Has.Count.GreaterThan(0));
            Assert.That(result.ErrorMessages[0], Does.Contain("not available"));
            Assert.That(result.Success, Is.False);
        }

        [Test]
        public async Task ReinitializeAsync_WhenNoJsonFilesFound_ReturnsWarningResult()
        {
            // Arrange
            _fakeVectorDatabaseService.IsConnected = true;
            _fakeEmbeddingService.IsAvailable = true;
            _fakeVectorDatabaseService.DeletedCount = 5;
            _fakeKnowledgeBaseService.Examples = new List<JsonExample>();

            // Act
            var result = await _service.ReinitializeAsync();

            // Assert
            Assert.That(result.EmbeddingsCleared, Is.EqualTo(5));
            Assert.That(result.FilesProcessed, Is.EqualTo(0));
            Assert.That(result.ErrorMessages, Has.Count.GreaterThan(0));
            Assert.That(result.ErrorMessages[0], Does.Contain("No JSON files"));
        }

        [Test]
        public async Task ReinitializeAsync_WhenSuccessful_ClearsAndCreatesEmbeddings()
        {
            // Arrange
            _fakeVectorDatabaseService.IsConnected = true;
            _fakeEmbeddingService.IsAvailable = true;
            _fakeVectorDatabaseService.DeletedCount = 10;
            _fakeVectorDatabaseService.StoreSuccess = true;
            _fakeEmbeddingService.Embedding = new float[] { 0.1f, 0.2f, 0.3f };

            var examples = new List<JsonExample>
            {
                new JsonExample
                {
                    Id = "example1",
                    FilePath = "test1.json",
                    JsonContent = "{\"test\": 1}",
                    Description = "Test 1"
                },
                new JsonExample
                {
                    Id = "example2",
                    FilePath = "test2.json",
                    JsonContent = "{\"test\": 2}",
                    Description = "Test 2"
                }
            };
            _fakeKnowledgeBaseService.Examples = examples;

            // Create test JSON files
            File.WriteAllText(Path.Combine(_testAppDataPath, "test1.json"), "{\"test\": 1}");
            File.WriteAllText(Path.Combine(_testAppDataPath, "test2.json"), "{\"test\": 2}");

            // Act
            var result = await _service.ReinitializeAsync();

            // Assert
            Assert.That(result.EmbeddingsCleared, Is.EqualTo(10));
            Assert.That(result.FilesProcessed, Is.EqualTo(2));
            Assert.That(result.EmbeddingsCreated, Is.EqualTo(2));
            Assert.That(result.Errors, Is.EqualTo(0));
            Assert.That(result.Success, Is.True);
            Assert.That(result.Duration, Is.GreaterThan(TimeSpan.Zero));
        }

        [Test]
        public async Task ReinitializeAsync_WhenEmbeddingGenerationFails_RecordsError()
        {
            // Arrange
            _fakeVectorDatabaseService.IsConnected = true;
            _fakeEmbeddingService.IsAvailable = true;
            _fakeVectorDatabaseService.DeletedCount = 0;
            _fakeEmbeddingService.Embedding = null; // Simulate failure

            var examples = new List<JsonExample>
            {
                new JsonExample
                {
                    Id = "example1",
                    FilePath = "test1.json",
                    JsonContent = "{\"test\": 1}",
                    Description = "Test 1"
                }
            };
            _fakeKnowledgeBaseService.Examples = examples;

            File.WriteAllText(Path.Combine(_testAppDataPath, "test1.json"), "{\"test\": 1}");

            // Act
            var result = await _service.ReinitializeAsync();

            // Assert
            Assert.That(result.FilesProcessed, Is.EqualTo(1));
            Assert.That(result.EmbeddingsCreated, Is.EqualTo(0));
            Assert.That(result.Errors, Is.GreaterThan(0));
            Assert.That(result.ErrorMessages, Has.Count.GreaterThan(0));
            Assert.That(result.ErrorMessages[0], Does.Contain("Failed to generate embedding"));
        }

        [Test]
        public async Task ReinitializeAsync_WhenStoringEmbeddingFails_RecordsError()
        {
            // Arrange
            _fakeVectorDatabaseService.IsConnected = true;
            _fakeEmbeddingService.IsAvailable = true;
            _fakeVectorDatabaseService.DeletedCount = 0;
            _fakeVectorDatabaseService.StoreSuccess = false;
            _fakeEmbeddingService.Embedding = new float[] { 0.1f, 0.2f, 0.3f };

            var examples = new List<JsonExample>
            {
                new JsonExample
                {
                    Id = "example1",
                    FilePath = "test1.json",
                    JsonContent = "{\"test\": 1}",
                    Description = "Test 1"
                }
            };
            _fakeKnowledgeBaseService.Examples = examples;

            File.WriteAllText(Path.Combine(_testAppDataPath, "test1.json"), "{\"test\": 1}");

            // Act
            var result = await _service.ReinitializeAsync();

            // Assert
            Assert.That(result.FilesProcessed, Is.EqualTo(1));
            Assert.That(result.EmbeddingsCreated, Is.EqualTo(0));
            Assert.That(result.Errors, Is.GreaterThan(0));
            Assert.That(result.ErrorMessages, Has.Count.GreaterThan(0));
            Assert.That(result.ErrorMessages[0], Does.Contain("Failed to store embedding"));
        }

        [Test]
        public async Task ReinitializeAsync_WhenExceptionOccurs_ReturnsErrorResult()
        {
            // Arrange
            _fakeVectorDatabaseService.IsConnected = true;
            _fakeEmbeddingService.IsAvailable = true;
            _fakeKnowledgeBaseService.ThrowException = new InvalidOperationException("Test exception");

            // Act
            var result = await _service.ReinitializeAsync();

            // Assert
            // When LoadExamplesAsync throws, it's caught and returns empty list, which triggers "No JSON files" warning
            Assert.That(result.FilesProcessed, Is.EqualTo(0));
            Assert.That(result.ErrorMessages, Has.Count.GreaterThan(0));
            Assert.That(result.ErrorMessages[0], Does.Contain("No JSON files"));
            Assert.That(result.Success, Is.False);
        }

        [Test]
        public async Task ReinitializeAsync_ProcessesMultipleExamplesCorrectly()
        {
            // Arrange
            _fakeVectorDatabaseService.IsConnected = true;
            _fakeEmbeddingService.IsAvailable = true;
            _fakeVectorDatabaseService.DeletedCount = 5;
            _fakeVectorDatabaseService.StoreSuccess = true;
            _fakeEmbeddingService.Embedding = new float[] { 0.1f, 0.2f, 0.3f };

            var examples = new List<JsonExample>();
            for (int i = 1; i <= 5; i++)
            {
                examples.Add(new JsonExample
                {
                    Id = $"example{i}",
                    FilePath = $"test{i}.json",
                    JsonContent = $"{{\"test\": {i}}}",
                    Description = $"Test {i}"
                });
                File.WriteAllText(Path.Combine(_testAppDataPath, $"test{i}.json"), $"{{\"test\": {i}}}");
            }
            _fakeKnowledgeBaseService.Examples = examples;

            // Act
            var result = await _service.ReinitializeAsync();

            // Assert
            Assert.That(result.FilesProcessed, Is.EqualTo(5));
            Assert.That(result.EmbeddingsCreated, Is.EqualTo(5));
            Assert.That(result.EmbeddingsCleared, Is.EqualTo(5));
            Assert.That(result.Errors, Is.EqualTo(0));
            Assert.That(result.Success, Is.True);
        }

        #endregion

        #region ValidateAsync Tests

        [Test]
        public async Task ValidateAsync_WhenNoJsonFiles_ReturnsWarning()
        {
            // Arrange
            // Empty directory

            // Act
            var result = await _service.ValidateAsync();

            // Assert
            Assert.That(result.TotalFiles, Is.EqualTo(0));
            Assert.That(result.Warnings, Has.Count.GreaterThan(0));
            Assert.That(result.Warnings[0], Does.Contain("No JSON files"));
            Assert.That(result.Duration, Is.GreaterThan(TimeSpan.Zero));
        }

        [Test]
        public async Task ValidateAsync_WhenValidJsonFiles_ReturnsValidResult()
        {
            // Arrange
            File.WriteAllText(Path.Combine(_testAppDataPath, "test1.json"), "{\"_description\": \"Test 1\", \"data\": 1}");
            File.WriteAllText(Path.Combine(_testAppDataPath, "test2.json"), "{\"_description\": \"Test 2\", \"data\": 2}");

            // Act
            var result = await _service.ValidateAsync();

            // Assert
            Assert.That(result.TotalFiles, Is.EqualTo(2));
            Assert.That(result.ValidFiles, Is.EqualTo(2));
            Assert.That(result.InvalidFiles, Is.EqualTo(0));
            Assert.That(result.MissingDescriptions, Is.EqualTo(0));
            Assert.That(result.IsValid, Is.True);
        }

        [Test]
        public async Task ValidateAsync_WhenInvalidJson_ReportsError()
        {
            // Arrange
            File.WriteAllText(Path.Combine(_testAppDataPath, "invalid.json"), "{invalid json}");

            // Act
            var result = await _service.ValidateAsync();

            // Assert
            Assert.That(result.TotalFiles, Is.EqualTo(1));
            Assert.That(result.ValidFiles, Is.EqualTo(0));
            Assert.That(result.InvalidFiles, Is.EqualTo(1));
            Assert.That(result.Errors, Has.Count.GreaterThan(0));
            Assert.That(result.Errors[0], Does.Contain("Invalid JSON"));
            Assert.That(result.IsValid, Is.False);
        }

        [Test]
        public async Task ValidateAsync_WhenMissingDescription_ReportsWarning()
        {
            // Arrange
            File.WriteAllText(Path.Combine(_testAppDataPath, "no_desc.json"), "{\"data\": 1}");

            // Act
            var result = await _service.ValidateAsync();

            // Assert
            Assert.That(result.TotalFiles, Is.EqualTo(1));
            Assert.That(result.ValidFiles, Is.EqualTo(1));
            Assert.That(result.MissingDescriptions, Is.EqualTo(1));
            Assert.That(result.Warnings, Has.Count.GreaterThan(0));
            Assert.That(result.Warnings[0], Does.Contain("Missing description"));
        }

        [Test]
        public async Task ValidateAsync_WhenDescriptionInMetadata_RecognizesIt()
        {
            // Arrange
            File.WriteAllText(Path.Combine(_testAppDataPath, "with_metadata.json"),
                "{\"_metadata\": {\"description\": \"Test\"}, \"data\": 1}");

            // Act
            var result = await _service.ValidateAsync();

            // Assert
            Assert.That(result.TotalFiles, Is.EqualTo(1));
            Assert.That(result.ValidFiles, Is.EqualTo(1));
            Assert.That(result.MissingDescriptions, Is.EqualTo(0));
        }

        [Test]
        public async Task ValidateAsync_WhenEmptyJson_ReportsWarning()
        {
            // Arrange
            File.WriteAllText(Path.Combine(_testAppDataPath, "empty.json"), "{}");

            // Act
            var result = await _service.ValidateAsync();

            // Assert
            Assert.That(result.TotalFiles, Is.EqualTo(1));
            Assert.That(result.ValidFiles, Is.EqualTo(1));
            Assert.That(result.Warnings, Has.Count.GreaterThan(0));
            Assert.That(result.Warnings.Any(w => w.Contains("Nearly empty")), Is.True);
        }

        [Test]
        public async Task ValidateAsync_WhenMixedValidAndInvalid_ReportsCorrectly()
        {
            // Arrange
            File.WriteAllText(Path.Combine(_testAppDataPath, "valid1.json"), "{\"_description\": \"Valid\", \"data\": 1}");
            File.WriteAllText(Path.Combine(_testAppDataPath, "invalid1.json"), "{invalid}");
            File.WriteAllText(Path.Combine(_testAppDataPath, "valid2.json"), "{\"_description\": \"Valid 2\", \"data\": 2}");
            File.WriteAllText(Path.Combine(_testAppDataPath, "no_desc.json"), "{\"data\": 3}");

            // Act
            var result = await _service.ValidateAsync();

            // Assert
            Assert.That(result.TotalFiles, Is.EqualTo(4));
            Assert.That(result.ValidFiles, Is.EqualTo(3));
            Assert.That(result.InvalidFiles, Is.EqualTo(1));
            Assert.That(result.MissingDescriptions, Is.EqualTo(1));
            Assert.That(result.Errors, Has.Count.EqualTo(1));
            Assert.That(result.Warnings, Has.Count.EqualTo(1));
        }

        [Test]
        public async Task ValidateAsync_WhenDirectoryDoesNotExist_ReturnsEmptyResult()
        {
            // Arrange
            // Delete the directory - this is handled gracefully by ScanJsonFilesAsync
            Directory.Delete(_testAppDataPath, true);

            // Act
            var result = await _service.ValidateAsync();

            // Assert
            // When directory doesn't exist, ScanJsonFilesAsync returns empty list
            Assert.That(result.TotalFiles, Is.EqualTo(0));
            Assert.That(result.Warnings, Has.Count.GreaterThan(0));
            Assert.That(result.Warnings[0], Does.Contain("No JSON files"));
        }

        #endregion

        #region ClearAllEmbeddingsAsync Tests

        [Test]
        public async Task ClearAllEmbeddingsAsync_WhenSuccessful_ReturnsCount()
        {
            // Arrange
            _fakeVectorDatabaseService.DeletedCount = 42;

            // Act
            var count = await _service.ClearAllEmbeddingsAsync();

            // Assert
            Assert.That(count, Is.EqualTo(42));
        }

        [Test]
        public async Task ClearAllEmbeddingsAsync_WhenExceptionOccurs_ReturnsZero()
        {
            // Arrange
            _fakeVectorDatabaseService.ThrowException = new InvalidOperationException("Database error");

            // Act
            var count = await _service.ClearAllEmbeddingsAsync();

            // Assert
            Assert.That(count, Is.EqualTo(0));
        }

        #endregion

        #region ScanJsonFilesAsync Tests

        [Test]
        public async Task ScanJsonFilesAsync_WhenDirectoryEmpty_ReturnsEmptyList()
        {
            // Arrange
            // Empty directory

            // Act
            var files = await _service.ScanJsonFilesAsync();

            // Assert
            Assert.That(files, Is.Empty);
        }

        [Test]
        public async Task ScanJsonFilesAsync_WhenJsonFilesExist_ReturnsFileList()
        {
            // Arrange
            File.WriteAllText(Path.Combine(_testAppDataPath, "test1.json"), "{}");
            File.WriteAllText(Path.Combine(_testAppDataPath, "test2.json"), "{}");
            File.WriteAllText(Path.Combine(_testAppDataPath, "test.txt"), "not json");

            // Act
            var files = await _service.ScanJsonFilesAsync();

            // Assert
            Assert.That(files, Has.Count.EqualTo(2));
            Assert.That(files.Any(f => f.EndsWith("test1.json")), Is.True);
            Assert.That(files.Any(f => f.EndsWith("test2.json")), Is.True);
            Assert.That(files.Any(f => f.EndsWith("test.txt")), Is.False);
        }

        [Test]
        public async Task ScanJsonFilesAsync_WhenSubdirectoriesExist_ReturnsAllJsonFiles()
        {
            // Arrange
            var subdir = Path.Combine(_testAppDataPath, "subdir");
            Directory.CreateDirectory(subdir);
            File.WriteAllText(Path.Combine(_testAppDataPath, "root.json"), "{}");
            File.WriteAllText(Path.Combine(subdir, "sub.json"), "{}");

            // Act
            var files = await _service.ScanJsonFilesAsync();

            // Assert
            Assert.That(files, Has.Count.EqualTo(2));
            Assert.That(files.Any(f => f.EndsWith("root.json")), Is.True);
            Assert.That(files.Any(f => f.EndsWith("sub.json")), Is.True);
        }

        [Test]
        public async Task ScanJsonFilesAsync_WhenDirectoryDoesNotExist_ReturnsEmptyList()
        {
            // Arrange
            Directory.Delete(_testAppDataPath, true);

            // Act
            var files = await _service.ScanJsonFilesAsync();

            // Assert
            Assert.That(files, Is.Empty);
        }

        [Test]
        public async Task ScanJsonFilesAsync_WhenExceptionOccurs_ReturnsEmptyList()
        {
            // Arrange
            _appSettings.Vector.AppDataPath = "\0invalid\0path";
            var serviceWithInvalidPath = new KnowledgeBaseManagementService(
                _logger,
                _fakeVectorDatabaseService,
                _fakeEmbeddingService,
                _fakeKnowledgeBaseService,
                _appSettings
            );

            // Act
            var files = await serviceWithInvalidPath.ScanJsonFilesAsync();

            // Assert
            Assert.That(files, Is.Empty);
        }

        #endregion

        #region Fake Service Implementations

        private class FakeVectorDatabaseService : IVectorDatabaseService
        {
            public bool IsConnected { get; set; }
            public int DeletedCount { get; set; }
            public bool StoreSuccess { get; set; }
            public Exception? ThrowException { get; set; }

            public async Task<bool> IsConnectedAsync()
            {
                if (ThrowException != null)
                    throw ThrowException;
                return await Task.FromResult(IsConnected);
            }

            public async Task<int> DeleteAllEmbeddingsAsync()
            {
                if (ThrowException != null)
                    throw ThrowException;
                return await Task.FromResult(DeletedCount);
            }

            public async Task<bool> StoreEmbeddingAsync(string id, float[] embedding, string jsonContent, string description, Dictionary<string, string>? metadata = null)
            {
                if (ThrowException != null)
                    throw ThrowException;
                return await Task.FromResult(StoreSuccess);
            }

            public Task<bool> InitializeAsync() => throw new NotImplementedException();
            public Task<List<SimilarityMatch>> FindSimilarAsync(float[] queryEmbedding, int maxResults = 5, float threshold = 0.7f)
                => throw new NotImplementedException();
            public Task<bool> EmbeddingExistsAsync(string id) => throw new NotImplementedException();
            public Task<bool> DeleteEmbeddingAsync(string id) => throw new NotImplementedException();
            public Task<List<string>> GetAllEmbeddingIdsAsync() => throw new NotImplementedException();
            public Task<long> GetEmbeddingCountAsync() => throw new NotImplementedException();
            public Task DisposeAsync() => throw new NotImplementedException();
        }

        private class FakeEmbeddingService : IEmbeddingService
        {
            public bool IsAvailable { get; set; }
            public float[]? Embedding { get; set; }
            public Exception? ThrowException { get; set; }

            public async Task<bool> IsEmbeddingServiceAvailableAsync()
            {
                if (ThrowException != null)
                    throw ThrowException;
                return await Task.FromResult(IsAvailable);
            }

            public async Task<float[]> GenerateEmbeddingAsync(string text)
            {
                if (ThrowException != null)
                    throw ThrowException;
                return await Task.FromResult(Embedding ?? Array.Empty<float>());
            }

            public string GetEmbeddingModelName() => "test-model";
        }

        private class FakeKnowledgeBaseService : IKnowledgeBaseService
        {
            public List<JsonExample>? Examples { get; set; }
            public Exception? ThrowException { get; set; }

            public async Task<List<JsonExample>> LoadExamplesAsync()
            {
                if (ThrowException != null)
                    throw ThrowException;
                return await Task.FromResult(Examples ?? new List<JsonExample>());
            }

            public Task InitializeVectorDatabaseAsync() => throw new NotImplementedException();
        }

        #endregion
    }
}
