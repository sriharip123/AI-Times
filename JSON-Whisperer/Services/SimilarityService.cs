using Microsoft.Extensions.Logging;
using JSON_Whisperer.Interfaces;
using JSON_Whisperer.Models;
using System.Diagnostics;

namespace JSON_Whisperer.Services
{
    /// <summary>
    /// Service for finding similar JSON patterns using vector embeddings and cosine similarity
    /// </summary>
    public class SimilarityService : ISimilarityService
    {
        private readonly ILogger<SimilarityService> _logger;
        private readonly IEmbeddingService _embeddingService;
        private readonly IVectorDatabaseService _vectorDatabaseService;
        private readonly AppSettings _appSettings;

        public SimilarityService(
            ILogger<SimilarityService> logger,
            IEmbeddingService embeddingService,
            IVectorDatabaseService vectorDatabaseService,
            AppSettings appSettings)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _embeddingService = embeddingService ?? throw new ArgumentNullException(nameof(embeddingService));
            _vectorDatabaseService = vectorDatabaseService ?? throw new ArgumentNullException(nameof(vectorDatabaseService));
            _appSettings = appSettings ?? throw new ArgumentNullException(nameof(appSettings));
        }

        /// <summary>
        /// Finds similar JSON patterns for the given input JSON
        /// </summary>
        public async Task<SimilarityResult> FindSimilarJsonAsync(string inputJson)
        {
            var stopwatch = Stopwatch.StartNew();
            var result = new SimilarityResult
            {
                ThresholdUsed = _appSettings.Vector.SimilarityThreshold,
                MaxResultsRequested = _appSettings.Vector.MaxSimilarResults
            };

            try
            {
                if (string.IsNullOrWhiteSpace(inputJson))
                {
                    _logger.LogWarning("Input JSON is null or empty");
                    return result;
                }

                if (!_appSettings.Vector.EnableSimilarityMatching)
                {
                    _logger.LogDebug("Similarity matching is disabled in configuration");
                    return result;
                }

                // Check if services are available
                if (!await IsAvailableAsync())
                {
                    _logger.LogWarning("Similarity services are not available");
                    return result;
                }

                _logger.LogDebug("Finding similar JSON patterns for input (length: {Length})", inputJson.Length);

                // Generate embedding for input JSON
                float[] queryEmbedding;
                try
                {
                    queryEmbedding = await _embeddingService.GenerateEmbeddingAsync(inputJson);
                    _logger.LogDebug("Generated query embedding with {Dimensions} dimensions", queryEmbedding.Length);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to generate embedding for input JSON");
                    return result;
                }

                // Find similar embeddings in the database
                try
                {
                    var matches = await _vectorDatabaseService.FindSimilarAsync(
                        queryEmbedding,
                        _appSettings.Vector.MaxSimilarResults,
                        _appSettings.Vector.SimilarityThreshold);

                    result.Matches = matches;
                    result.TotalMatches = matches.Count;
                    result.HighestScore = matches.Any() ? matches.Max(m => m.SimilarityScore) : 0.0f;

                    _logger.LogDebug("Found {Count} similar JSON patterns with highest score {HighestScore:F3}", 
                        matches.Count, result.HighestScore);

                    // Log details about matches in verbose mode
                    if (_appSettings.Application.VerboseMode && matches.Any())
                    {
                        foreach (var match in matches.Take(3)) // Log top 3 matches
                        {
                            _logger.LogDebug("Match: {Id} (score: {Score:F3}) - {Description}", 
                                match.Id, match.SimilarityScore, match.Description);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to find similar embeddings in database");
                    return result;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during similarity search");
            }
            finally
            {
                stopwatch.Stop();
                result.ProcessingTime = stopwatch.Elapsed;
                
                _logger.LogDebug("Similarity search completed in {Duration}ms", 
                    result.ProcessingTime.TotalMilliseconds);
            }

            return result;
        }

        /// <summary>
        /// Calculates cosine similarity between two vectors
        /// </summary>
        public float CalculateCosineSimilarity(float[] vector1, float[] vector2)
        {
            if (vector1 == null)
                throw new ArgumentNullException(nameof(vector1));
            
            if (vector2 == null)
                throw new ArgumentNullException(nameof(vector2));

            if (vector1.Length != vector2.Length)
            {
                throw new ArgumentException($"Vectors must have the same length. Vector1: {vector1.Length}, Vector2: {vector2.Length}");
            }

            if (vector1.Length == 0)
            {
                return 0.0f;
            }

            try
            {
                double dotProduct = 0.0;
                double magnitude1 = 0.0;
                double magnitude2 = 0.0;

                // Calculate dot product and magnitudes in a single pass for efficiency
                for (int i = 0; i < vector1.Length; i++)
                {
                    var v1 = vector1[i];
                    var v2 = vector2[i];
                    
                    dotProduct += v1 * v2;
                    magnitude1 += v1 * v1;
                    magnitude2 += v2 * v2;
                }

                // Calculate magnitudes
                magnitude1 = Math.Sqrt(magnitude1);
                magnitude2 = Math.Sqrt(magnitude2);

                // Handle zero magnitude vectors
                if (magnitude1 == 0.0 || magnitude2 == 0.0)
                {
                    return 0.0f;
                }

                // Calculate cosine similarity
                var similarity = dotProduct / (magnitude1 * magnitude2);
                
                // Clamp to valid range [0, 1] to handle floating point precision issues
                return (float)Math.Max(0.0, Math.Min(1.0, similarity));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating cosine similarity");
                return 0.0f;
            }
        }

        /// <summary>
        /// Checks if the similarity service is available
        /// </summary>
        public async Task<bool> IsAvailableAsync()
        {
            try
            {
                // Check if similarity matching is enabled
                if (!_appSettings.Vector.EnableSimilarityMatching)
                {
                    _logger.LogDebug("Similarity matching is disabled");
                    return false;
                }

                // Check embedding service
                if (!await _embeddingService.IsEmbeddingServiceAvailableAsync())
                {
                    _logger.LogDebug("Embedding service is not available");
                    return false;
                }

                // Check vector database service
                if (!await _vectorDatabaseService.IsConnectedAsync())
                {
                    _logger.LogDebug("Vector database service is not connected");
                    return false;
                }

                _logger.LogDebug("Similarity service is available");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking similarity service availability");
                return false;
            }
        }

        /// <summary>
        /// Gets the current configuration for similarity matching
        /// </summary>
        public SimilarityConfiguration GetConfiguration()
        {
            var config = new SimilarityConfiguration
            {
                Threshold = _appSettings.Vector.SimilarityThreshold,
                MaxResults = _appSettings.Vector.MaxSimilarResults,
                IsEnabled = _appSettings.Vector.EnableSimilarityMatching,
                EmbeddingModel = _embeddingService.GetEmbeddingModelName()
            };

            // Try to get knowledge base size
            try
            {
                config.KnowledgeBaseSize = _vectorDatabaseService.GetEmbeddingCountAsync().GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not retrieve knowledge base size");
                config.KnowledgeBaseSize = -1; // Indicate unknown
            }

            return config;
        }
    }
}