using JSON_Whisperer.Interfaces;
using JSON_Whisperer.Models;
using JSON_Whisperer.Services;
using Microsoft.Extensions.Logging;
using NUnit.Framework;

namespace JSON_Whisperer.Tests.Services
{
    [TestFixture]
    public class DiagnosticCommandExecutorTests
    {
        private TestLogger<DiagnosticCommandExecutor> _logger = null!;
        private FakeAiService _fakeAiService = null!;
        private FakeEmbeddingService _fakeEmbeddingService = null!;
        private FakeVectorDatabaseService _fakeVectorDatabaseService = null!;
        private FakeKnowledgeBaseService _fakeKnowledgeBaseService = null!;
        private FakeKnowledgeBaseManagementService _fakeKnowledgeBaseManagementService = null!;
        private FakeConfigurationService _fakeConfigurationService = null!;
        private AppSettings _appSettings = null!;
        private DiagnosticCommandExecutor _executor = null!;

        [SetUp]
        public void Setup()
        {
            _logger = new TestLogger<DiagnosticCommandExecutor>();
            _fakeAiService = new FakeAiService();
            _fakeEmbeddingService = new FakeEmbeddingService();
            _fakeVectorDatabaseService = new FakeVectorDatabaseService();
            _fakeKnowledgeBaseService = new FakeKnowledgeBaseService();
            _fakeKnowledgeBaseManagementService = new FakeKnowledgeBaseManagementService();
            _fakeConfigurationService = new FakeConfigurationService();

            _appSettings = new AppSettings
            {
                Ollama = new OllamaSettings
                {
                    BaseUrl = "http://localhost:11434",
                    ModelName = "mistral",
                    EmbeddingModel = "nomic-embed-text",
                    TimeoutSeconds = 30
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
                },
                Application = new ApplicationSettings
                {
                    MaxJsonSizeBytes = 1048576,
                    OutputFormat = "standard",
                    VerboseMode = false
                }
            };

            _executor = new DiagnosticCommandExecutor(
                _logger,
                _fakeAiService,
                _fakeEmbeddingService,
                _fakeVectorDatabaseService,
                _fakeKnowledgeBaseService,
                _fakeKnowledgeBaseManagementService,
                _fakeConfigurationService,
                _appSettings
            );
        }

        #region Command Routing Tests

        [Test]
        public async Task ExecuteAsync_WithHealthCheckCommand_RoutesToHealthCheck()
        {
            // Arrange
            _fakeAiService.IsAvailable = true;
            _fakeVectorDatabaseService.IsConnected = true;
            _fakeEmbeddingService.IsAvailable = true;
            _fakeKnowledgeBaseService.Examples = new List<JsonExample>
            {
                new JsonExample { FilePath = "test.json", Description = "Test" }
            };

            var options = new CommandLineOptions
            {
                Mode = ExecutionMode.Diagnostic,
                DiagnosticCommand = DiagnosticCommand.HealthCheck,
                VerboseMode = false
            };

            // Act
            var exitCode = await _executor.ExecuteAsync(DiagnosticCommand.HealthCheck, options);

            // Assert
            Assert.That(exitCode, Is.EqualTo(ExitCodes.Success));
        }

        [Test]
        public async Task ExecuteAsync_WithValidateConfigCommand_RoutesToConfigValidation()
        {
            // Arrange
            var options = new CommandLineOptions
            {
                Mode = ExecutionMode.Diagnostic,
                DiagnosticCommand = DiagnosticCommand.ValidateConfig,
                VerboseMode = false
            };

            // Act
            var exitCode = await _executor.ExecuteAsync(DiagnosticCommand.ValidateConfig, options);

            // Assert
            Assert.That(exitCode, Is.EqualTo(ExitCodes.Success));
        }

        [Test]
        public async Task ExecuteAsync_WithTestOllamaCommand_RoutesToOllamaTest()
        {
            // Arrange
            _fakeAiService.IsAvailable = true;

            var options = new CommandLineOptions
            {
                Mode = ExecutionMode.Diagnostic,
                DiagnosticCommand = DiagnosticCommand.TestOllama,
                VerboseMode = false
            };

            // Act
            var exitCode = await _executor.ExecuteAsync(DiagnosticCommand.TestOllama, options);

            // Assert
            Assert.That(exitCode, Is.EqualTo(ExitCodes.Success));
        }

