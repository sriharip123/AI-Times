using System.Net.Http;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using JSON_Whisperer.Interfaces;
using JSON_Whisperer.Models;

namespace JSON_Whisperer.Services
{
    /// <summary>
    /// Service for generating vector embeddings using Ollama's embedding API
    /// </summary>
    public class OllamaEmbeddingService : IEmbeddingService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<OllamaEmbeddingService> _logger;
        private readonly AppSettings _appSettings;
        private readonly JsonSerializerOptions _jsonOptions;

        public OllamaEmbeddingService(
            HttpClient httpClient,
            ILogger<OllamaEmbeddingService> logger,
            AppSettings appSettings)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _appSettings = appSettings ?? throw new ArgumentNullException(nameof(appSettings));

            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = false
            };
        }

        /// <summary>
        /// Generates vector embedding for the given JSON content
        /// </summary>
        /// <param name="jsonContent">JSON content to embed</param>
        /// <returns>Vector embedding as float array</returns>
        public async Task<float[]> GenerateEmbeddingAsync(string jsonContent)
        {
            if (string.IsNullOrWhiteSpace(jsonContent))
            {
                throw new ArgumentException("JSON content cannot be null or empty", nameof(jsonContent));
            }

            try
            {
                _logger.LogDebug("Generating embedding for JSON content (length: {Length})", jsonContent.Length);

                // Prepare the embedding request
                var request = new OllamaEmbeddingRequest
                {
                    Model = _appSettings.Ollama.EmbeddingModel,
                    Input = jsonContent
                };

                var requestJson = JsonSerializer.Serialize(request, _jsonOptions);
                var content = new StringContent(requestJson, Encoding.UTF8, "application/json");

                // Make the API call to the correct endpoint
                var response = await _httpClient.PostAsync("/api/embed", content);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError("Embedding API request failed with status {StatusCode}: {Error}", 
                        response.StatusCode, errorContent);
                    throw new HttpRequestException($"Embedding API request failed: {response.StatusCode} - {errorContent}");
                }

                var responseJson = await response.Content.ReadAsStringAsync();
                var embeddingResponse = JsonSerializer.Deserialize<OllamaEmbeddingResponse>(responseJson, _jsonOptions);

                if (embeddingResponse?.Embeddings == null || embeddingResponse.Embeddings.Length == 0)
                {
                    _logger.LogError("Received empty or null embeddings from API");
                    throw new InvalidOperationException("Received empty or null embeddings from Ollama API");
                }

                // Get the first embedding (since we're sending single input)
                var embedding = embeddingResponse.Embeddings[0];
                if (embedding == null || embedding.Length == 0)
                {
                    _logger.LogError("Received empty embedding vector from API");
                    throw new InvalidOperationException("Received empty embedding vector from Ollama API");
                }

                _logger.LogDebug("Successfully generated embedding with {Dimensions} dimensions in {Duration}ms", 
                    embedding.Length, embeddingResponse.TotalDuration / 1_000_000);

                return embedding;
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "HTTP error while generating embedding");
                throw new InvalidOperationException($"Failed to generate embedding: {ex.Message}", ex);
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "JSON parsing error while generating embedding");
                throw new InvalidOperationException($"Failed to parse embedding response: {ex.Message}", ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while generating embedding");
                throw new InvalidOperationException($"Unexpected error generating embedding: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Checks if the embedding service is available and the embedding model is loaded
        /// </summary>
        /// <returns>True if service is available, false otherwise</returns>
        public async Task<bool> IsEmbeddingServiceAvailableAsync()
        {
            try
            {
                _logger.LogDebug("Checking embedding service availability");

                // Check if Ollama service is running by calling the tags endpoint
                var response = await _httpClient.GetAsync("/api/tags");
                
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("Ollama service is not responding (status: {StatusCode})", response.StatusCode);
                    return false;
                }

                var responseJson = await response.Content.ReadAsStringAsync();
                var tagsResponse = JsonSerializer.Deserialize<JsonElement>(responseJson);

                // Check if the embedding model is available
                if (tagsResponse.TryGetProperty("models", out var modelsElement))
                {
                    var models = modelsElement.EnumerateArray()
                        .Select(m => m.GetProperty("name").GetString())
                        .Where(name => !string.IsNullOrEmpty(name))
                        .ToList();

                    var embeddingModelAvailable = models.Any(model => 
                        model!.StartsWith(_appSettings.Ollama.EmbeddingModel, StringComparison.OrdinalIgnoreCase));

                    if (!embeddingModelAvailable)
                    {
                        _logger.LogWarning("Embedding model '{Model}' is not available. Available models: {Models}", 
                            _appSettings.Ollama.EmbeddingModel, string.Join(", ", models));
                        return false;
                    }

                    _logger.LogDebug("Embedding service is available with model '{Model}'", _appSettings.Ollama.EmbeddingModel);
                    return true;
                }

                _logger.LogWarning("Could not parse models list from Ollama response");
                return false;
            }
            catch (HttpRequestException ex)
            {
                _logger.LogWarning(ex, "HTTP error while checking embedding service availability");
                return false;
            }
            catch (JsonException ex)
            {
                _logger.LogWarning(ex, "JSON parsing error while checking embedding service availability");
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while checking embedding service availability");
                return false;
            }
        }

        /// <summary>
        /// Gets the name of the embedding model being used
        /// </summary>
        /// <returns>Embedding model name</returns>
        public string GetEmbeddingModelName()
        {
            return _appSettings.Ollama.EmbeddingModel;
        }
    }
}