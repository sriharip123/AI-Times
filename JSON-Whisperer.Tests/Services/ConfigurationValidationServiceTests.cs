using JSON_Whisperer.Interfaces;
using JSON_Whisperer.Models;
using JSON_Whisperer.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NUnit.Framework;

namespace JSON_Whisperer.Tests.Services
{
    [TestFixture]
    public class ConfigurationValidationServiceTests
    {
        private TestLogger<ConfigurationValidationService> _logger = null!;
        private IConfiguration _configuration = null!;
        private ConfigurationValidationService _validationService = null!;

        [SetUp]
        public void Setup()
        {
            _logger = new TestLogger<ConfigurationValidationService>();
        }

        private void SetupConfiguration(Dictionary<string, string?> configValues)
        {
            _configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(configValues)
                .Build();

            _validationService = new ConfigurationValidationService(_configuration, _logger);
        }

        #region ValidateAsync Tests

        [Test]
        public async Task ValidateAsync_WithValidConfiguration_ReturnsValidResult()
        {
            // Arrange
            var configValues = GetValidConfiguration();
            SetupConfiguration(configValues);

            // Act
            var result = await _validationService.ValidateAsync();

            // Assert
            Assert.That(result.IsValid, Is.True);
            Assert.That(result.TotalSections, Is.EqualTo(4));
            Assert.That(result.ValidSections, Is.EqualTo(4));
            Assert.That(result.InvalidSections, Is.EqualTo(0));
            Assert.That(result.Errors, Is.Empty);
        }

        [Test]
        public async Task ValidateAsync_WithInvalidConfiguration_ReturnsInvalidResult()
        {
            // Arrange
            var configValues = new Dictionary<string, string?>
            {
                ["Ollama:BaseUrl"] = "invalid-url",
                ["Ollama:ModelName"] = "",
                ["ScyllaDb:Port"] = "99999"
            };
            SetupConfiguration(configValues);

            // Act
            var result = await _validationService.ValidateAsync();

            // Assert
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.InvalidSections, Is.GreaterThan(0));
            Assert.That(result.Errors, Is.Not.Empty);
        }

        [Test]
        public async Task ValidateAsync_ReturnsResultsForAllSections()
        {
            // Arrange
            var configValues = GetValidConfiguration();
            SetupConfiguration(configValues);

            // Act
            var result = await _validationService.ValidateAsync();

            // Assert
            Assert.That(result.Results.Count, Is.EqualTo(4));
            Assert.That(result.Results.Any(r => r.Section == "Ollama"), Is.True);
            Assert.That(result.Results.Any(r => r.Section == "ScyllaDB"), Is.True);
            Assert.That(result.Results.Any(r => r.Section == "Vector"), Is.True);
            Assert.That(result.Results.Any(r => r.Section == "Application"), Is.True);
        }

        #endregion

        #region ValidateOllamaConfig Tests

        [Test]
        public void ValidateOllamaConfig_WithValidSettings_ReturnsValidResult()
        {
            // Arrange
            SetupConfiguration(GetValidConfiguration());
            var settings = new OllamaSettings
            {
                BaseUrl = "http://localhost:11434",
                ModelName = "mistral",
                EmbeddingModel = "nomic-embed-text",
                TimeoutSeconds = 30,
                RetryAttempts = 3,
                RetryDelaySeconds = 5
            };

            // Act
            var result = _validationService.ValidateOllamaConfig(settings);

            // Assert
            Assert.That(result.IsValid, Is.True);
            Assert.That(result.Section, Is.EqualTo("Ollama"));
            Assert.That(result.Errors, Is.Empty);
        }