        [Test]
        public async Task ExecuteAsync_WithTestScyllaCommand_RoutesToScyllaTest()
        {
            // Arrange
            _fakeVectorDatabaseService.IsConnected = true;
            _fakeVectorDatabaseService.EmbeddingCount = 10;

            var options = new CommandLineOptions
            {
                Mode = ExecutionMode.Diagnostic,
                DiagnosticCommand = DiagnosticCommand.TestScylla,
                VerboseMode = false
            };

            // Act
            var exitCode = await _executor.ExecuteAsync(DiagnosticCommand.TestScylla, options);

            // Assert
            Assert.That(exitCode, Is.EqualTo(ExitCodes.Success));
        }

        [Test]
        public async Task ExecuteAsync_WithTestEmbeddingCommand_RoutesToEmbeddingTest()
        {
            // Arrange
            _fakeEmbeddingService.IsAvailable = true;

            var options = new CommandLineOptions
            {
                Mode = ExecutionMode.Diagnostic,
                DiagnosticCommand = DiagnosticCommand.TestEmbedding,
                VerboseMode = false
            };

            // Act
            var exitCode = await _executor.ExecuteAsync(DiagnosticCommand.TestEmbedding, options);

            // Assert
            Assert.That(exitCode, Is.EqualTo(ExitCodes.Success));
        }

        [Test]
        public async Task ExecuteAsync_WithTestSimilarityCommand_RoutesToSimilarityTest()
        {
            // Arrange
            _fakeEmbeddingService.IsAvailable = true;
            _fakeVectorDatabaseService.IsConnected = true;
            _fakeVectorDatabaseService.EmbeddingCount = 5;

            var options = new CommandLineOptions
            {
                Mode = ExecutionMode.Diagnostic,
                DiagnosticCommand = DiagnosticCommand.TestSimilarity,
                VerboseMode = false
            };

            // Act
            var exitCode = await _executor.ExecuteAsync(DiagnosticCommand.TestSimilarity, options);

            // Assert
            Assert.That(exitCode, Is.EqualTo(ExitCodes.Success));
        }

        [Test]
        public async Task ExecuteAsync_WithReinitializeKnowledgeBaseCommand_RoutesToReinitialize()
        {
            // Arrange
            _fakeEmbeddingService.IsAvailable = true;
            _fakeVectorDatabaseService.IsConnected = true;

            var options = new CommandLineOptions
            {
                Mode = ExecutionMode.Diagnostic,
                DiagnosticCommand = DiagnosticCommand.ReinitializeKnowledgeBase,
                VerboseMode = false
            };

            // Act
            var exitCode = await _executor.ExecuteAsync(DiagnosticCommand.ReinitializeKnowledgeBase, options);

            // Assert
            Assert.That(exitCode, Is.EqualTo(ExitCodes.Success));
        }

        [Test]
        public async Task ExecuteAsync_WithValidateKnowledgeBaseCommand_RoutesToValidation()
        {
            // Arrange
            _fakeKnowledgeBaseService.Examples = new List<JsonExample>
            {
                new JsonExample { FilePath = "test.json", Description = "Test" }
            };
            _fakeVectorDatabaseService.IsConnected = true;
            _fakeVectorDatabaseService.EmbeddingCount = 1;

            var options = new CommandLineOptions
            {
                Mode = ExecutionMode.Diagnostic,
                DiagnosticCommand = DiagnosticCommand.ValidateKnowledgeBase,
                VerboseMode = false
            };

            // Act
            var exitCode = await _executor.ExecuteAsync(DiagnosticCommand.ValidateKnowledgeBase, options);

            // Assert
            Assert.That(exitCode, Is.EqualTo(ExitCodes.Success));
        }

        [Test]
        public async Task ExecuteAsync_WithBenchmarkAllCommand_RoutesToBenchmarkAll()
        {
            // Arrange
            _fakeEmbeddingService.IsAvailable = true;
            _fakeVectorDatabaseService.IsConnected = true;
            _fakeVectorDatabaseService.EmbeddingCount = 10;

            var options = new CommandLineOptions
            {
                Mode = ExecutionMode.Diagnostic,
                DiagnosticCommand = DiagnosticCommand.BenchmarkAll,
                VerboseMode = false
            };

            // Act
            var exitCode = await _executor.ExecuteAsync(DiagnosticCommand.BenchmarkAll, options);

            // Assert
            Assert.That(exitCode, Is.EqualTo(ExitCodes.Success));
        }

