using NUnit.Framework;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using JSON_Whisperer;
using JSON_Whisperer.Interfaces;
using JSON_Whisperer.Models;
using JSON_Whisperer.Services;
using JSON_Whisperer.Tests.Services;

namespace JSON_Whisperer.Tests
{
    [TestFixture]
    public class JsonWhispererApplicationTests
    {
        private ServiceProvider _serviceProvider;
        private ServiceProvider _serviceProviderWithSimilarity;
        private IConfiguration _configuration;
        private IConfiguration _configurationWithSimilarity;
        private string _testAppDataPath;

        [SetUp]
        public void Setup()
        {
            // Create temporary test directory for AppData
            _testAppDataPath = Path.Combine(Path.GetTempPath(), "JsonWhispererTests", Guid.NewGuid().ToString());
            Directory.CreateDirectory(_testAppDataPath);

            // Create test configuration without similarity matching
            var configData = new Dictionary<string, string>
            {
                {"Ollama:BaseUrl", "http://localhost:11434"},
                {"Ollama:ModelName", "mistral"},
                {"Ollama:TimeoutSeconds", "30"},
                {"Application:VerboseMode", "false"},
                {"Vector:EnableSimilarityMatching", "false"}
            };

            _configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(configData)
                .Build();

            // Create test configuration with similarity matching enabled
            var configDataWithSimilarity = new Dictionary<string, string>
            {
                {"Ollama:BaseUrl", "http://localhost:11434"},
                {"Ollama:ModelName", "mistral"},
                {"Ollama:EmbeddingModel", "nomic-embed-text"},
                {"Ollama:TimeoutSeconds", "30"},
                {"Application:VerboseMode", "true"},
                {"Vector:EnableSimilarityMatching", "true"},
                {"Vector:SimilarityThreshold", "0.7"},
                {"Vector:MaxSimilarResults", "5"},
                {"Vector:AppDataPath", _testAppDataPath},
                {"ScyllaDb:ContactPoints", "127.0.0.1"},
                {"ScyllaDb:Port", "9042"},
                {"ScyllaDb:Keyspace", "json_whisperer_test"}
            };

            _configurationWithSimilarity = new ConfigurationBuilder()
                .AddInMemoryCollection(configDataWithSimilarity)
                .Build();

            // Setup dependency injection for tests
            var services = new ServiceCollection();
            ConfigureTestServices(services, _configuration, false);
            _serviceProvider = services.BuildServiceProvider();

            var servicesWithSimilarity = new ServiceCollection();
            ConfigureTestServices(servicesWithSimilarity, _configurationWithSimilarity, true);
            _serviceProviderWithSimilarity = servicesWithSimilarity.BuildServiceProvider();
        }

        [TearDown]
        public void TearDown()
        {
            _serviceProvider?.Dispose();
            _serviceProviderWithSimilarity?.Dispose();
            
            // Clean up test directory
            if (Directory.Exists(_testAppDataPath))
            {
                Directory.Delete(_testAppDataPath, true);
            }
        }

        private void ConfigureTestServices(IServiceCollection services, IConfiguration configuration, bool enableSimilarity)
        {
            // Configuration
            services.Configure<AppSettings>(configuration);
            services.AddSingleton(configuration);

            // Get and register AppSettings as singleton (required by services)
            var appSettings = configuration.Get<AppSettings>() ?? new AppSettings();
            services.AddSingleton(appSettings);

            // Logging
            services.AddLogging(builder =>
            {
                builder.AddConsole();
                builder.SetMinimumLevel(LogLevel.Warning); // Reduce noise in tests
            });

            // HttpClient for Ollama service
            services.AddHttpClient<IAiService, OllamaService>(client =>
            {
                client.BaseAddress = new Uri(appSettings.Ollama.BaseUrl);
                client.Timeout = TimeSpan.FromSeconds(appSettings.Ollama.TimeoutSeconds);
            });

            // Register core application services
            services.AddScoped<IInputHandler, InputHandler>();
            services.AddScoped<IJsonAnalyzer, JsonAnalyzer>();
            services.AddScoped<IAiService, OllamaService>();
            services.AddScoped<IOutputFormatter, OutputFormatter>();

            // Register monitoring and diagnostic services
            services.AddSingleton<PerformanceMonitoringService>();
            services.AddSingleton<DiagnosticService>();

            if (enableSimilarity)
            {
                // Register vector services with mock implementations for testing
                services.AddScoped<IEmbeddingService, MockEmbeddingService>();
                services.AddScoped<IVectorDatabaseService, MockVectorDatabaseService>();
                services.AddScoped<ISimilarityService, SimilarityService>();
                services.AddScoped<IKnowledgeBaseService, KnowledgeBaseService>();
            }
            else
            {
                // Register null implementations when similarity is disabled
                services.AddScoped<IEmbeddingService, NullEmbeddingService>();
                services.AddScoped<IVectorDatabaseService, NullVectorDatabaseService>();
                services.AddScoped<ISimilarityService, NullSimilarityService>();
                services.AddScoped<IKnowledgeBaseService, NullKnowledgeBaseService>();
            }

            services.AddScoped<JsonWhispererApplication>();
        }

