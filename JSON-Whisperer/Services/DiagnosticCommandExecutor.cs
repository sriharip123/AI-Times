using Microsoft.Extensions.Logging;
using JSON_Whisperer.Interfaces;
using JSON_Whisperer.Models;

namespace JSON_Whisperer.Services
{
    /// <summary>
    /// Executes diagnostic commands and returns appropriate exit codes
    /// </summary>
    public class DiagnosticCommandExecutor : IDiagnosticCommandExecutor
    {
        private readonly ILogger<DiagnosticCommandExecutor> _logger;
        private readonly IAiService _aiService;
        private readonly IEmbeddingService _embeddingService;
        private readonly IVectorDatabaseService _vectorDatabaseService;
        private readonly IKnowledgeBaseService _knowledgeBaseService;
        private readonly IKnowledgeBaseManagementService _knowledgeBaseManagementService;
        private readonly ConfigurationService _configurationService;
        private readonly AppSettings _appSettings;

        public DiagnosticCommandExecutor(
            ILogger<DiagnosticCommandExecutor> logger,
            IAiService aiService,
            IEmbeddingService embeddingService,
            IVectorDatabaseService vectorDatabaseService,
            IKnowledgeBaseService knowledgeBaseService,
            IKnowledgeBaseManagementService knowledgeBaseManagementService,
            ConfigurationService configurationService,
            AppSettings appSettings)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _aiService = aiService ?? throw new ArgumentNullException(nameof(aiService));
            _embeddingService = embeddingService ?? throw new ArgumentNullException(nameof(embeddingService));
            _vectorDatabaseService = vectorDatabaseService ?? throw new ArgumentNullException(nameof(vectorDatabaseService));
            _knowledgeBaseService = knowledgeBaseService ?? throw new ArgumentNullException(nameof(knowledgeBaseService));
            _knowledgeBaseManagementService = knowledgeBaseManagementService ?? throw new ArgumentNullException(nameof(knowledgeBaseManagementService));
            _configurationService = configurationService ?? throw new ArgumentNullException(nameof(configurationService));
            _appSettings = appSettings ?? throw new ArgumentNullException(nameof(appSettings));
        }