        [Test]
        public async Task ExecuteAsync_WithBenchmarkSimilarityCommand_RoutesToSimilarityBenchmark()
        {
            // Arrange
            _fakeEmbeddingService.IsAvailable = true;
            _fakeVectorDatabaseService.IsConnected = true;
            _fakeVectorDatabaseService.EmbeddingCount = 10;

            var options = new CommandLineOptions
            {
                Mode = ExecutionMode.Diagnostic,
                DiagnosticCommand = DiagnosticCommand.BenchmarkSimilarity,
                VerboseMode = false
            };

            // Act
            var exitCode = await _executor.ExecuteAsync(DiagnosticCommand.BenchmarkSimilarity, options);

            // Assert
            Assert.That(exitCode, Is.EqualTo(ExitCodes.Success));
        }

        [Test]
        public async Task ExecuteAsync_WithBenchmarkVectorOperationsCommand_RoutesToVectorBenchmark()
        {
            // Arrange
            _fakeVectorDatabaseService.IsConnected = true;

            var options = new CommandLineOptions
            {
                Mode = ExecutionMode.Diagnostic,
                DiagnosticCommand = DiagnosticCommand.BenchmarkVectorOperations,
                VerboseMode = false
            };

            // Act
            var exitCode = await _executor.ExecuteAsync(DiagnosticCommand.BenchmarkVectorOperations, options);

            // Assert
            Assert.That(exitCode, Is.EqualTo(ExitCodes.Success));
        }

        [Test]
        public async Task ExecuteAsync_WithBenchmarkEmbeddingCommand_RoutesToEmbeddingBenchmark()
        {
            // Arrange
            _fakeEmbeddingService.IsAvailable = true;

            var options = new CommandLineOptions
            {
                Mode = ExecutionMode.Diagnostic,
                DiagnosticCommand = DiagnosticCommand.BenchmarkEmbedding,
                VerboseMode = false
            };

            // Act
            var exitCode = await _executor.ExecuteAsync(DiagnosticCommand.BenchmarkEmbedding, options);

            // Assert
            Assert.That(exitCode, Is.EqualTo(ExitCodes.Success));
        }

        #endregion

        #region Exit Code Generation Tests

        [Test]
        public async Task ExecuteAsync_WhenHealthCheckSucceeds_ReturnsSuccessExitCode()
        {
            // Arrange
            _fakeAiService.IsAvailable = true;
            _fakeVectorDatabaseService.IsConnected = true;
            _fakeEmbeddingService.IsAvailable = true;
            _fakeKnowledgeBaseService.Examples = new List<JsonExample>();

            var options = new CommandLineOptions
            {
                DiagnosticCommand = DiagnosticCommand.HealthCheck
            };

            // Act
            var exitCode = await _executor.ExecuteAsync(DiagnosticCommand.HealthCheck, options);

            // Assert
            Assert.That(exitCode, Is.EqualTo(ExitCodes.Success));
        }

        [Test]
        public async Task ExecuteAsync_WhenHealthCheckFails_ReturnsServiceUnavailableExitCode()
        {
            // Arrange
            _fakeAiService.IsAvailable = false;
            _fakeVectorDatabaseService.IsConnected = false;
            _fakeEmbeddingService.IsAvailable = false;
            _fakeKnowledgeBaseService.ThrowException = new Exception("Error");

            var options = new CommandLineOptions
            {
                DiagnosticCommand = DiagnosticCommand.HealthCheck
            };

            // Act
            var exitCode = await _executor.ExecuteAsync(DiagnosticCommand.HealthCheck, options);

            // Assert
            Assert.That(exitCode, Is.EqualTo(ExitCodes.ServiceUnavailable));
        }