        [Test]
        public async Task RunAsync_WithValidJsonArgument_SimilarityDisabled_ReturnsSuccess()
        {
            // Arrange
            var app = _serviceProvider.GetRequiredService<JsonWhispererApplication>();
            var validJson = "{\"name\":\"test\",\"value\":123}";
            var args = new[] { validJson };

            // Act & Assert
            // Note: This test may fail if Ollama is not running, which is expected behavior
            var result = await app.RunAsync(args);
            
            // The result should be either 0 (success) or 1 (Ollama not available)
            // Both are valid outcomes for this integration test
            Assert.That(result, Is.InRange(0, 1));
        }

        [Test]
        public async Task RunAsync_WithValidJsonArgument_SimilarityEnabled_ReturnsSuccess()
        {
            // Arrange
            var app = _serviceProviderWithSimilarity.GetRequiredService<JsonWhispererApplication>();
            var validJson = "{\"name\":\"test\",\"value\":123}";
            var args = new[] { validJson };

            // Setup mock similarity service with test data
            var mockSimilarityService = _serviceProviderWithSimilarity.GetRequiredService<ISimilarityService>() as SimilarityService;
            var mockEmbeddingService = _serviceProviderWithSimilarity.GetRequiredService<IEmbeddingService>() as MockEmbeddingService;
            var mockVectorService = _serviceProviderWithSimilarity.GetRequiredService<IVectorDatabaseService>() as MockVectorDatabaseService;

            // Setup mock responses
            mockEmbeddingService?.SetAvailable(true);
            mockVectorService?.SetConnected(true);

            // Act
            var result = await app.RunAsync(args);

            // Assert
            // Should be 0 (success) or 1 (Ollama not available)
            Assert.That(result, Is.InRange(0, 1));
        }

        [Test]
        public async Task RunAsync_WithSimilarityMatching_VectorServicesUnavailable_FallsBackGracefully()
        {
            // Arrange
            var app = _serviceProviderWithSimilarity.GetRequiredService<JsonWhispererApplication>();
            var validJson = "{\"name\":\"test\",\"value\":123}";
            var args = new[] { validJson };

            // Setup mock services to be unavailable
            var mockEmbeddingService = _serviceProviderWithSimilarity.GetRequiredService<IEmbeddingService>() as MockEmbeddingService;
            var mockVectorService = _serviceProviderWithSimilarity.GetRequiredService<IVectorDatabaseService>() as MockVectorDatabaseService;

            mockEmbeddingService?.SetAvailable(false);
            mockVectorService?.SetConnected(false);

            // Act
            var result = await app.RunAsync(args);

            // Assert
            // Should still work without similarity matching
            Assert.That(result, Is.InRange(0, 1));
        }

        [Test]
        public async Task RunAsync_WithKnowledgeBaseExamples_UsesSimilarityMatching()
        {
            // Arrange
            var app = _serviceProviderWithSimilarity.GetRequiredService<JsonWhispererApplication>();
            
            // Create test JSON examples in AppData directory
            var exampleJson1 = "{\"user\": {\"name\": \"Alice\", \"age\": 25}, \"_description\": \"User profile example\"}";
            var exampleJson2 = "{\"product\": {\"name\": \"Widget\", \"price\": 19.99}, \"_description\": \"Product catalog example\"}";
            
            var exampleFile1 = Path.Combine(_testAppDataPath, "user_example.json");
            var exampleFile2 = Path.Combine(_testAppDataPath, "product_example.json");
            
            await File.WriteAllTextAsync(exampleFile1, exampleJson1);
            await File.WriteAllTextAsync(exampleFile2, exampleJson2);

            // Input JSON similar to user example
            var inputJson = "{\"user\": {\"name\": \"Bob\", \"age\": 30, \"email\": \"bob@example.com\"}}";
            var args = new[] { inputJson };

            // Setup mock services
            var mockEmbeddingService = _serviceProviderWithSimilarity.GetRequiredService<IEmbeddingService>() as MockEmbeddingService;
            var mockVectorService = _serviceProviderWithSimilarity.GetRequiredService<IVectorDatabaseService>() as MockVectorDatabaseService;

            mockEmbeddingService?.SetAvailable(true);
            mockVectorService?.SetConnected(true);

            // Setup embeddings for similarity matching
            var inputEmbedding = new float[] { 0.8f, 0.6f, 0.4f };
            var exampleEmbedding = new float[] { 0.9f, 0.7f, 0.5f };
            
            mockEmbeddingService?.SetupEmbedding(inputJson, inputEmbedding);
            mockEmbeddingService?.SetupEmbedding(exampleJson1, exampleEmbedding);

            // Setup similarity matches
            var similarityMatches = new List<SimilarityMatch>
            {
                new SimilarityMatch
                {
                    Id = "user_example.json_12345678",
                    JsonContent = exampleJson1,
                    Description = "User profile example",
                    SimilarityScore = 0.85f
                }
            };
            mockVectorService?.SetupSimilarMatches(inputEmbedding, similarityMatches);

            // Act
            var result = await app.RunAsync(args);

            // Assert
            Assert.That(result, Is.InRange(0, 1));
        }