        [Test]
        public void ValidateOllamaConfig_WithEmptyBaseUrl_ReturnsInvalidResult()
        {
            // Arrange
            SetupConfiguration(GetValidConfiguration());
            var settings = new OllamaSettings
            {
                BaseUrl = "",
                ModelName = "mistral",
                EmbeddingModel = "nomic-embed-text"
            };

            // Act
            var result = _validationService.ValidateOllamaConfig(settings);

            // Assert
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Errors, Has.Some.Contains("BaseUrl is required"));
        }

        [Test]
        public void ValidateOllamaConfig_WithInvalidUrl_ReturnsInvalidResult()
        {
            // Arrange
            SetupConfiguration(GetValidConfiguration());
            var settings = new OllamaSettings
            {
                BaseUrl = "not-a-valid-url",
                ModelName = "mistral",
                EmbeddingModel = "nomic-embed-text"
            };

            // Act
            var result = _validationService.ValidateOllamaConfig(settings);

            // Assert
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Errors, Has.Some.Contains("not a valid URL"));
        }

        [Test]
        public void ValidateOllamaConfig_WithUrlMissingScheme_ReturnsInvalidResult()
        {
            // Arrange
            SetupConfiguration(GetValidConfiguration());
            var settings = new OllamaSettings
            {
                BaseUrl = "localhost:11434",
                ModelName = "mistral",
                EmbeddingModel = "nomic-embed-text"
            };

            // Act
            var result = _validationService.ValidateOllamaConfig(settings);

            // Assert
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Errors, Has.Some.Contains("not a valid URL"));
        }

        [Test]
        public void ValidateOllamaConfig_WithEmptyModelName_ReturnsInvalidResult()
        {
            // Arrange
            SetupConfiguration(GetValidConfiguration());
            var settings = new OllamaSettings
            {
                BaseUrl = "http://localhost:11434",
                ModelName = "",
                EmbeddingModel = "nomic-embed-text"
            };

            // Act
            var result = _validationService.ValidateOllamaConfig(settings);

            // Assert
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Errors, Has.Some.Contains("ModelName is required"));
        }

        [Test]
        public void ValidateOllamaConfig_WithEmptyEmbeddingModel_ReturnsInvalidResult()
        {
            // Arrange
            SetupConfiguration(GetValidConfiguration());
            var settings = new OllamaSettings
            {
                BaseUrl = "http://localhost:11434",
                ModelName = "mistral",
                EmbeddingModel = ""
            };

            // Act
            var result = _validationService.ValidateOllamaConfig(settings);

            // Assert
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Errors, Has.Some.Contains("EmbeddingModel is required"));
        }

        [Test]
        public void ValidateOllamaConfig_WithTimeoutOutOfRange_ReturnsInvalidResult()
        {
            // Arrange
            SetupConfiguration(GetValidConfiguration());
            var settings = new OllamaSettings
            {
                BaseUrl = "http://localhost:11434",
                ModelName = "mistral",
                EmbeddingModel = "nomic-embed-text",
                TimeoutSeconds = 500
            };

            // Act
            var result = _validationService.ValidateOllamaConfig(settings);

            // Assert
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Errors, Has.Some.Contains("TimeoutSeconds must be between 1 and 300"));
        }

        [Test]
        public void ValidateOllamaConfig_WithLowTimeout_ReturnsWarning()
        {
            // Arrange
            SetupConfiguration(GetValidConfiguration());
            var settings = new OllamaSettings
            {
                BaseUrl = "http://localhost:11434",
                ModelName = "mistral",
                EmbeddingModel = "nomic-embed-text",
                TimeoutSeconds = 5
            };

            // Act
            var result = _validationService.ValidateOllamaConfig(settings);

            // Assert
            Assert.That(result.IsValid, Is.True);
            Assert.That(result.Warnings, Has.Some.Contains("very low"));
        }

        [Test]
        public void ValidateOllamaConfig_WithRetryAttemptsOutOfRange_ReturnsInvalidResult()
        {
            // Arrange
            SetupConfiguration(GetValidConfiguration());
            var settings = new OllamaSettings
            {
                BaseUrl = "http://localhost:11434",
                ModelName = "mistral",
                EmbeddingModel = "nomic-embed-text",
                RetryAttempts = 15
            };

            // Act
            var result = _validationService.ValidateOllamaConfig(settings);

            // Assert
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Errors, Has.Some.Contains("RetryAttempts must be between 0 and 10"));
        }

        [Test]
        public void ValidateOllamaConfig_WithRetryDelayOutOfRange_ReturnsInvalidResult()
        {
            // Arrange
            SetupConfiguration(GetValidConfiguration());
            var settings = new OllamaSettings
            {
                BaseUrl = "http://localhost:11434",
                ModelName = "mistral",
                EmbeddingModel = "nomic-embed-text",
                RetryDelaySeconds = 100
            };

            // Act
            var result = _validationService.ValidateOllamaConfig(settings);

            // Assert
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Errors, Has.Some.Contains("RetryDelaySeconds must be between 1 and 60"));
        }

        #endregion

        #region ValidateScyllaDbConfig Tests

        [Test]
        public void ValidateScyllaDbConfig_WithValidSettings_ReturnsValidResult()
        {
            // Arrange
            SetupConfiguration(GetValidConfiguration());
            var settings = new ScyllaDbSettings
            {
                ContactPoints = "127.0.0.1",
                Port = 9042,
                Keyspace = "json_whisperer",
                DataCenter = "datacenter1",
                ConnectionTimeoutSeconds = 30,
                QueryTimeoutSeconds = 30
            };

            // Act
            var result = _validationService.ValidateScyllaDbConfig(settings);

            // Assert
            Assert.That(result.IsValid, Is.True);
            Assert.That(result.Section, Is.EqualTo("ScyllaDB"));
            Assert.That(result.Errors, Is.Empty);
        }

        [Test]
        public void ValidateScyllaDbConfig_WithEmptyContactPoints_ReturnsInvalidResult()
        {
            // Arrange
            SetupConfiguration(GetValidConfiguration());
            var settings = new ScyllaDbSettings
            {
                ContactPoints = "",
                Port = 9042,
                Keyspace = "json_whisperer"
            };

            // Act
            var result = _validationService.ValidateScyllaDbConfig(settings);

            // Assert
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Errors, Has.Some.Contains("ContactPoints is required"));
        }

        [Test]
        public void ValidateScyllaDbConfig_WithInvalidContactPoint_ReturnsInvalidResult()
        {
            // Arrange
            SetupConfiguration(GetValidConfiguration());
            var settings = new ScyllaDbSettings
            {
                ContactPoints = "invalid@host!",
                Port = 9042,
                Keyspace = "json_whisperer"
            };

            // Act
            var result = _validationService.ValidateScyllaDbConfig(settings);

            // Assert
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Errors, Has.Some.Contains("Invalid ScyllaDB contact point"));
        }

        [Test]
        public void ValidateScyllaDbConfig_WithMultipleValidContactPoints_ReturnsValidResult()
        {
            // Arrange
            SetupConfiguration(GetValidConfiguration());
            var settings = new ScyllaDbSettings
            {
                ContactPoints = "127.0.0.1,192.168.1.1,scylla.example.com",
                Port = 9042,
                Keyspace = "json_whisperer"
            };

            // Act
            var result = _validationService.ValidateScyllaDbConfig(settings);

            // Assert
            Assert.That(result.IsValid, Is.True);
        }

        [Test]
        public void ValidateScyllaDbConfig_WithPortOutOfRange_ReturnsInvalidResult()
        {
            // Arrange
            SetupConfiguration(GetValidConfiguration());
            var settings = new ScyllaDbSettings
            {
                ContactPoints = "127.0.0.1",
                Port = 99999,
                Keyspace = "json_whisperer"
            };

            // Act
            var result = _validationService.ValidateScyllaDbConfig(settings);

            // Assert
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Errors, Has.Some.Contains("Port must be between 1 and 65535"));
        }

        [Test]
        public void ValidateScyllaDbConfig_WithEmptyKeyspace_ReturnsInvalidResult()
        {
            // Arrange
            SetupConfiguration(GetValidConfiguration());
            var settings = new ScyllaDbSettings
            {
                ContactPoints = "127.0.0.1",
                Port = 9042,
                Keyspace = ""
            };

            // Act
            var result = _validationService.ValidateScyllaDbConfig(settings);

            // Assert
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Errors, Has.Some.Contains("Keyspace is required"));
        }

        [Test]
        public void ValidateScyllaDbConfig_WithInvalidKeyspaceName_ReturnsInvalidResult()
        {
            // Arrange
            SetupConfiguration(GetValidConfiguration());
            var settings = new ScyllaDbSettings
            {
                ContactPoints = "127.0.0.1",
                Port = 9042,
                Keyspace = "invalid-keyspace-name"
            };

            // Act
            var result = _validationService.ValidateScyllaDbConfig(settings);

            // Assert
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Errors, Has.Some.Contains("Keyspace name is invalid"));
        }

        [Test]
        public void ValidateScyllaDbConfig_WithEmptyDataCenter_ReturnsWarning()
        {
            // Arrange
            SetupConfiguration(GetValidConfiguration());
            var settings = new ScyllaDbSettings
            {
                ContactPoints = "127.0.0.1",
                Port = 9042,
                Keyspace = "json_whisperer",
                DataCenter = ""
            };

            // Act
            var result = _validationService.ValidateScyllaDbConfig(settings);

            // Assert
            Assert.That(result.IsValid, Is.True);
            Assert.That(result.Warnings, Has.Some.Contains("DataCenter is not specified"));
        }

        [Test]
        public void ValidateScyllaDbConfig_WithConnectionTimeoutOutOfRange_ReturnsInvalidResult()
        {
            // Arrange
            SetupConfiguration(GetValidConfiguration());
            var settings = new ScyllaDbSettings
            {
                ContactPoints = "127.0.0.1",
                Port = 9042,
                Keyspace = "json_whisperer",
                ConnectionTimeoutSeconds = 500
            };

            // Act
            var result = _validationService.ValidateScyllaDbConfig(settings);

            // Assert
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Errors, Has.Some.Contains("ConnectionTimeoutSeconds must be between 1 and 300"));
        }

        [Test]
        public void ValidateScyllaDbConfig_WithQueryTimeoutOutOfRange_ReturnsInvalidResult()
        {
            // Arrange
            SetupConfiguration(GetValidConfiguration());
            var settings = new ScyllaDbSettings
            {
                ContactPoints = "127.0.0.1",
                Port = 9042,
                Keyspace = "json_whisperer",
                QueryTimeoutSeconds = 0
            };

            // Act
            var result = _validationService.ValidateScyllaDbConfig(settings);

            // Assert
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Errors, Has.Some.Contains("QueryTimeoutSeconds must be between 1 and 300"));
        }

        [Test]
        public void ValidateScyllaDbConfig_WithUsernameButNoPassword_ReturnsWarning()
        {
            // Arrange
            SetupConfiguration(GetValidConfiguration());
            var settings = new ScyllaDbSettings
            {
                ContactPoints = "127.0.0.1",
                Port = 9042,
                Keyspace = "json_whisperer",
                Username = "admin",
                Password = ""
            };

            // Act
            var result = _validationService.ValidateScyllaDbConfig(settings);

            // Assert
            Assert.That(result.IsValid, Is.True);
            Assert.That(result.Warnings, Has.Some.Contains("Username is specified but Password is empty"));
        }

        #endregion

        #region ValidateVectorConfig Tests

        [Test]
        public void ValidateVectorConfig_WithValidSettings_ReturnsValidResult()
        {
            // Arrange
            SetupConfiguration(GetValidConfiguration());
            var settings = new VectorSettings
            {
                SimilarityThreshold = 0.7f,
                MaxSimilarResults = 5,
                AppDataPath = "AppData",
                EnableSimilarityMatching = true,
                InitializeKnowledgeBase = true
            };

            // Act
            var result = _validationService.ValidateVectorConfig(settings);

            // Assert
            Assert.That(result.IsValid, Is.True);
            Assert.That(result.Section, Is.EqualTo("Vector"));
            Assert.That(result.Errors, Is.Empty);
        }

        [Test]
        public void ValidateVectorConfig_WithSimilarityThresholdOutOfRange_ReturnsInvalidResult()
        {
            // Arrange
            SetupConfiguration(GetValidConfiguration());
            var settings = new VectorSettings
            {
                SimilarityThreshold = 1.5f,
                MaxSimilarResults = 5,
                AppDataPath = "AppData"
            };

            // Act
            var result = _validationService.ValidateVectorConfig(settings);

            // Assert
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Errors, Has.Some.Contains("SimilarityThreshold must be between 0.0 and 1.0"));
        }

        [Test]
        public void ValidateVectorConfig_WithLowSimilarityThreshold_ReturnsWarning()
        {
            // Arrange
            SetupConfiguration(GetValidConfiguration());
            var settings = new VectorSettings
            {
                SimilarityThreshold = 0.2f,
                MaxSimilarResults = 5,
                AppDataPath = "AppData"
            };

            // Act
            var result = _validationService.ValidateVectorConfig(settings);

            // Assert
            Assert.That(result.IsValid, Is.True);
            Assert.That(result.Warnings, Has.Some.Contains("very low"));
        }

        [Test]
        public void ValidateVectorConfig_WithHighSimilarityThreshold_ReturnsWarning()
        {
            // Arrange
            SetupConfiguration(GetValidConfiguration());
            var settings = new VectorSettings
            {
                SimilarityThreshold = 0.96f,
                MaxSimilarResults = 5,
                AppDataPath = "AppData"
            };

            // Act
            var result = _validationService.ValidateVectorConfig(settings);

            // Assert
            Assert.That(result.IsValid, Is.True);
            Assert.That(result.Warnings, Has.Some.Contains("very high"));
        }

        [Test]
        public void ValidateVectorConfig_WithMaxSimilarResultsOutOfRange_ReturnsInvalidResult()
        {
            // Arrange
            SetupConfiguration(GetValidConfiguration());
            var settings = new VectorSettings
            {
                SimilarityThreshold = 0.7f,
                MaxSimilarResults = 100,
                AppDataPath = "AppData"
            };

            // Act
            var result = _validationService.ValidateVectorConfig(settings);

            // Assert
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Errors, Has.Some.Contains("MaxSimilarResults must be between 1 and 50"));
        }

        [Test]
        public void ValidateVectorConfig_WithEmptyAppDataPath_ReturnsInvalidResult()
        {
            // Arrange
            SetupConfiguration(GetValidConfiguration());
            var settings = new VectorSettings
            {
                SimilarityThreshold = 0.7f,
                MaxSimilarResults = 5,
                AppDataPath = ""
            };

            // Act
            var result = _validationService.ValidateVectorConfig(settings);

            // Assert
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Errors, Has.Some.Contains("AppDataPath is required"));
        }

        [Test]
        public void ValidateVectorConfig_WithNonExistentAppDataPath_ReturnsWarning()
        {
            // Arrange
            SetupConfiguration(GetValidConfiguration());
            var settings = new VectorSettings
            {
                SimilarityThreshold = 0.7f,
                MaxSimilarResults = 5,
                AppDataPath = "NonExistentPath123456"
            };

            // Act
            var result = _validationService.ValidateVectorConfig(settings);

            // Assert
            Assert.That(result.IsValid, Is.True);
            Assert.That(result.Warnings, Has.Some.Contains("does not exist"));
        }

        [Test]
        public void ValidateVectorConfig_WithSimilarityDisabledButInitializeEnabled_ReturnsWarning()
        {
            // Arrange
            SetupConfiguration(GetValidConfiguration());
            var settings = new VectorSettings
            {
                SimilarityThreshold = 0.7f,
                MaxSimilarResults = 5,
                AppDataPath = "AppData",
                EnableSimilarityMatching = false,
                InitializeKnowledgeBase = true
            };

            // Act
            var result = _validationService.ValidateVectorConfig(settings);

            // Assert
            Assert.That(result.IsValid, Is.True);
            Assert.That(result.Warnings, Has.Some.Contains("similarity matching is disabled"));
        }

        #endregion

        #region ValidateApplicationConfig Tests

        [Test]
        public void ValidateApplicationConfig_WithValidSettings_ReturnsValidResult()
        {
            // Arrange
            SetupConfiguration(GetValidConfiguration());
            var settings = new ApplicationSettings
            {
                MaxJsonSizeBytes = 1048576,
                OutputFormat = "standard",
                VerboseMode = false
            };

            // Act
            var result = _validationService.ValidateApplicationConfig(settings);

            // Assert
            Assert.That(result.IsValid, Is.True);
            Assert.That(result.Section, Is.EqualTo("Application"));
            Assert.That(result.Errors, Is.Empty);
        }

        [Test]
        public void ValidateApplicationConfig_WithMaxJsonSizeTooSmall_ReturnsInvalidResult()
        {
            // Arrange
            SetupConfiguration(GetValidConfiguration());
            var settings = new ApplicationSettings
            {
                MaxJsonSizeBytes = 500,
                OutputFormat = "standard"
            };

            // Act
            var result = _validationService.ValidateApplicationConfig(settings);

            // Assert
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Errors, Has.Some.Contains("MaxJsonSizeBytes must be between 1024"));
        }

        [Test]
        public void ValidateApplicationConfig_WithMaxJsonSizeTooLarge_ReturnsInvalidResult()
        {
            // Arrange
            SetupConfiguration(GetValidConfiguration());
            var settings = new ApplicationSettings
            {
                MaxJsonSizeBytes = 200000000,
                OutputFormat = "standard"
            };

            // Act
            var result = _validationService.ValidateApplicationConfig(settings);

            // Assert
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Errors, Has.Some.Contains("MaxJsonSizeBytes must be between 1024"));
        }

        [Test]
        public void ValidateApplicationConfig_WithVerySmallMaxJsonSize_ReturnsWarning()
        {
            // Arrange
            SetupConfiguration(GetValidConfiguration());
            var settings = new ApplicationSettings
            {
                MaxJsonSizeBytes = 5000,
                OutputFormat = "standard"
            };

            // Act
            var result = _validationService.ValidateApplicationConfig(settings);

            // Assert
            Assert.That(result.IsValid, Is.True);
            Assert.That(result.Warnings, Has.Some.Contains("very small"));
        }

        [Test]
        public void ValidateApplicationConfig_WithEmptyOutputFormat_ReturnsInvalidResult()
        {
            // Arrange
            SetupConfiguration(GetValidConfiguration());
            var settings = new ApplicationSettings
            {
                MaxJsonSizeBytes = 1048576,
                OutputFormat = ""
            };

            // Act
            var result = _validationService.ValidateApplicationConfig(settings);

            // Assert
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Errors, Has.Some.Contains("OutputFormat is required"));
        }

        [Test]
        public void ValidateApplicationConfig_WithInvalidOutputFormat_ReturnsInvalidResult()
        {
            // Arrange
            SetupConfiguration(GetValidConfiguration());
            var settings = new ApplicationSettings
            {
                MaxJsonSizeBytes = 1048576,
                OutputFormat = "invalid-format"
            };

            // Act
            var result = _validationService.ValidateApplicationConfig(settings);

            // Assert
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Errors, Has.Some.Contains("OutputFormat must be one of"));
        }

        [Test]
        public void ValidateApplicationConfig_WithCompactOutputFormat_ReturnsValidResult()
        {
            // Arrange
            SetupConfiguration(GetValidConfiguration());
            var settings = new ApplicationSettings
            {
                MaxJsonSizeBytes = 1048576,
                OutputFormat = "compact"
            };

            // Act
            var result = _validationService.ValidateApplicationConfig(settings);

            // Assert
            Assert.That(result.IsValid, Is.True);
        }

        [Test]
        public void ValidateApplicationConfig_WithDetailedOutputFormat_ReturnsValidResult()
        {
            // Arrange
            SetupConfiguration(GetValidConfiguration());
            var settings = new ApplicationSettings
            {
                MaxJsonSizeBytes = 1048576,
                OutputFormat = "detailed"
            };

            // Act
            var result = _validationService.ValidateApplicationConfig(settings);

            // Assert
            Assert.That(result.IsValid, Is.True);
        }

        #endregion

        #region Helper Methods

        private Dictionary<string, string?> GetValidConfiguration()
        {
            return new Dictionary<string, string?>
            {
                ["Ollama:BaseUrl"] = "http://localhost:11434",
                ["Ollama:ModelName"] = "mistral",
                ["Ollama:EmbeddingModel"] = "nomic-embed-text",
                ["Ollama:TimeoutSeconds"] = "30",
                ["Ollama:RetryAttempts"] = "3",
                ["Ollama:RetryDelaySeconds"] = "5",
                ["ScyllaDb:ContactPoints"] = "127.0.0.1",
                ["ScyllaDb:Port"] = "9042",
                ["ScyllaDb:Keyspace"] = "json_whisperer",
                ["ScyllaDb:DataCenter"] = "datacenter1",
                ["ScyllaDb:ConnectionTimeoutSeconds"] = "30",
                ["ScyllaDb:QueryTimeoutSeconds"] = "30",
                ["Vector:SimilarityThreshold"] = "0.7",
                ["Vector:MaxSimilarResults"] = "5",
                ["Vector:AppDataPath"] = "AppData",
                ["Vector:EnableSimilarityMatching"] = "true",
                ["Vector:InitializeKnowledgeBase"] = "true",
                ["Application:MaxJsonSizeBytes"] = "1048576",
                ["Application:OutputFormat"] = "standard",
                ["Application:VerboseMode"] = "false"
            };
        }

        #endregion
    }
}