        [Test]
        public async Task ExecuteAsync_WhenConfigValidationSucceeds_ReturnsSuccessExitCode()
        {
            // Arrange
            var options = new CommandLineOptions
            {
                DiagnosticCommand = DiagnosticCommand.ValidateConfig
            };

            // Act
            var exitCode = await _executor.ExecuteAsync(DiagnosticCommand.ValidateConfig, options);

            // Assert
            Assert.That(exitCode, Is.EqualTo(ExitCodes.Success));
        }

        [Test]
        public async Task ExecuteAsync_WhenConfigValidationFails_ReturnsConfigurationErrorExitCode()
        {
            // Arrange
            _appSettings.Ollama.BaseUrl = ""; // Invalid URL

            var options = new CommandLineOptions
            {
                DiagnosticCommand = DiagnosticCommand.ValidateConfig
            };

            // Act
            var exitCode = await _executor.ExecuteAsync(DiagnosticCommand.ValidateConfig, options);

            // Assert
            Assert.That(exitCode, Is.EqualTo(ExitCodes.ConfigurationError));
        }

        [Test]
        public async Task ExecuteAsync_WhenServiceTestFails_ReturnsServiceUnavailableExitCode()
        {
            // Arrange
            _fakeAiService.IsAvailable = false;

            var options = new CommandLineOptions
            {
                DiagnosticCommand = DiagnosticCommand.TestOllama
            };

            // Act
            var exitCode = await _executor.ExecuteAsync(DiagnosticCommand.TestOllama, options);

            // Assert
            Assert.That(exitCode, Is.EqualTo(ExitCodes.ServiceUnavailable));
        }

        [Test]
        public async Task ExecuteAsync_WhenKnowledgeBaseValidationFails_ReturnsValidationErrorExitCode()
        {
            // Arrange
            _fakeKnowledgeBaseManagementService.ValidationResult = new KnowledgeBaseValidationResult
            {
                InvalidFiles = 1,
                Errors = new List<string> { "Validation error" }
            };

            var options = new CommandLineOptions
            {
                DiagnosticCommand = DiagnosticCommand.ValidateKnowledgeBase
            };

            // Act
            var exitCode = await _executor.ExecuteAsync(DiagnosticCommand.ValidateKnowledgeBase, options);

            // Assert
            Assert.That(exitCode, Is.EqualTo(ExitCodes.ValidationError));
        }

        [Test]
        public async Task ExecuteAsync_WhenBenchmarkFails_ReturnsGeneralErrorExitCode()
        {
            // Arrange
            _fakeEmbeddingService.ThrowException = new Exception("Benchmark error");

            var options = new CommandLineOptions
            {
                DiagnosticCommand = DiagnosticCommand.BenchmarkEmbedding
            };

            // Act
            var exitCode = await _executor.ExecuteAsync(DiagnosticCommand.BenchmarkEmbedding, options);

            // Assert
            Assert.That(exitCode, Is.EqualTo(ExitCodes.GeneralError));
        }

        #endregion

        #region Error Handling Tests

        [Test]
        public async Task ExecuteAsync_WhenExceptionOccurs_ReturnsServiceUnavailableExitCode()
        {
            // Arrange
            _fakeAiService.ThrowException = new InvalidOperationException("Unexpected error");

            var options = new CommandLineOptions
            {
                DiagnosticCommand = DiagnosticCommand.HealthCheck
            };

            // Act
            var exitCode = await _executor.ExecuteAsync(DiagnosticCommand.HealthCheck, options);

            // Assert
            // Health check returns ServiceUnavailable when any service fails
            Assert.That(exitCode, Is.EqualTo(ExitCodes.ServiceUnavailable));
        }

        [Test]
        public async Task ExecuteAsync_WhenServiceThrowsException_HandlesGracefully()
        {
            // Arrange
            _fakeVectorDatabaseService.ThrowException = new TimeoutException("Connection timeout");

            var options = new CommandLineOptions
            {
                DiagnosticCommand = DiagnosticCommand.TestScylla
            };

            // Act
            var exitCode = await _executor.ExecuteAsync(DiagnosticCommand.TestScylla, options);

            // Assert
            Assert.That(exitCode, Is.EqualTo(ExitCodes.ServiceUnavailable));
        }

