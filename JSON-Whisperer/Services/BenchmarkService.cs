using Microsoft.Extensions.Logging;
using JSON_Whisperer.Interfaces;
using JSON_Whisperer.Models;
using System.Diagnostics;

namespace JSON_Whisperer.Services
{
    /// <summary>
    /// Service for executing performance benchmarks on various operations
    /// </summary>
    public class BenchmarkService : IBenchmarkService
    {
        private readonly ILogger<BenchmarkService> _logger;
        private readonly IEmbeddingService _embeddingService;
        private readonly IVectorDatabaseService _vectorDatabaseService;
        private readonly ISimilarityService _similarityService;
        private readonly AppSettings _appSettings;

        // Benchmark configuration
        private const int DefaultIterations = 10;
        private const int EmbeddingIterations = 20;
        private const int SimilarityIterations = 15;
        private const int VectorOperationsIterations = 10;

        public BenchmarkService(
            ILogger<BenchmarkService> logger,
            IEmbeddingService embeddingService,
            IVectorDatabaseService vectorDatabaseService,
            ISimilarityService similarityService,
            AppSettings appSettings)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _embeddingService = embeddingService ?? throw new ArgumentNullException(nameof(embeddingService));
            _vectorDatabaseService = vectorDatabaseService ?? throw new ArgumentNullException(nameof(vectorDatabaseService));
            _similarityService = similarityService ?? throw new ArgumentNullException(nameof(similarityService));
            _appSettings = appSettings ?? throw new ArgumentNullException(nameof(appSettings));
        }

        /// <summary>
        /// Runs all available benchmarks and returns aggregated results
        /// </summary>
        public async Task<List<BenchmarkResult>> RunAllBenchmarksAsync()
        {
            _logger.LogInformation("Starting comprehensive benchmark suite...");

            var results = new List<BenchmarkResult>();

            // Run embedding benchmark
            _logger.LogInformation("Running embedding generation benchmark...");
            var embeddingResult = await BenchmarkEmbeddingAsync();
            results.Add(embeddingResult);

            // Run vector operations benchmark
            _logger.LogInformation("Running vector operations benchmark...");
            var vectorOpsResult = await BenchmarkVectorOperationsAsync();
            results.Add(vectorOpsResult);

            // Run similarity search benchmark
            _logger.LogInformation("Running similarity search benchmark...");
            var similarityResult = await BenchmarkSimilarityAsync();
            results.Add(similarityResult);

            _logger.LogInformation("Benchmark suite completed. Total benchmarks: {Count}", results.Count);

            return results;
        }

        /// <summary>
        /// Benchmarks similarity search performance
        /// </summary>
        public async Task<BenchmarkResult> BenchmarkSimilarityAsync()
        {
            var result = new BenchmarkResult
            {
                BenchmarkName = "Similarity Search",
                Iterations = SimilarityIterations,
                ExecutedAt = DateTime.UtcNow
            };

            try
            {
                _logger.LogInformation("Starting similarity search benchmark with {Iterations} iterations", SimilarityIterations);

                // Check if similarity service is available
                var isAvailable = await _similarityService.IsAvailableAsync();
                if (!isAvailable)
                {
                    result.Success = false;
                    result.ErrorMessage = "Similarity service is not available";
                    _logger.LogWarning("Similarity service is not available for benchmarking");
                    return result;
                }

                // Test JSON samples
                var testSamples = new[]
                {
                    @"{""name"": ""John Doe"", ""email"": ""john@example.com"", ""age"": 30}",
                    @"{""product"": ""Laptop"", ""price"": 999.99, ""category"": ""Electronics""}",
                    @"{""order_id"": ""12345"", ""status"": ""shipped"", ""items"": [""item1"", ""item2""]}",
                    @"{""user_id"": 42, ""preferences"": {""theme"": ""dark"", ""language"": ""en""}}"
                };

                var memoryBefore = GC.GetTotalMemory(true);
                var stopwatch = Stopwatch.StartNew();
                var durations = new List<double>();
                var matchCounts = new List<int>();

                for (int i = 0; i < SimilarityIterations; i++)
                {
                    var sample = testSamples[i % testSamples.Length];
                    var iterationStopwatch = Stopwatch.StartNew();

                    var similarityResult = await _similarityService.FindSimilarJsonAsync(sample);

                    iterationStopwatch.Stop();
                    durations.Add(iterationStopwatch.Elapsed.TotalMilliseconds);

                    if (similarityResult != null)
                    {
                        matchCounts.Add(similarityResult.Matches.Count);
                    }
                }

                stopwatch.Stop();
                var memoryAfter = GC.GetTotalMemory(false);

                result.TotalDuration = stopwatch.Elapsed;
                result.AverageDurationMs = durations.Average();
                result.OperationsPerSecond = SimilarityIterations / stopwatch.Elapsed.TotalSeconds;
                result.MemoryUsedBytes = memoryAfter - memoryBefore;
                result.Success = true;

                // Additional metrics
                result.AdditionalMetrics["MinDurationMs"] = durations.Min();
                result.AdditionalMetrics["MaxDurationMs"] = durations.Max();
                result.AdditionalMetrics["MedianDurationMs"] = CalculateMedian(durations);
                result.AdditionalMetrics["AverageMatchesFound"] = matchCounts.Any() ? matchCounts.Average() : 0;
                result.AdditionalMetrics["TotalMatchesFound"] = matchCounts.Sum();

                var config = _similarityService.GetConfiguration();
                result.AdditionalMetrics["SimilarityThreshold"] = config.Threshold;
                result.AdditionalMetrics["KnowledgeBaseSize"] = config.KnowledgeBaseSize;

                _logger.LogInformation(
                    "Similarity search benchmark completed. Avg: {Avg:F2}ms, Ops/sec: {Ops:F2}, Memory: {Memory} bytes",
                    result.AverageDurationMs,
                    result.OperationsPerSecond,
                    result.MemoryUsedBytes);
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.ErrorMessage = ex.Message;
                _logger.LogError(ex, "Error during similarity search benchmark");
            }

            return result;
        }