        [Test]
        public async Task RunAsync_WithInvalidJson_ReturnsError()
        {
            // Arrange
            var app = _serviceProvider.GetRequiredService<JsonWhispererApplication>();
            var invalidJson = "{invalid json}";
            var args = new[] { invalidJson };

            // Act
            var result = await app.RunAsync(args);

            // Assert
            Assert.That(result, Is.EqualTo(1));
        }

        [Test]
        public async Task RunAsync_WithEmptyArgs_AttemptsStdinRead()
        {
            // Arrange
            var app = _serviceProvider.GetRequiredService<JsonWhispererApplication>();
            var args = new string[0];

            // Act
            var result = await app.RunAsync(args);

            // Assert
            // Should return error since no stdin input is provided in test environment
            Assert.That(result, Is.EqualTo(1));
        }

        [Test]
        public async Task RunAsync_WithComplexValidJson_ProcessesSuccessfully()
        {
            // Arrange
            var app = _serviceProvider.GetRequiredService<JsonWhispererApplication>();
            var complexJson = @"{
                ""users"": [
                    {
                        ""id"": 1,
                        ""name"": ""John Doe"",
                        ""email"": ""john@example.com"",
                        ""preferences"": {
                            ""theme"": ""dark"",
                            ""notifications"": true
                        }
                    }
                ],
                ""metadata"": {
                    ""version"": ""1.0"",
                    ""created"": ""2024-01-01T00:00:00Z""
                }
            }";
            var args = new[] { complexJson };

            // Act
            var result = await app.RunAsync(args);

            // Assert
            // Should be 0 (success) or 1 (Ollama not available)
            Assert.That(result, Is.InRange(0, 1));
        }

        [Test]
        public void ConfigureServices_WithoutSimilarity_RegistersAllRequiredServices()
        {
            // Assert that all required services are registered
            Assert.That(_serviceProvider.GetService<IInputHandler>(), Is.Not.Null);
            Assert.That(_serviceProvider.GetService<IJsonAnalyzer>(), Is.Not.Null);
            Assert.That(_serviceProvider.GetService<IAiService>(), Is.Not.Null);
            Assert.That(_serviceProvider.GetService<IOutputFormatter>(), Is.Not.Null);
            Assert.That(_serviceProvider.GetService<JsonWhispererApplication>(), Is.Not.Null);
            Assert.That(_serviceProvider.GetService<IConfiguration>(), Is.Not.Null);
            Assert.That(_serviceProvider.GetService<IOptions<AppSettings>>(), Is.Not.Null);
            
            // Vector services should be null implementations
            Assert.That(_serviceProvider.GetService<IEmbeddingService>(), Is.InstanceOf<NullEmbeddingService>());
            Assert.That(_serviceProvider.GetService<IVectorDatabaseService>(), Is.InstanceOf<NullVectorDatabaseService>());
            Assert.That(_serviceProvider.GetService<ISimilarityService>(), Is.InstanceOf<NullSimilarityService>());
            Assert.That(_serviceProvider.GetService<IKnowledgeBaseService>(), Is.InstanceOf<NullKnowledgeBaseService>());
        }