        [Test]
        public async Task ExecuteAsync_WhenMultipleServicesThrowExceptions_HandlesAllGracefully()
        {
            // Arrange
            _fakeAiService.ThrowException = new Exception("Ollama error");
            _fakeVectorDatabaseService.ThrowException = new Exception("ScyllaDB error");
            _fakeEmbeddingService.ThrowException = new Exception("Embedding error");
            _fakeKnowledgeBaseService.ThrowException = new Exception("KB error");

            var options = new CommandLineOptions
            {
                DiagnosticCommand = DiagnosticCommand.HealthCheck
            };

            // Act
            var exitCode = await _executor.ExecuteAsync(DiagnosticCommand.HealthCheck, options);

            // Assert
            Assert.That(exitCode, Is.EqualTo(ExitCodes.ServiceUnavailable));
        }

        [Test]
        public async Task ExecuteAsync_WhenReinitializeFails_ReturnsServiceUnavailableExitCode()
        {
            // Arrange
            _fakeEmbeddingService.IsAvailable = false;

            var options = new CommandLineOptions
            {
                DiagnosticCommand = DiagnosticCommand.ReinitializeKnowledgeBase
            };

            // Act
            var exitCode = await _executor.ExecuteAsync(DiagnosticCommand.ReinitializeKnowledgeBase, options);

            // Assert
            // Reinitialize returns ServiceUnavailable when embedding service is not available
            Assert.That(exitCode, Is.EqualTo(ExitCodes.ServiceUnavailable));
        }

        #endregion

        #region Result Formatting Tests

        [Test]
        public async Task ExecuteAsync_WithVerboseMode_IncludesDetailedInformation()
        {
            // Arrange
            _fakeAiService.IsAvailable = true;
            _fakeVectorDatabaseService.IsConnected = true;
            _fakeVectorDatabaseService.EmbeddingCount = 42;
            _fakeEmbeddingService.IsAvailable = true;
            _fakeKnowledgeBaseService.Examples = new List<JsonExample>
            {
                new JsonExample { FilePath = "test.json", Description = "Test" }
            };

            var options = new CommandLineOptions
            {
                DiagnosticCommand = DiagnosticCommand.HealthCheck,
                VerboseMode = true
            };

            // Act
            var exitCode = await _executor.ExecuteAsync(DiagnosticCommand.HealthCheck, options);

            // Assert
            Assert.That(exitCode, Is.EqualTo(ExitCodes.Success));
            // In verbose mode, additional details are printed to console
            // We verify the command completes successfully
        }

        [Test]
        public async Task ExecuteAsync_WithoutVerboseMode_ShowsMinimalInformation()
        {
            // Arrange
            _fakeAiService.IsAvailable = true;
            _fakeVectorDatabaseService.IsConnected = true;
            _fakeEmbeddingService.IsAvailable = true;
            _fakeKnowledgeBaseService.Examples = new List<JsonExample>();

            var options = new CommandLineOptions
            {
                DiagnosticCommand = DiagnosticCommand.HealthCheck,
                VerboseMode = false
            };

            // Act
            var exitCode = await _executor.ExecuteAsync(DiagnosticCommand.HealthCheck, options);

            // Assert
            Assert.That(exitCode, Is.EqualTo(ExitCodes.Success));
            // Without verbose mode, minimal information is shown
        }

        [Test]
        public async Task ExecuteAsync_WhenTestSimilarityWithNoEmbeddings_ReturnsSuccessWithWarning()
        {
            // Arrange
            _fakeEmbeddingService.IsAvailable = true;
            _fakeVectorDatabaseService.IsConnected = true;
            _fakeVectorDatabaseService.EmbeddingCount = 0;

            var options = new CommandLineOptions
            {
                DiagnosticCommand = DiagnosticCommand.TestSimilarity
            };

            // Act
            var exitCode = await _executor.ExecuteAsync(DiagnosticCommand.TestSimilarity, options);

            // Assert
            Assert.That(exitCode, Is.EqualTo(ExitCodes.Success));
            // Should succeed but show warning about no embeddings
        }