        /// <summary>
        /// Benchmarks vector operations (embedding generation and storage)
        /// </summary>
        public async Task<BenchmarkResult> BenchmarkVectorOperationsAsync()
        {
            var result = new BenchmarkResult
            {
                BenchmarkName = "Vector Operations",
                Iterations = VectorOperationsIterations,
                ExecutedAt = DateTime.UtcNow
            };

            try
            {
                _logger.LogInformation("Starting vector operations benchmark with {Iterations} iterations", VectorOperationsIterations);

                // Check if services are available
                var embeddingAvailable = await _embeddingService.IsEmbeddingServiceAvailableAsync();
                var dbConnected = await _vectorDatabaseService.IsConnectedAsync();

                if (!embeddingAvailable || !dbConnected)
                {
                    result.Success = false;
                    result.ErrorMessage = $"Required services not available. Embedding: {embeddingAvailable}, Database: {dbConnected}";
                    _logger.LogWarning("Vector operations services not available for benchmarking");
                    return result;
                }

                var testTexts = new[]
                {
                    "Sample JSON document for user profile with name, email, and preferences",
                    "Product catalog entry with SKU, price, description, and inventory details",
                    "Order transaction record with customer information and line items",
                    "Configuration settings with nested objects and array values"
                };

                var memoryBefore = GC.GetTotalMemory(true);
                var stopwatch = Stopwatch.StartNew();
                var embeddingDurations = new List<double>();
                var storageDurations = new List<double>();
                var testIds = new List<string>();

                for (int i = 0; i < VectorOperationsIterations; i++)
                {
                    var text = testTexts[i % testTexts.Length];
                    var testId = $"benchmark_test_{Guid.NewGuid()}";
                    testIds.Add(testId);

                    // Benchmark embedding generation
                    var embeddingStopwatch = Stopwatch.StartNew();
                    var embedding = await _embeddingService.GenerateEmbeddingAsync(text);
                    embeddingStopwatch.Stop();
                    embeddingDurations.Add(embeddingStopwatch.Elapsed.TotalMilliseconds);

                    if (embedding == null || embedding.Length == 0)
                    {
                        _logger.LogWarning("Embedding generation failed for iteration {Iteration}", i);
                        continue;
                    }

                    // Benchmark storage
                    var storageStopwatch = Stopwatch.StartNew();
                    var stored = await _vectorDatabaseService.StoreEmbeddingAsync(
                        testId,
                        embedding,
                        $"{{\"test\": \"data_{i}\"}}",
                        text);
                    storageStopwatch.Stop();
                    storageDurations.Add(storageStopwatch.Elapsed.TotalMilliseconds);

                    if (!stored)
                    {
                        _logger.LogWarning("Storage failed for iteration {Iteration}", i);
                    }
                }

                stopwatch.Stop();
                var memoryAfter = GC.GetTotalMemory(false);

                // Clean up test data
                foreach (var testId in testIds)
                {
                    try
                    {
                        await _vectorDatabaseService.DeleteEmbeddingAsync(testId);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to clean up test embedding {Id}", testId);
                    }
                }

                result.TotalDuration = stopwatch.Elapsed;
                result.AverageDurationMs = (embeddingDurations.Average() + storageDurations.Average());
                result.OperationsPerSecond = VectorOperationsIterations / stopwatch.Elapsed.TotalSeconds;
                result.MemoryUsedBytes = memoryAfter - memoryBefore;
                result.Success = true;

                // Additional metrics
                result.AdditionalMetrics["AvgEmbeddingGenerationMs"] = embeddingDurations.Average();
                result.AdditionalMetrics["AvgStorageMs"] = storageDurations.Average();
                result.AdditionalMetrics["MinEmbeddingMs"] = embeddingDurations.Min();
                result.AdditionalMetrics["MaxEmbeddingMs"] = embeddingDurations.Max();
                result.AdditionalMetrics["MinStorageMs"] = storageDurations.Min();
                result.AdditionalMetrics["MaxStorageMs"] = storageDurations.Max();
                result.AdditionalMetrics["EmbeddingDimensions"] = 768; // Default for nomic-embed-text

                _logger.LogInformation(
                    "Vector operations benchmark completed. Avg embedding: {AvgEmb:F2}ms, Avg storage: {AvgStore:F2}ms, Ops/sec: {Ops:F2}",
                    embeddingDurations.Average(),
                    storageDurations.Average(),
                    result.OperationsPerSecond);
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.ErrorMessage = ex.Message;
                _logger.LogError(ex, "Error during vector operations benchmark");
            }

            return result;
        }

