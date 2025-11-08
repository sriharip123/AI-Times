using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

using JSON_Whisperer.Interfaces;
using JSON_Whisperer.Models;

namespace JSON_Whisperer.Services
{
    /// <summary>
    /// Service for integrating with Ollama AI to generate JSON summaries
    /// </summary>
    public class OllamaService : IAiService
    {
        private readonly HttpClient _httpClient;
        private readonly AppSettings _settings;
        private readonly ILogger<OllamaService> _logger;
        private readonly JsonSerializerOptions _jsonOptions;

        public OllamaService(HttpClient httpClient, AppSettings settings, ILogger<OllamaService> logger)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = false
            };

            // Configure HttpClient base address and timeout
            _httpClient.BaseAddress = new Uri(_settings.Ollama.BaseUrl);
            _httpClient.Timeout = TimeSpan.FromSeconds(_settings.Ollama.TimeoutSeconds);
        }

        /// <summary>
        /// Checks if Ollama service is available and the specified model is loaded
        /// </summary>
        public async Task<bool> IsAvailableAsync()
        {
            try
            {
                _logger.LogInformation("Checking Ollama service availability at {BaseUrl}", _settings.Ollama.BaseUrl);
                
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10)); // Shorter timeout for availability check
                
                // First check if Ollama service is running
                var response = await _httpClient.GetAsync("/api/tags", cts.Token);
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("Ollama service returned status code: {StatusCode}", response.StatusCode);
                    return false;
                }

                var content = await response.Content.ReadAsStringAsync();
                _logger.LogDebug("Ollama tags response: {Content}", content);

                // Check if our specific model is available
                return await IsModelAvailableAsync();
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Failed to connect to Ollama service at {BaseUrl}. Please ensure Ollama is running and accessible.", _settings.Ollama.BaseUrl);
                return false;
            }
            catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
            {
                _logger.LogError(ex, "Timeout connecting to Ollama service. The service may be slow to respond or unavailable.");
                return false;
            }
            catch (TaskCanceledException ex)
            {
                _logger.LogError(ex, "Request to Ollama service was cancelled");
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error checking Ollama availability");
                return false;
            }
        }

        /// <summary>
        /// Generates a plain English summary of JSON data using the configured AI model
        /// </summary>
        public async Task<string> GenerateSummaryAsync(JsonAnalysisResult analysis, string originalJson, SimilarityResult? similarityResult = null)
        {
            if (analysis == null)
                throw new ArgumentNullException(nameof(analysis));
            
            if (string.IsNullOrEmpty(originalJson))
                throw new ArgumentException("Original JSON cannot be null or empty", nameof(originalJson));

            return await ExecuteWithRetryAsync(async () =>
            {
                _logger.LogInformation("Generating summary for JSON with {PropertyCount} properties", analysis.TotalProperties);

                if (similarityResult != null && similarityResult.Matches.Count > 0)
                {
                    _logger.LogInformation("Using {MatchCount} similar examples for context enhancement", similarityResult.Matches.Count);
                }

                var prompt = BuildPrompt(analysis, originalJson, similarityResult);
                var request = new OllamaRequest
                {
                    Model = _settings.Ollama.ModelName,
                    Prompt = prompt,
                    Stream = false
                };

                var requestJson = JsonSerializer.Serialize(request, _jsonOptions);
                var content = new StringContent(requestJson, Encoding.UTF8, "application/json");

                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(_settings.Ollama.TimeoutSeconds));
                var response = await _httpClient.PostAsync("/api/generate", content, cts.Token);
                
                await HandleHttpResponseAsync(response);

                var responseContent = await response.Content.ReadAsStringAsync();
                var ollamaResponse = JsonSerializer.Deserialize<OllamaResponse>(responseContent, _jsonOptions);

                if (ollamaResponse?.Response == null)
                {
                    _logger.LogError("Received null or empty response from Ollama");
                    throw new InvalidOperationException("Received invalid response from Ollama service");
                }

                _logger.LogInformation("Successfully generated summary of {Length} characters", ollamaResponse.Response.Length);
                return ollamaResponse.Response.Trim();
            });
        }

        /// <summary>
        /// Checks if the specified model is available in Ollama
        /// </summary>
        private async Task<bool> IsModelAvailableAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync("/api/tags");
                if (!response.IsSuccessStatusCode)
                    return false;

                var content = await response.Content.ReadAsStringAsync();
                
                // Simple check - if the response contains our model name, assume it's available
                // In a production system, you'd parse the JSON response properly
                return content.Contains(_settings.Ollama.ModelName, StringComparison.OrdinalIgnoreCase);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking model availability");
                return false;
            }
        }

        /// <summary>
        /// Executes an operation with exponential backoff retry logic
        /// </summary>
        private async Task<T> ExecuteWithRetryAsync<T>(Func<Task<T>> operation, int? maxRetries = null)
        {
            var actualMaxRetries = maxRetries ?? _settings.Ollama.RetryAttempts;
            var delay = TimeSpan.FromSeconds(_settings.Ollama.RetryDelaySeconds);
            Exception? lastException = null;

            for (int attempt = 0; attempt <= actualMaxRetries; attempt++)
            {
                try
                {
                    return await operation();
                }
                catch (HttpRequestException ex) when (attempt < actualMaxRetries)
                {
                    lastException = ex;
                    _logger.LogWarning("HTTP request failed on attempt {Attempt}/{MaxRetries}. Retrying in {Delay}ms. Error: {Error}", 
                        attempt + 1, actualMaxRetries + 1, delay.TotalMilliseconds, ex.Message);
                    
                    await Task.Delay(delay);
                    delay = TimeSpan.FromMilliseconds(delay.TotalMilliseconds * 2); // Exponential backoff
                }
                catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException && attempt < actualMaxRetries)
                {
                    lastException = ex;
                    _logger.LogWarning("Request timeout on attempt {Attempt}/{MaxRetries}. Retrying in {Delay}ms", 
                        attempt + 1, actualMaxRetries + 1, delay.TotalMilliseconds);
                    
                    await Task.Delay(delay);
                    delay = TimeSpan.FromMilliseconds(delay.TotalMilliseconds * 2);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Non-retryable error occurred during operation");
                    throw;
                }
            }

            _logger.LogError(lastException, "Operation failed after {MaxRetries} retries", actualMaxRetries + 1);
            throw new InvalidOperationException($"Operation failed after {actualMaxRetries + 1} attempts. See inner exception for details.", lastException);
        }

        /// <summary>
        /// Handles HTTP response and provides informative error messages
        /// </summary>
        private async Task HandleHttpResponseAsync(HttpResponseMessage response)
        {
            if (response.IsSuccessStatusCode)
                return;

            var errorContent = await response.Content.ReadAsStringAsync();
            
            var errorMessage = response.StatusCode switch
            {
                HttpStatusCode.NotFound => $"Ollama API endpoint not found. Please ensure Ollama is running and the API is accessible at {_settings.Ollama.BaseUrl}",
                HttpStatusCode.BadRequest => $"Invalid request to Ollama API. The model '{_settings.Ollama.ModelName}' may not be available. Error: {errorContent}",
                HttpStatusCode.InternalServerError => "Ollama service encountered an internal error. Please check the Ollama service logs.",
                HttpStatusCode.ServiceUnavailable => "Ollama service is temporarily unavailable. Please try again later.",
                HttpStatusCode.RequestTimeout => "Request to Ollama service timed out. The model may be loading or the service may be overloaded.",
                _ => $"Ollama API returned error {response.StatusCode}: {errorContent}"
            };

            _logger.LogError("Ollama API error: {StatusCode} - {ErrorMessage}", response.StatusCode, errorMessage);
            
            if (response.StatusCode == HttpStatusCode.BadRequest && errorContent.Contains("model") && errorContent.Contains("not found"))
            {
                throw new InvalidOperationException($"The model '{_settings.Ollama.ModelName}' is not available in Ollama. Please install it using: ollama pull {_settings.Ollama.ModelName}");
            }

            throw new HttpRequestException(errorMessage);
        }

        /// <summary>
        /// Builds an intelligent prompt incorporating JSON analysis metadata and similarity context
        /// </summary>
        private string BuildPrompt(JsonAnalysisResult analysis, string originalJson, SimilarityResult? similarityResult = null)
        {
            var promptBuilder = new StringBuilder();
            
            // Start with clear instructions for business-friendly output
            promptBuilder.AppendLine("You are a data analyst helping business users understand JSON data structures.");
            promptBuilder.AppendLine("Analyze the following JSON data and provide a clear, business-friendly summary in plain English.");
            promptBuilder.AppendLine("Avoid technical jargon and focus on what the data means from a business perspective.");
            promptBuilder.AppendLine();

            // Add similarity context if available
            if (similarityResult != null && similarityResult.Matches.Count > 0)
            {
                promptBuilder.AppendLine("Similar Data Examples Found:");
                promptBuilder.AppendLine("The following similar JSON structures have been analyzed before. Use these as context to improve your analysis:");
                promptBuilder.AppendLine();

                foreach (var match in similarityResult.Matches.Take(3)) // Limit to top 3 matches for prompt efficiency
                {
                    promptBuilder.AppendLine($"Similar Example (Similarity: {match.SimilarityScore:P1}):");
                    promptBuilder.AppendLine($"Description: {match.Description}");
                    
                    // Include a truncated version of the similar JSON for context
                    var similarJsonPreview = GetSimilarJsonPreview(match.JsonContent);
                    promptBuilder.AppendLine($"Structure: {similarJsonPreview}");
                    promptBuilder.AppendLine();
                }

                promptBuilder.AppendLine("Use these similar examples to provide better context and insights in your analysis.");
                promptBuilder.AppendLine("If the current JSON appears to be similar to any of these examples, mention the relationship and build upon the existing descriptions.");
                promptBuilder.AppendLine();
            }
            
            // Add intelligent context based on analysis
            promptBuilder.AppendLine("Data Structure Analysis:");
            promptBuilder.AppendLine($"- This JSON contains {analysis.TotalProperties} total properties");
            promptBuilder.AppendLine($"- Data complexity: {GetComplexityDescription(analysis)}");
            promptBuilder.AppendLine($"- Data size: {GetSizeDescription(analysis.EstimatedSize)}");
            
            // Provide context about data relationships
            if (analysis.ArrayFields.Count > 0 || analysis.ObjectFields.Count > 0)
            {
                promptBuilder.AppendLine();
                promptBuilder.AppendLine("Data Relationships:");
                
                if (analysis.ArrayFields.Count > 0)
                {
                    promptBuilder.AppendLine($"- Contains lists/collections: {string.Join(", ", analysis.ArrayFields)}");
                }
                
                if (analysis.ObjectFields.Count > 0)
                {
                    promptBuilder.AppendLine($"- Contains nested information groups: {string.Join(", ", analysis.ObjectFields)}");
                }
            }

            // Add data type insights
            if (analysis.PropertyTypes.Count > 0)
            {
                promptBuilder.AppendLine();
                promptBuilder.AppendLine("Data Types Present:");
                var typeGroups = analysis.PropertyTypes.GroupBy(kvp => kvp.Value)
                    .Select(g => $"- {GetBusinessFriendlyTypeName(g.Key)}: {g.Count()} fields")
                    .ToList();
                
                foreach (var typeGroup in typeGroups)
                {
                    promptBuilder.AppendLine(typeGroup);
                }
            }
            
            promptBuilder.AppendLine();
            promptBuilder.AppendLine("JSON Data to Analyze:");
            
            // Intelligently truncate JSON based on complexity
            var jsonForPrompt = GetOptimalJsonForPrompt(originalJson, analysis);
            promptBuilder.AppendLine(jsonForPrompt);
            
            promptBuilder.AppendLine();
            promptBuilder.AppendLine("Please provide a comprehensive summary that includes:");
            promptBuilder.AppendLine("1. What this data likely represents (e.g., customer records, product catalog, transaction log)");
            promptBuilder.AppendLine("2. The main categories of information contained in the data");
            promptBuilder.AppendLine("3. Key business insights about the data structure and relationships");
            promptBuilder.AppendLine("4. How this data might be used in a business context");
            promptBuilder.AppendLine("5. Any notable patterns or characteristics that stand out");
            
            if (similarityResult != null && similarityResult.Matches.Count > 0)
            {
                promptBuilder.AppendLine("6. How this data relates to or differs from the similar examples provided above");
            }
            
            promptBuilder.AppendLine();
            promptBuilder.AppendLine("Format your response in clear paragraphs suitable for business stakeholders.");
            
            return promptBuilder.ToString();
        }

        /// <summary>
        /// Gets a business-friendly description of data complexity
        /// </summary>
        private string GetComplexityDescription(JsonAnalysisResult analysis)
        {
            if (analysis.MaxDepth <= 1 && analysis.TotalProperties <= 5)
                return "Simple (flat structure with few fields)";
            
            if (analysis.MaxDepth <= 2 && analysis.TotalProperties <= 15)
                return "Moderate (some nesting with manageable field count)";
            
            if (analysis.MaxDepth <= 4 && analysis.TotalProperties <= 50)
                return "Complex (multiple levels of nesting)";
            
            return "Highly Complex (deep nesting with many fields)";
        }

        /// <summary>
        /// Gets a business-friendly description of data size
        /// </summary>
        private string GetSizeDescription(int sizeInBytes)
        {
            return sizeInBytes switch
            {
                < 1024 => "Small (less than 1KB)",
                < 10240 => "Medium (1-10KB)",
                < 102400 => "Large (10-100KB)",
                _ => "Very Large (over 100KB)"
            };
        }

        /// <summary>
        /// Converts JSON value types to business-friendly names
        /// </summary>
        private string GetBusinessFriendlyTypeName(JsonValueKind valueKind)
        {
            return valueKind switch
            {
                JsonValueKind.String => "Text/Labels",
                JsonValueKind.Number => "Numbers/Quantities",
                JsonValueKind.True or JsonValueKind.False => "Yes/No Flags",
                JsonValueKind.Array => "Lists/Collections",
                JsonValueKind.Object => "Information Groups",
                JsonValueKind.Null => "Empty Values",
                _ => "Mixed Data"
            };
        }

        /// <summary>
        /// Optimally truncates JSON for prompt based on analysis
        /// </summary>
        private string GetOptimalJsonForPrompt(string originalJson, JsonAnalysisResult analysis)
        {
            // For simple structures, include more of the JSON
            var maxLength = analysis.MaxDepth <= 2 ? 3000 : 2000;
            
            // For very complex structures, be more aggressive with truncation
            if (analysis.TotalProperties > 50)
                maxLength = 1500;
            
            if (originalJson.Length <= maxLength)
                return originalJson;
            
            // Try to truncate at a logical boundary (end of object/array)
            var truncated = originalJson.Substring(0, maxLength);
            var lastBrace = Math.Max(truncated.LastIndexOf('}'), truncated.LastIndexOf(']'));
            
            if (lastBrace > maxLength * 0.7) // If we found a good boundary
            {
                truncated = originalJson.Substring(0, lastBrace + 1);
            }
            
            return truncated + "\n... [remaining data truncated for analysis]";
        }

        /// <summary>
        /// Creates a concise preview of similar JSON for context in prompts
        /// </summary>
        private string GetSimilarJsonPreview(string jsonContent)
        {
            const int maxPreviewLength = 200;
            
            try
            {
                // Try to parse and get key structure information
                using var document = JsonDocument.Parse(jsonContent);
                var root = document.RootElement;
                
                if (root.ValueKind == JsonValueKind.Object)
                {
                    var properties = root.EnumerateObject().Take(5).Select(p => p.Name);
                    return $"Object with properties: {string.Join(", ", properties)}{(root.GetRawText().Length > maxPreviewLength ? "..." : "")}";
                }
                else if (root.ValueKind == JsonValueKind.Array && root.GetArrayLength() > 0)
                {
                    var firstElement = root[0];
                    if (firstElement.ValueKind == JsonValueKind.Object)
                    {
                        var properties = firstElement.EnumerateObject().Take(3).Select(p => p.Name);
                        return $"Array of objects with properties: {string.Join(", ", properties)}...";
                    }
                    return $"Array of {root.GetArrayLength()} {firstElement.ValueKind} elements";
                }
            }
            catch (JsonException)
            {
                // Fallback to simple truncation if JSON parsing fails
            }
            
            // Fallback: simple truncation
            if (jsonContent.Length <= maxPreviewLength)
                return jsonContent;
            
            return jsonContent.Substring(0, maxPreviewLength) + "...";
        }
    }
}