        [Test]
        public async Task ExecuteAsync_WhenBenchmarkWithNoEmbeddings_ReturnsSuccessWithWarning()
        {
            // Arrange
            _fakeEmbeddingService.IsAvailable = true;
            _fakeVectorDatabaseService.IsConnected = true;
            _fakeVectorDatabaseService.EmbeddingCount = 0;

            var options = new CommandLineOptions
            {
                DiagnosticCommand = DiagnosticCommand.BenchmarkSimilarity
            };

            // Act
            var exitCode = await _executor.ExecuteAsync(DiagnosticCommand.BenchmarkSimilarity, options);

            // Assert
            Assert.That(exitCode, Is.EqualTo(ExitCodes.Success));
            // Should succeed but show warning about no embeddings
        }

        #endregion

        #region Configuration Validation Tests

        [Test]
        public async Task ExecuteAsync_ValidateConfig_WithInvalidOllamaUrl_ReturnsConfigurationError()
        {
            // Arrange
            _appSettings.Ollama.BaseUrl = "not-a-valid-url";

            var options = new CommandLineOptions
            {
                DiagnosticCommand = DiagnosticCommand.ValidateConfig
            };

            // Act
            var exitCode = await _executor.ExecuteAsync(DiagnosticCommand.ValidateConfig, options);

            // Assert
            Assert.That(exitCode, Is.EqualTo(ExitCodes.ConfigurationError));
        }

        [Test]
        public async Task ExecuteAsync_ValidateConfig_WithMissingModelName_ReturnsConfigurationError()
        {
            // Arrange
            _appSettings.Ollama.ModelName = "";

            var options = new CommandLineOptions
            {
                DiagnosticCommand = DiagnosticCommand.ValidateConfig
            };

            // Act
            var exitCode = await _executor.ExecuteAsync(DiagnosticCommand.ValidateConfig, options);

            // Assert
            Assert.That(exitCode, Is.EqualTo(ExitCodes.ConfigurationError));
        }

        [Test]
        public async Task ExecuteAsync_ValidateConfig_WithInvalidTimeout_ReturnsConfigurationError()
        {
            // Arrange
            _appSettings.Ollama.TimeoutSeconds = -1;

            var options = new CommandLineOptions
            {
                DiagnosticCommand = DiagnosticCommand.ValidateConfig
            };

            // Act
            var exitCode = await _executor.ExecuteAsync(DiagnosticCommand.ValidateConfig, options);

            // Assert
            Assert.That(exitCode, Is.EqualTo(ExitCodes.ConfigurationError));
        }

        [Test]
        public async Task ExecuteAsync_ValidateConfig_WithInvalidOutputFormat_ReturnsConfigurationError()
        {
            // Arrange
            _appSettings.Application.OutputFormat = "invalid-format";

            var options = new CommandLineOptions
            {
                DiagnosticCommand = DiagnosticCommand.ValidateConfig
            };

            // Act
            var exitCode = await _executor.ExecuteAsync(DiagnosticCommand.ValidateConfig, options);

            // Assert
            Assert.That(exitCode, Is.EqualTo(ExitCodes.ConfigurationError));
        }

        [Test]
        public async Task ExecuteAsync_ValidateConfig_WithInvalidMaxJsonSize_ReturnsConfigurationError()
        {
            // Arrange
            _appSettings.Application.MaxJsonSizeBytes = 0;

            var options = new CommandLineOptions
            {
                DiagnosticCommand = DiagnosticCommand.ValidateConfig
            };

            // Act
            var exitCode = await _executor.ExecuteAsync(DiagnosticCommand.ValidateConfig, options);

            // Assert
            Assert.That(exitCode, Is.EqualTo(ExitCodes.ConfigurationError));
        }

        #endregion

        #region Fake Service Implementations

        private class FakeAiService : IAiService
        {
            public bool IsAvailable { get; set; }
            public Exception? ThrowException { get; set; }

            public async Task<bool> IsAvailableAsync()
            {
                if (ThrowException != null)
                    throw ThrowException;

                await Task.CompletedTask;
                return IsAvailable;
            }

            public Task<string> GenerateSummaryAsync(JsonAnalysisResult analysis, string originalJson, SimilarityResult? similarityResult = null)
                => throw new NotImplementedException();
        }

        private class FakeEmbeddingService : IEmbeddingService
        {
            public bool IsAvailable { get; set; }
            public Exception? ThrowException { get; set; }

            public async Task<bool> IsEmbeddingServiceAvailableAsync()
            {
                if (ThrowException != null)
                    throw ThrowException;

                await Task.CompletedTask;
                return IsAvailable;
            }

