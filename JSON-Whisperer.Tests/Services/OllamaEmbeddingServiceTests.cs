using NUnit.Framework;
using Microsoft.Extensions.Logging;
using JSON_Whisperer.Services;
using JSON_Whisperer.Models;
using System.Net;
using System.Text;
using System.Text.Json;

namespace JSON_Whisperer.Tests.Services
{
    [TestFixture]
    public class OllamaEmbeddingServiceTests
    {
        private ILogger<OllamaEmbeddingService> _logger;
        private AppSettings _appSettings;
        private MockHttpMessageHandler _mockHttpHandler;
        private HttpClient _httpClient;
        private OllamaEmbeddingService _embeddingService;

        [SetUp]
        public void Setup()
        {
            _logger = new TestLogger<OllamaEmbeddingService>();
            _appSettings = new AppSettings
            {
                Ollama = new OllamaSettings
                {
                    BaseUrl = "http://localhost:11434",
                    EmbeddingModel = "nomic-embed-text"
                }
            };

            _mockHttpHandler = new MockHttpMessageHandler();
            _httpClient = new HttpClient(_mockHttpHandler)
            {
                BaseAddress = new Uri(_appSettings.Ollama.BaseUrl)
            };

            _embeddingService = new OllamaEmbeddingService(_httpClient, _logger, _appSettings);
        }

        [TearDown]
        public void TearDown()
        {
            _httpClient?.Dispose();
            _mockHttpHandler?.Dispose();
        }

        [Test]
        public async Task GenerateEmbeddingAsync_ValidJson_ReturnsEmbedding()
        {
            // Arrange
            var jsonContent = """{"name": "test", "value": 123}""";
            var expectedEmbedding = new float[] { 0.1f, 0.2f, 0.3f, 0.4f };
            
            var response = new OllamaEmbeddingResponse
            {
                Embeddings = new[] { expectedEmbedding },
                Model = "nomic-embed-text",
                TotalDuration = 1000000
            };

            _mockHttpHandler.SetupResponse("/api/embed", HttpStatusCode.OK, JsonSerializer.Serialize(response));

            // Act
            var result = await _embeddingService.GenerateEmbeddingAsync(jsonContent);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Length, Is.EqualTo(expectedEmbedding.Length));
            Assert.That(result, Is.EqualTo(expectedEmbedding));
        }

        [Test]
        public void GenerateEmbeddingAsync_NullInput_ThrowsArgumentException()
        {
            // Act & Assert
            Assert.ThrowsAsync<ArgumentException>(async () => 
                await _embeddingService.GenerateEmbeddingAsync(null));
        }

        [Test]
        public void GenerateEmbeddingAsync_EmptyInput_ThrowsArgumentException()
        {
            // Act & Assert
            Assert.ThrowsAsync<ArgumentException>(async () => 
                await _embeddingService.GenerateEmbeddingAsync(""));
        }

        [Test]
        public async Task GenerateEmbeddingAsync_HttpError_ThrowsInvalidOperationException()
        {
            // Arrange
            var jsonContent = """{"name": "test"}""";
            _mockHttpHandler.SetupResponse("/api/embed", HttpStatusCode.InternalServerError, "Server Error");

            // Act & Assert
            var ex = Assert.ThrowsAsync<InvalidOperationException>(async () => 
                await _embeddingService.GenerateEmbeddingAsync(jsonContent));
            
            Assert.That(ex.Message, Does.Contain("Failed to generate embedding"));
        }

        [Test]
        public async Task GenerateEmbeddingAsync_EmptyEmbeddingResponse_ThrowsInvalidOperationException()
        {
            // Arrange
            var jsonContent = """{"name": "test"}""";
            var response = new OllamaEmbeddingResponse
            {
                Embeddings = Array.Empty<float[]>(),
                Model = "nomic-embed-text"
            };

            _mockHttpHandler.SetupResponse("/api/embed", HttpStatusCode.OK, JsonSerializer.Serialize(response));

            // Act & Assert
            var ex = Assert.ThrowsAsync<InvalidOperationException>(async () => 
                await _embeddingService.GenerateEmbeddingAsync(jsonContent));
            
            Assert.That(ex.Message, Does.Contain("empty or null embeddings"));
        }

        [Test]
        public async Task IsEmbeddingServiceAvailableAsync_ServiceRunning_ReturnsTrue()
        {
            // Arrange
            var tagsResponse = new
            {
                models = new[]
                {
                    new { name = "nomic-embed-text:latest" },
                    new { name = "mistral:latest" }
                }
            };

            _mockHttpHandler.SetupResponse("/api/tags", HttpStatusCode.OK, JsonSerializer.Serialize(tagsResponse));

            // Act
            var result = await _embeddingService.IsEmbeddingServiceAvailableAsync();

            // Assert
            Assert.That(result, Is.True);
        }

        [Test]
        public async Task IsEmbeddingServiceAvailableAsync_ServiceDown_ReturnsFalse()
        {
            // Arrange
            _mockHttpHandler.SetupResponse("/api/tags", HttpStatusCode.ServiceUnavailable, "Service Unavailable");

            // Act
            var result = await _embeddingService.IsEmbeddingServiceAvailableAsync();

            // Assert
            Assert.That(result, Is.False);
        }

        [Test]
        public async Task IsEmbeddingServiceAvailableAsync_ModelNotAvailable_ReturnsFalse()
        {
            // Arrange
            var tagsResponse = new
            {
                models = new[]
                {
                    new { name = "mistral:latest" },
                    new { name = "llama2:latest" }
                }
            };

            _mockHttpHandler.SetupResponse("/api/tags", HttpStatusCode.OK, JsonSerializer.Serialize(tagsResponse));

            // Act
            var result = await _embeddingService.IsEmbeddingServiceAvailableAsync();

            // Assert
            Assert.That(result, Is.False);
        }

        [Test]
        public void GetEmbeddingModelName_ReturnsConfiguredModel()
        {
            // Act
            var result = _embeddingService.GetEmbeddingModelName();

            // Assert
            Assert.That(result, Is.EqualTo("nomic-embed-text"));
        }
    }

    // Mock HTTP message handler for testing
    public class MockHttpMessageHandler : HttpMessageHandler
    {
        private readonly Dictionary<string, (HttpStatusCode statusCode, string content)> _responses = new();

        public void SetupResponse(string path, HttpStatusCode statusCode, string content)
        {
            _responses[path] = (statusCode, content);
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var path = request.RequestUri?.AbsolutePath ?? "";
            
            if (_responses.TryGetValue(path, out var response))
            {
                return Task.FromResult(new HttpResponseMessage(response.statusCode)
                {
                    Content = new StringContent(response.content, Encoding.UTF8, "application/json")
                });
            }

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _responses.Clear();
            }
            base.Dispose(disposing);
        }
    }


}