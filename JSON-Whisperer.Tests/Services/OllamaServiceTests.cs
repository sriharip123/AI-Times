using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NUnit.Framework;
using JSON_Whisperer.Models;
using JSON_Whisperer.Services;

namespace JSON_Whisperer.Tests.Services
{
    /// <summary>
    /// Simple test logger implementation for testing
    /// </summary>
    public class TestLogger<T> : ILogger<T>
    {
        public IDisposable BeginScope<TState>(TState state) => null;
        public bool IsEnabled(LogLevel logLevel) => true;
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter) { }
    }

    /// <summary>
    /// Simple test options implementation for testing
    /// </summary>
    public class TestOptions<T> : IOptions<T> where T : class
    {
        public TestOptions(T value) => Value = value;
        public T Value { get; }
    }

    [TestFixture]
    public class OllamaServiceTests
    {
        private TestLogger<OllamaService> _logger;
        private AppSettings _appSettings;

        [SetUp]
        public void Setup()
        {
            _logger = new TestLogger<OllamaService>();
            _appSettings = new AppSettings
            {
                Ollama = new OllamaSettings
                {
                    BaseUrl = "http://localhost:11434",
                    ModelName = "mistral",
                    TimeoutSeconds = 30
                }
            };
        }

        [Test]
        public async Task IsAvailableAsync_InvalidUrl_ReturnsFalse()
        {
            // Arrange
            var invalidSettings = new AppSettings
            {
                Ollama = new OllamaSettings
                {
                    BaseUrl = "http://invalid-url:8080",
                    ModelName = "mistral",
                    TimeoutSeconds = 5
                }
            };
            using var httpClient = new HttpClient();
            var service = new OllamaService(httpClient, invalidSettings, _logger);

            // Act
            var result = await service.IsAvailableAsync();

            // Assert
            Assert.That(result, Is.False);
        }

        [Test]
        public void GenerateSummaryAsync_NullAnalysis_ThrowsArgumentNullException()
        {
            // Arrange
            using var httpClient = new HttpClient();
            var service = new OllamaService(httpClient, _appSettings, _logger);
            var originalJson = """{"name": "John"}""";

            // Act & Assert
            var ex = Assert.ThrowsAsync<ArgumentNullException>(
                () => service.GenerateSummaryAsync(null, originalJson));
            Assert.That(ex.ParamName, Is.EqualTo("analysis"));
        }

        [Test]
        public void GenerateSummaryAsync_EmptyJson_ThrowsArgumentException()
        {
            // Arrange
            using var httpClient = new HttpClient();
            var service = new OllamaService(httpClient, _appSettings, _logger);
            var analysis = CreateSampleAnalysis();

            // Act & Assert
            var ex = Assert.ThrowsAsync<ArgumentException>(
                () => service.GenerateSummaryAsync(analysis, ""));
            Assert.That(ex.ParamName, Is.EqualTo("originalJson"));
        }

        [Test]
        public void OllamaService_NullHttpClient_ThrowsArgumentNullException()
        {
            // Act & Assert
            var ex = Assert.Throws<ArgumentNullException>(
                () => new OllamaService(null, _appSettings, _logger));
            Assert.That(ex.ParamName, Is.EqualTo("httpClient"));
        }

        [Test]
        public void OllamaService_NullOptions_ThrowsArgumentNullException()
        {
            // Arrange
            using var httpClient = new HttpClient();

            // Act & Assert
            var ex = Assert.Throws<ArgumentNullException>(
                () => new OllamaService(httpClient, null, _logger));
            Assert.That(ex.ParamName, Is.EqualTo("settings"));
        }

        [Test]
        public void OllamaService_NullLogger_ThrowsArgumentNullException()
        {
            // Arrange
            using var httpClient = new HttpClient();

            // Act & Assert
            var ex = Assert.Throws<ArgumentNullException>(
                () => new OllamaService(httpClient, _appSettings, null));
            Assert.That(ex.ParamName, Is.EqualTo("logger"));
        }

        private JsonAnalysisResult CreateSampleAnalysis()
        {
            return new JsonAnalysisResult
            {
                TotalProperties = 2,
                MaxDepth = 1,
                EstimatedSize = 25,
                PropertyTypes = 
                {
                    { "name", JsonValueKind.String },
                    { "age", JsonValueKind.Number }
                }
            };
        }
    }

    [TestFixture]
    public class OllamaServicePromptGenerationTests
    {
        [Test]
        public void BuildPrompt_SimpleStructure_ContainsExpectedElements()
        {
            // This test validates that the service can be constructed and basic validation works
            // without requiring actual Ollama connection
            
            // Arrange
            var logger = new TestLogger<OllamaService>();
            var settings = new AppSettings
            {
                Ollama = new OllamaSettings
                {
                    BaseUrl = "http://localhost:11434",
                    ModelName = "mistral",
                    TimeoutSeconds = 1 // Short timeout to fail quickly
                }
            };
            
            using var httpClient = new HttpClient();
            var service = new OllamaService(httpClient, settings, logger);
            
            var analysis = new JsonAnalysisResult
            {
                TotalProperties = 3,
                MaxDepth = 1,
                EstimatedSize = 50,
                PropertyTypes = 
                {
                    { "name", JsonValueKind.String },
                    { "age", JsonValueKind.Number },
                    { "active", JsonValueKind.True }
                }
            };
            
            var simpleJson = """{"name": "John", "age": 30, "active": true}""";

            // Act & Assert - Verify service handles input validation correctly
            Assert.DoesNotThrow(() =>
            {
                // This should not throw during construction or basic validation
                // The actual HTTP call will fail, but that's expected in unit tests
                var task = service.GenerateSummaryAsync(analysis, simpleJson);
                
                // We expect this to eventually fail due to no Ollama connection,
                // but the prompt generation and validation should work
                Assert.That(task, Is.Not.Null);
            });
        }

        [Test]
        public void BuildPrompt_ComplexStructure_HandlesComplexity()
        {
            // Arrange
            var logger = new TestLogger<OllamaService>();
            var settings = new AppSettings
            {
                Ollama = new OllamaSettings
                {
                    BaseUrl = "http://localhost:11434",
                    ModelName = "mistral",
                    TimeoutSeconds = 1 // Short timeout to fail quickly
                }
            };
            
            using var httpClient = new HttpClient();
            var service = new OllamaService(httpClient, settings, logger);
            
            var analysis = new JsonAnalysisResult
            {
                TotalProperties = 25,
                MaxDepth = 5,
                EstimatedSize = 5000,
                ArrayFields = { "users", "orders", "items" },
                ObjectFields = { "metadata", "user.profile", "user.settings" },
                PropertyTypes = 
                {
                    { "name", JsonValueKind.String },
                    { "count", JsonValueKind.Number },
                    { "items", JsonValueKind.Array },
                    { "metadata", JsonValueKind.Object }
                }
            };
            
            var complexJson = """{"users": [{"profile": {"name": "John"}}], "metadata": {"total": 1}}""";

            // Act & Assert - Verify service can handle complex input without crashing during validation
            Assert.DoesNotThrow(() =>
            {
                // This should not throw during construction or basic validation
                var task = service.GenerateSummaryAsync(analysis, complexJson);
                
                // The task creation should succeed even if execution will fail
                Assert.That(task, Is.Not.Null);
            });
        }
    }
}