            public string GetEmbeddingModelName() => "nomic-embed-text";

            public async Task<float[]> GenerateEmbeddingAsync(string text)
            {
                if (ThrowException != null)
                    throw ThrowException;

                await Task.CompletedTask;
                var embedding = new float[384];
                for (int i = 0; i < embedding.Length; i++)
                {
                    embedding[i] = (float)Random.Shared.NextDouble();
                }
                return embedding;
            }
        }

        private class FakeVectorDatabaseService : IVectorDatabaseService
        {
            public bool IsConnected { get; set; }
            public long EmbeddingCount { get; set; }
            public Exception? ThrowException { get; set; }

            public async Task<bool> IsConnectedAsync()
            {
                if (ThrowException != null)
                    throw ThrowException;

                await Task.CompletedTask;
                return IsConnected;
            }

            public async Task<long> GetEmbeddingCountAsync()
            {
                if (ThrowException != null)
                    throw ThrowException;

                await Task.CompletedTask;
                return EmbeddingCount;
            }

            public Task<bool> InitializeAsync() => Task.FromResult(true);

            public async Task<bool> StoreEmbeddingAsync(string id, float[] embedding, string jsonContent, string description, Dictionary<string, string>? metadata = null)
            {
                if (ThrowException != null)
                    throw ThrowException;

                await Task.CompletedTask;
                return true;
            }

            public async Task<List<SimilarityMatch>> FindSimilarAsync(float[] queryEmbedding, int maxResults = 5, float threshold = 0.7f)
            {
                if (ThrowException != null)
                    throw ThrowException;

                await Task.CompletedTask;
                return new List<SimilarityMatch>
                {
                    new SimilarityMatch
                    {
                        Id = "test1",
                        SimilarityScore = 0.95f,
                        Description = "Test match 1"
                    }
                };
            }

            public Task<bool> EmbeddingExistsAsync(string id) => Task.FromResult(true);

            public async Task<bool> DeleteEmbeddingAsync(string id)
            {
                if (ThrowException != null)
                    throw ThrowException;

                await Task.CompletedTask;
                return true;
            }

            public Task<int> DeleteAllEmbeddingsAsync() => Task.FromResult(0);
            public Task<List<string>> GetAllEmbeddingIdsAsync() => Task.FromResult(new List<string>());
            public Task DisposeAsync() => Task.CompletedTask;
        }

        private class FakeKnowledgeBaseService : IKnowledgeBaseService
        {
            public List<JsonExample>? Examples { get; set; }
            public Exception? ThrowException { get; set; }

            public async Task<List<JsonExample>> LoadExamplesAsync()
            {
                if (ThrowException != null)
                    throw ThrowException;

                await Task.CompletedTask;
                return Examples ?? new List<JsonExample>();
            }

            public async Task InitializeVectorDatabaseAsync()
            {
                if (ThrowException != null)
                    throw ThrowException;

                await Task.CompletedTask;
            }
        }

        private class FakeKnowledgeBaseManagementService : IKnowledgeBaseManagementService
        {
            public ReinitializeResult? ReinitializeResult { get; set; }
            public KnowledgeBaseValidationResult? ValidationResult { get; set; }
            public int ClearCount { get; set; }
            public List<string> JsonFiles { get; set; } = new List<string>();

            public Task<ReinitializeResult> ReinitializeAsync()
            {
                return Task.FromResult(ReinitializeResult ?? new ReinitializeResult());
            }

            public Task<KnowledgeBaseValidationResult> ValidateAsync()
            {
                return Task.FromResult(ValidationResult ?? new KnowledgeBaseValidationResult());
            }

            public Task<int> ClearAllEmbeddingsAsync()
            {
                return Task.FromResult(ClearCount);
            }

            public Task<List<string>> ScanJsonFilesAsync()
            {
                return Task.FromResult(JsonFiles);
            }
        }

        private class FakeConfigurationService : ConfigurationService
        {
            public FakeConfigurationService() : base(
                new Microsoft.Extensions.Configuration.ConfigurationBuilder().Build(),
                new TestLogger<ConfigurationService>())
            {
            }
        }

        #endregion
    }
}