        /// <summary>
        /// Executes a diagnostic command and returns an exit code
        /// </summary>
        public async Task<int> ExecuteAsync(DiagnosticCommand command, CommandLineOptions options)
        {
            try
            {
                _logger.LogInformation("Executing diagnostic command: {Command}", command);
                
                return command switch
                {
                    DiagnosticCommand.HealthCheck => await ExecuteHealthCheckAsync(options),
                    DiagnosticCommand.ValidateConfig => await ExecuteValidateConfigAsync(options),
                    DiagnosticCommand.TestOllama => await ExecuteTestOllamaAsync(options),
                    DiagnosticCommand.TestScylla => await ExecuteTestScyllaAsync(options),
                    DiagnosticCommand.TestEmbedding => await ExecuteTestEmbeddingAsync(options),
                    DiagnosticCommand.TestSimilarity => await ExecuteTestSimilarityAsync(options),
                    DiagnosticCommand.ReinitializeKnowledgeBase => await ExecuteReinitializeKnowledgeBaseAsync(options),
                    DiagnosticCommand.ValidateKnowledgeBase => await ExecuteValidateKnowledgeBaseAsync(options),
                    DiagnosticCommand.BenchmarkAll => await ExecuteBenchmarkAllAsync(options),
                    DiagnosticCommand.BenchmarkSimilarity => await ExecuteBenchmarkSimilarityAsync(options),
                    DiagnosticCommand.BenchmarkVectorOperations => await ExecuteBenchmarkVectorOperationsAsync(options),
                    DiagnosticCommand.BenchmarkEmbedding => await ExecuteBenchmarkEmbeddingAsync(options),
                    _ => throw new ArgumentException($"Unknown diagnostic command: {command}")
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing diagnostic command: {Command}", command);
                DisplayError($"Diagnostic command failed: {ex.Message}");
                return ExitCodes.GeneralError;
            }
        }

        #region Health Check

        private async Task<int> ExecuteHealthCheckAsync(CommandLineOptions options)
        {
            Console.WriteLine("=== Health Check ===");
            Console.WriteLine();

            bool allHealthy = true;
            var startTime = DateTime.UtcNow;

            // Check Ollama service
            var ollamaHealthy = await CheckOllamaHealthAsync(options.VerboseMode);
            allHealthy &= ollamaHealthy;

            // Check ScyllaDB
            var scyllaHealthy = await CheckScyllaHealthAsync(options.VerboseMode);
            allHealthy &= scyllaHealthy;

            // Check Embedding service
            var embeddingHealthy = await CheckEmbeddingHealthAsync(options.VerboseMode);
            allHealthy &= embeddingHealthy;

            // Check Knowledge Base
            var knowledgeBaseHealthy = await CheckKnowledgeBaseHealthAsync(options.VerboseMode);
            allHealthy &= knowledgeBaseHealthy;

            var duration = DateTime.UtcNow - startTime;

            Console.WriteLine();
            Console.WriteLine($"Health check completed in {duration.TotalMilliseconds:F0}ms");
            Console.WriteLine($"Overall Status: {(allHealthy ? "HEALTHY" : "UNHEALTHY")}");

            return allHealthy ? ExitCodes.Success : ExitCodes.ServiceUnavailable;
        }

        private async Task<bool> CheckOllamaHealthAsync(bool verbose)
        {
            Console.Write("Checking Ollama service... ");
            var startTime = DateTime.UtcNow;

            try
            {
                var isAvailable = await _aiService.IsAvailableAsync();
                var duration = DateTime.UtcNow - startTime;

                if (isAvailable)
                {
                    Console.WriteLine($"✓ HEALTHY ({duration.TotalMilliseconds:F0}ms)");
                    if (verbose)
                    {
                        Console.WriteLine($"  URL: {_appSettings.Ollama.BaseUrl}");
                        Console.WriteLine($"  Model: {_appSettings.Ollama.ModelName}");
                    }
                    return true;
                }
                else
                {
                    Console.WriteLine($"✗ UNAVAILABLE ({duration.TotalMilliseconds:F0}ms)");
                    if (verbose)
                    {
                        Console.WriteLine($"  URL: {_appSettings.Ollama.BaseUrl}");
                        Console.WriteLine($"  Model: {_appSettings.Ollama.ModelName}");
                    }
                    return false;
                }
            }
            catch (Exception ex)
            {
                var duration = DateTime.UtcNow - startTime;
                Console.WriteLine($"✗ ERROR ({duration.TotalMilliseconds:F0}ms)");
                if (verbose)
                {
                    Console.WriteLine($"  Error: {ex.Message}");
                }
                return false;
            }
        }

        private async Task<bool> CheckScyllaHealthAsync(bool verbose)
        {
            Console.Write("Checking ScyllaDB... ");
            var startTime = DateTime.UtcNow;

            try
            {
                var isConnected = await _vectorDatabaseService.IsConnectedAsync();
                var duration = DateTime.UtcNow - startTime;

                if (isConnected)
                {
                    Console.WriteLine($"✓ HEALTHY ({duration.TotalMilliseconds:F0}ms)");
                    if (verbose)
                    {
                        var count = await _vectorDatabaseService.GetEmbeddingCountAsync();
                        Console.WriteLine($"  Embeddings stored: {count}");
                    }
                    return true;
                }
                else
                {
                    Console.WriteLine($"✗ UNAVAILABLE ({duration.TotalMilliseconds:F0}ms)");
                    return false;
                }
            }
            catch (Exception ex)
            {
                var duration = DateTime.UtcNow - startTime;
                Console.WriteLine($"✗ ERROR ({duration.TotalMilliseconds:F0}ms)");
                if (verbose)
                {
                    Console.WriteLine($"  Error: {ex.Message}");
                }
                return false;
            }
        }

        private async Task<bool> CheckEmbeddingHealthAsync(bool verbose)
        {
            Console.Write("Checking Embedding service... ");
            var startTime = DateTime.UtcNow;

            try
            {
                var isAvailable = await _embeddingService.IsEmbeddingServiceAvailableAsync();
                var duration = DateTime.UtcNow - startTime;

                if (isAvailable)
                {
                    Console.WriteLine($"✓ HEALTHY ({duration.TotalMilliseconds:F0}ms)");
                    if (verbose)
                    {
                        Console.WriteLine($"  Model: {_embeddingService.GetEmbeddingModelName()}");
                    }
                    return true;
                }
                else
                {
                    Console.WriteLine($"✗ UNAVAILABLE ({duration.TotalMilliseconds:F0}ms)");
                    return false;
                }
            }
            catch (Exception ex)
            {
                var duration = DateTime.UtcNow - startTime;
                Console.WriteLine($"✗ ERROR ({duration.TotalMilliseconds:F0}ms)");
                if (verbose)
                {
                    Console.WriteLine($"  Error: {ex.Message}");
                }
                return false;
            }
        }

        private async Task<bool> CheckKnowledgeBaseHealthAsync(bool verbose)
        {
            Console.Write("Checking Knowledge Base... ");
            var startTime = DateTime.UtcNow;

            try
            {
                var examples = await _knowledgeBaseService.LoadExamplesAsync();
                var duration = DateTime.UtcNow - startTime;

                if (examples != null && examples.Count > 0)
                {
                    Console.WriteLine($"✓ HEALTHY ({duration.TotalMilliseconds:F0}ms)");
                    if (verbose)
                    {
                        Console.WriteLine($"  Examples loaded: {examples.Count}");
                    }
                    return true;
                }
                else
                {
                    Console.WriteLine($"⚠ WARNING ({duration.TotalMilliseconds:F0}ms)");
                    Console.WriteLine("  No examples found in knowledge base");
                    return true; // Not a critical failure
                }
            }
            catch (Exception ex)
            {
                var duration = DateTime.UtcNow - startTime;
                Console.WriteLine($"✗ ERROR ({duration.TotalMilliseconds:F0}ms)");
                if (verbose)
                {
                    Console.WriteLine($"  Error: {ex.Message}");
                }
                return false;
            }
        }

        #endregion

        #region Configuration Validation

        private async Task<int> ExecuteValidateConfigAsync(CommandLineOptions options)
        {
            Console.WriteLine("=== Configuration Validation ===");
            Console.WriteLine();

            bool isValid = true;
            var errors = new List<string>();

            // Validate Ollama configuration
            Console.WriteLine("Validating Ollama configuration...");
            if (!ValidateOllamaConfig(errors, options.VerboseMode))
            {
                isValid = false;
            }

            // Validate Application configuration
            Console.WriteLine("Validating Application configuration...");
            if (!ValidateApplicationConfig(errors, options.VerboseMode))
            {
                isValid = false;
            }

            Console.WriteLine();
            if (isValid)
            {
                Console.WriteLine("✓ Configuration is valid");
                return ExitCodes.Success;
            }
            else
            {
                Console.WriteLine("✗ Configuration validation failed:");
                foreach (var error in errors)
                {
                    Console.WriteLine($"  - {error}");
                }
                return ExitCodes.ConfigurationError;
            }

            await Task.CompletedTask;
        }

        private bool ValidateOllamaConfig(List<string> errors, bool verbose)
        {
            bool isValid = true;

            // Validate BaseUrl
            if (string.IsNullOrWhiteSpace(_appSettings.Ollama.BaseUrl))
            {
                errors.Add("Ollama BaseUrl is missing");
                isValid = false;
            }
            else if (!Uri.TryCreate(_appSettings.Ollama.BaseUrl, UriKind.Absolute, out var uri))
            {
                errors.Add($"Ollama BaseUrl is not a valid URL: {_appSettings.Ollama.BaseUrl}");
                isValid = false;
            }
            else if (verbose)
            {
                Console.WriteLine($"  ✓ BaseUrl: {_appSettings.Ollama.BaseUrl}");
            }

            // Validate ModelName
            if (string.IsNullOrWhiteSpace(_appSettings.Ollama.ModelName))
            {
                errors.Add("Ollama ModelName is missing");
                isValid = false;
            }
            else if (verbose)
            {
                Console.WriteLine($"  ✓ ModelName: {_appSettings.Ollama.ModelName}");
            }

            // Validate TimeoutSeconds
            if (_appSettings.Ollama.TimeoutSeconds <= 0)
            {
                errors.Add($"Ollama TimeoutSeconds must be positive: {_appSettings.Ollama.TimeoutSeconds}");
                isValid = false;
            }
            else if (verbose)
            {
                Console.WriteLine($"  ✓ TimeoutSeconds: {_appSettings.Ollama.TimeoutSeconds}");
            }

            return isValid;
        }

        private bool ValidateApplicationConfig(List<string> errors, bool verbose)
        {
            bool isValid = true;

            // Validate MaxJsonSizeBytes
            if (_appSettings.Application.MaxJsonSizeBytes <= 0)
            {
                errors.Add($"Application MaxJsonSizeBytes must be positive: {_appSettings.Application.MaxJsonSizeBytes}");
                isValid = false;
            }
            else if (verbose)
            {
                Console.WriteLine($"  ✓ MaxJsonSizeBytes: {_appSettings.Application.MaxJsonSizeBytes}");
            }

            // Validate OutputFormat
            var validFormats = new[] { "standard", "compact", "detailed" };
            if (string.IsNullOrWhiteSpace(_appSettings.Application.OutputFormat))
            {
                errors.Add("Application OutputFormat is missing");
                isValid = false;
            }
            else if (!validFormats.Contains(_appSettings.Application.OutputFormat.ToLower()))
            {
                errors.Add($"Application OutputFormat must be one of: {string.Join(", ", validFormats)}");
                isValid = false;
            }
            else if (verbose)
            {
                Console.WriteLine($"  ✓ OutputFormat: {_appSettings.Application.OutputFormat}");
            }

            return isValid;
        }

        #endregion

        #region Service Testing

        private async Task<int> ExecuteTestOllamaAsync(CommandLineOptions options)
        {
            Console.WriteLine("=== Testing Ollama Service ===");
            Console.WriteLine();

            var startTime = DateTime.UtcNow;

            try
            {
                Console.WriteLine($"Connecting to: {_appSettings.Ollama.BaseUrl}");
                Console.WriteLine($"Testing model: {_appSettings.Ollama.ModelName}");
                Console.WriteLine();

                var isAvailable = await _aiService.IsAvailableAsync();
                var duration = DateTime.UtcNow - startTime;

                if (isAvailable)
                {
                    Console.WriteLine($"✓ Ollama service is available ({duration.TotalMilliseconds:F0}ms)");
                    Console.WriteLine($"✓ Model '{_appSettings.Ollama.ModelName}' is loaded and ready");
                    return ExitCodes.Success;
                }
                else
                {
                    Console.WriteLine($"✗ Ollama service is not available ({duration.TotalMilliseconds:F0}ms)");
                    Console.WriteLine();
                    Console.WriteLine("Troubleshooting suggestions:");
                    Console.WriteLine("  1. Verify Ollama is running: ollama list");
                    Console.WriteLine($"  2. Check the URL is correct: {_appSettings.Ollama.BaseUrl}");
                    Console.WriteLine($"  3. Ensure model is pulled: ollama pull {_appSettings.Ollama.ModelName}");
                    return ExitCodes.ServiceUnavailable;
                }
            }
            catch (Exception ex)
            {
                var duration = DateTime.UtcNow - startTime;
                Console.WriteLine($"✗ Error testing Ollama service ({duration.TotalMilliseconds:F0}ms)");
                Console.WriteLine($"Error: {ex.Message}");
                if (options.VerboseMode)
                {
                    Console.WriteLine($"Stack trace: {ex.StackTrace}");
                }
                return ExitCodes.ServiceUnavailable;
            }
        }

        private async Task<int> ExecuteTestScyllaAsync(CommandLineOptions options)
        {
            Console.WriteLine("=== Testing ScyllaDB ===");
            Console.WriteLine();

            var startTime = DateTime.UtcNow;

            try
            {
                Console.WriteLine("Connecting to ScyllaDB...");

                var isConnected = await _vectorDatabaseService.IsConnectedAsync();
                var duration = DateTime.UtcNow - startTime;

                if (isConnected)
                {
                    Console.WriteLine($"✓ ScyllaDB is connected ({duration.TotalMilliseconds:F0}ms)");
                    
                    var count = await _vectorDatabaseService.GetEmbeddingCountAsync();
                    Console.WriteLine($"✓ Embeddings stored: {count}");
                    
                    return ExitCodes.Success;
                }
                else
                {
                    Console.WriteLine($"✗ ScyllaDB is not connected ({duration.TotalMilliseconds:F0}ms)");
                    Console.WriteLine();
                    Console.WriteLine("Troubleshooting suggestions:");
                    Console.WriteLine("  1. Verify ScyllaDB is running");
                    Console.WriteLine("  2. Check connection settings in configuration");
                    Console.WriteLine("  3. Verify network connectivity");
                    return ExitCodes.ServiceUnavailable;
                }
            }
            catch (Exception ex)
            {
                var duration = DateTime.UtcNow - startTime;
                Console.WriteLine($"✗ Error testing ScyllaDB ({duration.TotalMilliseconds:F0}ms)");
                Console.WriteLine($"Error: {ex.Message}");
                if (options.VerboseMode)
                {
                    Console.WriteLine($"Stack trace: {ex.StackTrace}");
                }
                return ExitCodes.ServiceUnavailable;
            }
        }

        private async Task<int> ExecuteTestEmbeddingAsync(CommandLineOptions options)
        {
            Console.WriteLine("=== Testing Embedding Service ===");
            Console.WriteLine();

            var startTime = DateTime.UtcNow;

            try
            {
                Console.WriteLine($"Testing embedding model: {_embeddingService.GetEmbeddingModelName()}");
                Console.WriteLine();

                var isAvailable = await _embeddingService.IsEmbeddingServiceAvailableAsync();
                
                if (isAvailable)
                {
                    Console.WriteLine($"✓ Embedding service is available");
                    
                    // Test actual embedding generation
                    Console.WriteLine("Generating test embedding...");
                    var testJson = "{\"test\": \"data\"}";
                    var embedding = await _embeddingService.GenerateEmbeddingAsync(testJson);
                    var duration = DateTime.UtcNow - startTime;
                    
                    Console.WriteLine($"✓ Test embedding generated successfully ({duration.TotalMilliseconds:F0}ms)");
                    Console.WriteLine($"✓ Embedding dimensions: {embedding.Length}");
                    
                    if (options.VerboseMode)
                    {
                        Console.WriteLine($"  First 5 values: [{string.Join(", ", embedding.Take(5).Select(v => v.ToString("F4")))}...]");
                    }
                    
                    return ExitCodes.Success;
                }
                else
                {
                    var duration = DateTime.UtcNow - startTime;
                    Console.WriteLine($"✗ Embedding service is not available ({duration.TotalMilliseconds:F0}ms)");
                    Console.WriteLine();
                    Console.WriteLine("Troubleshooting suggestions:");
                    Console.WriteLine("  1. Verify Ollama is running");
                    Console.WriteLine($"  2. Ensure embedding model is pulled: ollama pull {_embeddingService.GetEmbeddingModelName()}");
                    return ExitCodes.ServiceUnavailable;
                }
            }
            catch (Exception ex)
            {
                var duration = DateTime.UtcNow - startTime;
                Console.WriteLine($"✗ Error testing embedding service ({duration.TotalMilliseconds:F0}ms)");
                Console.WriteLine($"Error: {ex.Message}");
                if (options.VerboseMode)
                {
                    Console.WriteLine($"Stack trace: {ex.StackTrace}");
                }
                return ExitCodes.ServiceUnavailable;
            }
        }

        private async Task<int> ExecuteTestSimilarityAsync(CommandLineOptions options)
        {
            Console.WriteLine("=== Testing Similarity Search ===");
            Console.WriteLine();

            var startTime = DateTime.UtcNow;

            try
            {
                // Check if services are available
                var embeddingAvailable = await _embeddingService.IsEmbeddingServiceAvailableAsync();
                var dbConnected = await _vectorDatabaseService.IsConnectedAsync();

                if (!embeddingAvailable)
                {
                    Console.WriteLine("✗ Embedding service is not available");
                    return ExitCodes.ServiceUnavailable;
                }

                if (!dbConnected)
                {
                    Console.WriteLine("✗ ScyllaDB is not connected");
                    return ExitCodes.ServiceUnavailable;
                }

                // Check if there are embeddings to search
                var count = await _vectorDatabaseService.GetEmbeddingCountAsync();
                if (count == 0)
                {
                    Console.WriteLine("⚠ No embeddings in database to search");
                    Console.WriteLine("Run --reinitialize-knowledge-base to populate the database");
                    return ExitCodes.Success; // Not an error, just no data
                }

                Console.WriteLine($"Found {count} embeddings in database");
                Console.WriteLine("Generating test query embedding...");

                // Generate test embedding
                var testQuery = "{\"example\": \"test query\"}";
                var queryEmbedding = await _embeddingService.GenerateEmbeddingAsync(testQuery);

                Console.WriteLine("Searching for similar embeddings...");
                var results = await _vectorDatabaseService.FindSimilarAsync(queryEmbedding, maxResults: 3, threshold: 0.0f);
                var duration = DateTime.UtcNow - startTime;

                Console.WriteLine($"✓ Similarity search completed ({duration.TotalMilliseconds:F0}ms)");
                Console.WriteLine($"✓ Found {results.Count} results");

                if (options.VerboseMode && results.Count > 0)
                {
                    Console.WriteLine();
                    Console.WriteLine("Top results:");
                    foreach (var result in results.Take(3))
                    {
                        Console.WriteLine($"  - Similarity: {result.SimilarityScore:F4}");
                        Console.WriteLine($"    Description: {result.Description}");
                    }
                }

                return ExitCodes.Success;
            }
            catch (Exception ex)
            {
                var duration = DateTime.UtcNow - startTime;
                Console.WriteLine($"✗ Error testing similarity search ({duration.TotalMilliseconds:F0}ms)");
                Console.WriteLine($"Error: {ex.Message}");
                if (options.VerboseMode)
                {
                    Console.WriteLine($"Stack trace: {ex.StackTrace}");
                }
                return ExitCodes.ServiceUnavailable;
            }
        }

        #endregion

        #region Knowledge Base Management

        private async Task<int> ExecuteReinitializeKnowledgeBaseAsync(CommandLineOptions options)
        {
            Console.WriteLine("=== Reinitializing Knowledge Base ===");
            Console.WriteLine();
            Console.WriteLine("⚠ This will clear all existing embeddings and regenerate them.");
            Console.WriteLine();

            var startTime = DateTime.UtcNow;

            try
            {
                // Check if services are available
                var embeddingAvailable = await _embeddingService.IsEmbeddingServiceAvailableAsync();
                var dbConnected = await _vectorDatabaseService.IsConnectedAsync();

                if (!embeddingAvailable)
                {
                    Console.WriteLine("✗ Embedding service is not available");
                    return ExitCodes.ServiceUnavailable;
                }

                if (!dbConnected)
                {
                    Console.WriteLine("✗ ScyllaDB is not connected");
                    return ExitCodes.ServiceUnavailable;
                }

                // Initialize vector database (this will create tables if needed)
                Console.WriteLine("Initializing vector database...");
                await _knowledgeBaseService.InitializeVectorDatabaseAsync();

                var duration = DateTime.UtcNow - startTime;
                Console.WriteLine($"✓ Knowledge base reinitialized successfully ({duration.TotalMilliseconds:F0}ms)");

                var count = await _vectorDatabaseService.GetEmbeddingCountAsync();
                Console.WriteLine($"✓ Total embeddings: {count}");

                return ExitCodes.Success;
            }
            catch (Exception ex)
            {
                var duration = DateTime.UtcNow - startTime;
                Console.WriteLine($"✗ Error reinitializing knowledge base ({duration.TotalMilliseconds:F0}ms)");
                Console.WriteLine($"Error: {ex.Message}");
                if (options.VerboseMode)
                {
                    Console.WriteLine($"Stack trace: {ex.StackTrace}");
                }
                return ExitCodes.GeneralError;
            }
        }

        private async Task<int> ExecuteValidateKnowledgeBaseAsync(CommandLineOptions options)
        {
            Console.WriteLine("=== Validating Knowledge Base ===");
            Console.WriteLine();

            try
            {
                var validationResult = await _knowledgeBaseManagementService.ValidateAsync();

                // Return ValidationError if there are any invalid files or errors
                if (validationResult.InvalidFiles > 0 || validationResult.Errors.Count > 0)
                {
                    return ExitCodes.ValidationError;
                }

                return ExitCodes.Success;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ Error validating knowledge base");
                Console.WriteLine($"Error: {ex.Message}");
                if (options.VerboseMode)
                {
                    Console.WriteLine($"Stack trace: {ex.StackTrace}");
                }
                return ExitCodes.ValidationError;
            }
        }

        #endregion

        #region Benchmarking

        private async Task<int> ExecuteBenchmarkAllAsync(CommandLineOptions options)
        {
            Console.WriteLine("=== Running All Benchmarks ===");
            Console.WriteLine();

            var overallSuccess = true;

            // Run similarity benchmark
            var similarityResult = await ExecuteBenchmarkSimilarityAsync(options);
            overallSuccess &= (similarityResult == ExitCodes.Success);
            Console.WriteLine();

            // Run vector operations benchmark
            var vectorResult = await ExecuteBenchmarkVectorOperationsAsync(options);
            overallSuccess &= (vectorResult == ExitCodes.Success);
            Console.WriteLine();

            // Run embedding benchmark
            var embeddingResult = await ExecuteBenchmarkEmbeddingAsync(options);
            overallSuccess &= (embeddingResult == ExitCodes.Success);

            Console.WriteLine();
            Console.WriteLine($"Overall: {(overallSuccess ? "All benchmarks completed successfully" : "Some benchmarks failed")}");

            return overallSuccess ? ExitCodes.Success : ExitCodes.GeneralError;
        }

        private async Task<int> ExecuteBenchmarkSimilarityAsync(CommandLineOptions options)
        {
            Console.WriteLine("=== Benchmark: Similarity Search ===");
            Console.WriteLine();

            try
            {
                // Check prerequisites
                var embeddingAvailable = await _embeddingService.IsEmbeddingServiceAvailableAsync();
                var dbConnected = await _vectorDatabaseService.IsConnectedAsync();

                if (!embeddingAvailable || !dbConnected)
                {
                    Console.WriteLine("✗ Required services not available");
                    return ExitCodes.ServiceUnavailable;
                }

                var count = await _vectorDatabaseService.GetEmbeddingCountAsync();
                if (count == 0)
                {
                    Console.WriteLine("⚠ No embeddings in database to benchmark");
                    return ExitCodes.Success;
                }

                Console.WriteLine($"Benchmarking similarity search with {count} embeddings...");

                const int iterations = 10;
                var durations = new List<double>();

                // Generate test embedding once
                var testQuery = "{\"benchmark\": \"test\"}";
                var queryEmbedding = await _embeddingService.GenerateEmbeddingAsync(testQuery);

                // Run benchmark iterations
                for (int i = 0; i < iterations; i++)
                {
                    var startTime = DateTime.UtcNow;
                    await _vectorDatabaseService.FindSimilarAsync(queryEmbedding, maxResults: 5, threshold: 0.0f);
                    var duration = (DateTime.UtcNow - startTime).TotalMilliseconds;
                    durations.Add(duration);

                    if (options.VerboseMode)
                    {
                        Console.WriteLine($"  Iteration {i + 1}: {duration:F2}ms");
                    }
                }

                var avgDuration = durations.Average();
                var minDuration = durations.Min();
                var maxDuration = durations.Max();
                var opsPerSecond = 1000.0 / avgDuration;

                Console.WriteLine($"✓ Benchmark completed");
                Console.WriteLine($"  Iterations: {iterations}");
                Console.WriteLine($"  Average: {avgDuration:F2}ms");
                Console.WriteLine($"  Min: {minDuration:F2}ms");
                Console.WriteLine($"  Max: {maxDuration:F2}ms");
                Console.WriteLine($"  Throughput: {opsPerSecond:F2} ops/sec");

                return ExitCodes.Success;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ Benchmark failed: {ex.Message}");
                if (options.VerboseMode)
                {
                    Console.WriteLine($"Stack trace: {ex.StackTrace}");
                }
                return ExitCodes.GeneralError;
            }
        }

        private async Task<int> ExecuteBenchmarkVectorOperationsAsync(CommandLineOptions options)
        {
            Console.WriteLine("=== Benchmark: Vector Operations ===");
            Console.WriteLine();

            try
            {
                var dbConnected = await _vectorDatabaseService.IsConnectedAsync();
                if (!dbConnected)
                {
                    Console.WriteLine("✗ ScyllaDB not connected");
                    return ExitCodes.ServiceUnavailable;
                }

                Console.WriteLine("Benchmarking vector storage operations...");

                const int iterations = 10;
                var durations = new List<double>();

                // Create test embedding
                var testEmbedding = new float[384]; // Standard embedding size
                for (int i = 0; i < testEmbedding.Length; i++)
                {
                    testEmbedding[i] = (float)Random.Shared.NextDouble();
                }

                // Run benchmark iterations
                for (int i = 0; i < iterations; i++)
                {
                    var testId = $"benchmark_test_{Guid.NewGuid()}";
                    var startTime = DateTime.UtcNow;
                    
                    await _vectorDatabaseService.StoreEmbeddingAsync(
                        testId,
                        testEmbedding,
                        "{\"test\": \"data\"}",
                        "Benchmark test embedding"
                    );
                    
                    var duration = (DateTime.UtcNow - startTime).TotalMilliseconds;
                    durations.Add(duration);

                    // Clean up
                    await _vectorDatabaseService.DeleteEmbeddingAsync(testId);

                    if (options.VerboseMode)
                    {
                        Console.WriteLine($"  Iteration {i + 1}: {duration:F2}ms");
                    }
                }

                var avgDuration = durations.Average();
                var minDuration = durations.Min();
                var maxDuration = durations.Max();
                var opsPerSecond = 1000.0 / avgDuration;

                Console.WriteLine($"✓ Benchmark completed");
                Console.WriteLine($"  Iterations: {iterations}");
                Console.WriteLine($"  Average: {avgDuration:F2}ms");
                Console.WriteLine($"  Min: {minDuration:F2}ms");
                Console.WriteLine($"  Max: {maxDuration:F2}ms");
                Console.WriteLine($"  Throughput: {opsPerSecond:F2} ops/sec");

                return ExitCodes.Success;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ Benchmark failed: {ex.Message}");
                if (options.VerboseMode)
                {
                    Console.WriteLine($"Stack trace: {ex.StackTrace}");
                }
                return ExitCodes.GeneralError;
            }
        }

        private async Task<int> ExecuteBenchmarkEmbeddingAsync(CommandLineOptions options)
        {
            Console.WriteLine("=== Benchmark: Embedding Generation ===");
            Console.WriteLine();

            try
            {
                var embeddingAvailable = await _embeddingService.IsEmbeddingServiceAvailableAsync();
                if (!embeddingAvailable)
                {
                    Console.WriteLine("✗ Embedding service not available");
                    return ExitCodes.ServiceUnavailable;
                }

                Console.WriteLine($"Benchmarking embedding generation with model: {_embeddingService.GetEmbeddingModelName()}");

                const int iterations = 10;
                var durations = new List<double>();
                var testJson = "{\"test\": \"data\", \"benchmark\": true, \"value\": 123}";

                // Run benchmark iterations
                for (int i = 0; i < iterations; i++)
                {
                    var startTime = DateTime.UtcNow;
                    var embedding = await _embeddingService.GenerateEmbeddingAsync(testJson);
                    var duration = (DateTime.UtcNow - startTime).TotalMilliseconds;
                    durations.Add(duration);

                    if (options.VerboseMode)
                    {
                        Console.WriteLine($"  Iteration {i + 1}: {duration:F2}ms (dimensions: {embedding.Length})");
                    }
                }

                var avgDuration = durations.Average();
                var minDuration = durations.Min();
                var maxDuration = durations.Max();
                var opsPerSecond = 1000.0 / avgDuration;

                Console.WriteLine($"✓ Benchmark completed");
                Console.WriteLine($"  Iterations: {iterations}");
                Console.WriteLine($"  Average: {avgDuration:F2}ms");
                Console.WriteLine($"  Min: {minDuration:F2}ms");
                Console.WriteLine($"  Max: {maxDuration:F2}ms");
                Console.WriteLine($"  Throughput: {opsPerSecond:F2} ops/sec");

                return ExitCodes.Success;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ Benchmark failed: {ex.Message}");
                if (options.VerboseMode)
                {
                    Console.WriteLine($"Stack trace: {ex.StackTrace}");
                }
                return ExitCodes.GeneralError;
            }
        }

        #endregion

        #region Helper Methods

        private void DisplayError(string message)
        {
            Console.WriteLine();
            Console.WriteLine($"ERROR: {message}");
            Console.WriteLine();
            Console.WriteLine("For more help, run: dotnet JSON-Whisperer.dll --help");
        }

        #endregion
    }
}