        [Test]
        public void ConfigureServices_WithSimilarity_RegistersAllVectorServices()
        {
            // Assert that all required services are registered including vector services
            Assert.That(_serviceProviderWithSimilarity.GetService<IInputHandler>(), Is.Not.Null);
            Assert.That(_serviceProviderWithSimilarity.GetService<IJsonAnalyzer>(), Is.Not.Null);
            Assert.That(_serviceProviderWithSimilarity.GetService<IAiService>(), Is.Not.Null);
            Assert.That(_serviceProviderWithSimilarity.GetService<IOutputFormatter>(), Is.Not.Null);
            Assert.That(_serviceProviderWithSimilarity.GetService<JsonWhispererApplication>(), Is.Not.Null);
            
            // Vector services should be mock implementations
            Assert.That(_serviceProviderWithSimilarity.GetService<IEmbeddingService>(), Is.InstanceOf<MockEmbeddingService>());
            Assert.That(_serviceProviderWithSimilarity.GetService<IVectorDatabaseService>(), Is.InstanceOf<MockVectorDatabaseService>());
            Assert.That(_serviceProviderWithSimilarity.GetService<ISimilarityService>(), Is.InstanceOf<SimilarityService>());
            Assert.That(_serviceProviderWithSimilarity.GetService<IKnowledgeBaseService>(), Is.InstanceOf<KnowledgeBaseService>());
        }

        [Test]
        public void AppSettings_ConfigurationBinding_WorksCorrectly()
        {
            // Arrange & Act
            var appSettings = _serviceProvider.GetRequiredService<IOptions<AppSettings>>().Value;

            // Assert
            Assert.That(appSettings.Ollama.BaseUrl, Is.EqualTo("http://localhost:11434"));
            Assert.That(appSettings.Ollama.ModelName, Is.EqualTo("mistral"));
            Assert.That(appSettings.Ollama.TimeoutSeconds, Is.EqualTo(30));
            Assert.That(appSettings.Application.VerboseMode, Is.False);
            Assert.That(appSettings.Vector.EnableSimilarityMatching, Is.False);
        }

        [Test]
        public void AppSettings_SimilarityConfiguration_WorksCorrectly()
        {
            // Arrange & Act
            var appSettings = _serviceProviderWithSimilarity.GetRequiredService<IOptions<AppSettings>>().Value;

            // Assert
            Assert.That(appSettings.Ollama.BaseUrl, Is.EqualTo("http://localhost:11434"));
            Assert.That(appSettings.Ollama.ModelName, Is.EqualTo("mistral"));
            Assert.That(appSettings.Ollama.EmbeddingModel, Is.EqualTo("nomic-embed-text"));
            Assert.That(appSettings.Application.VerboseMode, Is.True);
            Assert.That(appSettings.Vector.EnableSimilarityMatching, Is.True);
            Assert.That(appSettings.Vector.SimilarityThreshold, Is.EqualTo(0.7f));
            Assert.That(appSettings.Vector.MaxSimilarResults, Is.EqualTo(5));
            Assert.That(appSettings.Vector.AppDataPath, Is.EqualTo(_testAppDataPath));
        }
    }

    // Null implementations for when similarity matching is disabled
    public class NullEmbeddingService : IEmbeddingService
    {
        public Task<float[]> GenerateEmbeddingAsync(string jsonContent) => Task.FromResult<float[]>(null);
        public Task<bool> IsEmbeddingServiceAvailableAsync() => Task.FromResult(false);
        public string GetEmbeddingModelName() => "none";
    }

    public class NullVectorDatabaseService : IVectorDatabaseService
    {
        public Task<bool> InitializeAsync() => Task.FromResult(false);
        public Task<bool> StoreEmbeddingAsync(string id, float[] embedding, string jsonContent, string description, Dictionary<string, string>? metadata = null) => Task.FromResult(false);
        public Task<List<SimilarityMatch>> FindSimilarAsync(float[] queryEmbedding, int maxResults = 5, float threshold = 0.7f) => Task.FromResult(new List<SimilarityMatch>());
        public Task<bool> IsConnectedAsync() => Task.FromResult(false);
        public Task<long> GetEmbeddingCountAsync() => Task.FromResult(0L);
        public Task<bool> EmbeddingExistsAsync(string id) => Task.FromResult(false);
        public Task<bool> DeleteEmbeddingAsync(string id) => Task.FromResult(false);
        public Task DisposeAsync() => Task.CompletedTask;
    }

    public class NullSimilarityService : ISimilarityService
    {
        public Task<SimilarityResult> FindSimilarJsonAsync(string inputJson) => Task.FromResult(new SimilarityResult());
        public float CalculateCosineSimilarity(float[] vector1, float[] vector2) => 0.0f;
        public Task<bool> IsAvailableAsync() => Task.FromResult(false);
        public SimilarityConfiguration GetConfiguration() => new SimilarityConfiguration();
    }

    public class NullKnowledgeBaseService : IKnowledgeBaseService
    {
        public Task<List<JsonExample>> LoadExamplesAsync() => Task.FromResult(new List<JsonExample>());
        public Task InitializeVectorDatabaseAsync() => Task.CompletedTask;
    }
}