        /// <summary>
        /// Benchmarks embedding generation performance
        /// </summary>
        public async Task<BenchmarkResult> BenchmarkEmbeddingAsync()
        {
            var result = new BenchmarkResult
            {
                BenchmarkName = "Embedding Generation",
                Iterations = EmbeddingIterations,
                ExecutedAt = DateTime.UtcNow
            };

            try
            {
                _logger.LogInformation("Starting embedding generation benchmark with {Iterations} iterations", EmbeddingIterations);

                // Check if embedding service is available
                var isAvailable = await _embeddingService.IsEmbeddingServiceAvailableAsync();
                if (!isAvailable)
                {
                    result.Success = false;
                    result.ErrorMessage = "Embedding service is not available";
                    _logger.LogWarning("Embedding service is not available for benchmarking");
                    return result;
                }

                // Test texts of varying lengths
                var testTexts = new[]
                {
                    "Short JSON description",
                    "Medium length JSON document description with more details about the structure and content",
                    "Long JSON document description that includes comprehensive information about the data model, field types, validation rules, and usage examples for developers",
                    "Very long JSON document description that provides extensive documentation including field definitions, data types, constraints, relationships, business rules, validation logic, error handling, and detailed examples of various use cases"
                };

                var memoryBefore = GC.GetTotalMemory(true);
                var stopwatch = Stopwatch.StartNew();
                var durations = new List<double>();
                var textLengths = new List<int>();
                var embeddingDimensions = new List<int>();

                for (int i = 0; i < EmbeddingIterations; i++)
                {
                    var text = testTexts[i % testTexts.Length];
                    textLengths.Add(text.Length);

                    var iterationStopwatch = Stopwatch.StartNew();
                    var embedding = await _embeddingService.GenerateEmbeddingAsync(text);
                    iterationStopwatch.Stop();

                    durations.Add(iterationStopwatch.Elapsed.TotalMilliseconds);

                    if (embedding != null && embedding.Length > 0)
                    {
                        embeddingDimensions.Add(embedding.Length);
                    }
                }

                stopwatch.Stop();
                var memoryAfter = GC.GetTotalMemory(false);

                result.TotalDuration = stopwatch.Elapsed;
                result.AverageDurationMs = durations.Average();
                result.OperationsPerSecond = EmbeddingIterations / stopwatch.Elapsed.TotalSeconds;
                result.MemoryUsedBytes = memoryAfter - memoryBefore;
                result.Success = true;

                // Additional metrics
                result.AdditionalMetrics["MinDurationMs"] = durations.Min();
                result.AdditionalMetrics["MaxDurationMs"] = durations.Max();
                result.AdditionalMetrics["MedianDurationMs"] = CalculateMedian(durations);
                result.AdditionalMetrics["AverageTextLength"] = textLengths.Average();
                result.AdditionalMetrics["EmbeddingDimensions"] = embeddingDimensions.Any() ? embeddingDimensions.First() : 0;
                result.AdditionalMetrics["CharactersPerSecond"] = textLengths.Sum() / stopwatch.Elapsed.TotalSeconds;

                _logger.LogInformation(
                    "Embedding generation benchmark completed. Avg: {Avg:F2}ms, Ops/sec: {Ops:F2}, Memory: {Memory} bytes",
                    result.AverageDurationMs,
                    result.OperationsPerSecond,
                    result.MemoryUsedBytes);
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.ErrorMessage = ex.Message;
                _logger.LogError(ex, "Error during embedding generation benchmark");
            }

            return result;
        }

        /// <summary>
        /// Calculates the median value from a list of doubles
        /// </summary>
        private double CalculateMedian(List<double> values)
        {
            if (values == null || values.Count == 0)
                return 0;

            var sorted = values.OrderBy(v => v).ToList();
            int count = sorted.Count;

            if (count % 2 == 0)
            {
                // Even number of elements - average the two middle values
                return (sorted[count / 2 - 1] + sorted[count / 2]) / 2.0;
            }
            else
            {
                // Odd number of elements - return the middle value
                return sorted[count / 2];
            }
        }
    }
}
