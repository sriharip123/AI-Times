using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Text.Json;
using JSON_Whisperer.Interfaces;
using JSON_Whisperer.Models;

namespace JSON_Whisperer.Services
{
    /// <summary>
    /// Service for managing knowledge base operations including reinitialization and validation
    /// </summary>
    public class KnowledgeBaseManagementService : IKnowledgeBaseManagementService
    {
        private readonly ILogger<KnowledgeBaseManagementService> _logger;
        private readonly IVectorDatabaseService _vectorDatabaseService;
        private readonly IEmbeddingService _embeddingService;
        private readonly IKnowledgeBaseService _knowledgeBaseService;
        private readonly VectorSettings _vectorSettings;

        public KnowledgeBaseManagementService(
            ILogger<KnowledgeBaseManagementService> logger,
            IVectorDatabaseService vectorDatabaseService,
            IEmbeddingService embeddingService,
            IKnowledgeBaseService knowledgeBaseService,
            AppSettings appSettings)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _vectorDatabaseService = vectorDatabaseService ?? throw new ArgumentNullException(nameof(vectorDatabaseService));
            _embeddingService = embeddingService ?? throw new ArgumentNullException(nameof(embeddingService));
            _knowledgeBaseService = knowledgeBaseService ?? throw new ArgumentNullException(nameof(knowledgeBaseService));
            _vectorSettings = appSettings?.Vector ?? throw new ArgumentNullException(nameof(appSettings));
        }

        /// <summary>
        /// Reinitializes the knowledge base by clearing all embeddings and regenerating them from JSON files
        /// </summary>
        public async Task<ReinitializeResult> ReinitializeAsync()
        {
            var stopwatch = Stopwatch.StartNew();
            var result = new ReinitializeResult();

            try
            {
                _logger.LogInformation("Starting knowledge base reinitialization...");

                // Step 1: Check if vector database is connected
                if (!await _vectorDatabaseService.IsConnectedAsync())
                {
                    var errorMsg = "Vector database is not connected. Cannot reinitialize knowledge base.";
                    _logger.LogError(errorMsg);
                    result.ErrorMessages.Add(errorMsg);
                    result.Errors++;
                    result.Duration = stopwatch.Elapsed;
                    return result;
                }

                // Step 2: Check if embedding service is available
                if (!await _embeddingService.IsEmbeddingServiceAvailableAsync())
                {
                    var errorMsg = "Embedding service is not available. Cannot reinitialize knowledge base.";
                    _logger.LogError(errorMsg);
                    result.ErrorMessages.Add(errorMsg);
                    result.Errors++;
                    result.Duration = stopwatch.Elapsed;
                    return result;
                }

                // Step 3: Clear all existing embeddings
                Console.WriteLine("Clearing existing embeddings...");
                result.EmbeddingsCleared = await ClearAllEmbeddingsAsync();
                Console.WriteLine($"Cleared {result.EmbeddingsCleared} existing embeddings.");

                // Step 4: Scan for JSON files
                Console.WriteLine("Scanning for JSON files...");
                var jsonFiles = await ScanJsonFilesAsync();
                Console.WriteLine($"Found {jsonFiles.Count} JSON files.");

                if (jsonFiles.Count == 0)
                {
                    var warningMsg = "No JSON files found in AppData directory.";
                    _logger.LogWarning(warningMsg);
                    result.ErrorMessages.Add(warningMsg);
                    result.Duration = stopwatch.Elapsed;
                    return result;
                }

                // Step 5: Load and process examples
                Console.WriteLine("Loading JSON examples...");
                var examples = await _knowledgeBaseService.LoadExamplesAsync();
                result.FilesProcessed = examples.Count;

                if (examples.Count == 0)
                {
                    var errorMsg = "Failed to load any valid JSON examples.";
                    _logger.LogError(errorMsg);
                    result.ErrorMessages.Add(errorMsg);
                    result.Errors++;
                    result.Duration = stopwatch.Elapsed;
                    return result;
                }

                // Step 6: Generate and store embeddings
                Console.WriteLine($"Generating embeddings for {examples.Count} examples...");
                var processedCount = 0;

                foreach (var example in examples)
                {
                    try
                    {
                        processedCount++;
                        Console.Write($"\rProcessing {processedCount}/{examples.Count}: {Path.GetFileName(example.FilePath)}...");

                        // Generate embedding
                        var embedding = await _embeddingService.GenerateEmbeddingAsync(example.JsonContent);
                        if (embedding == null || embedding.Length == 0)
                        {
                            var errorMsg = $"Failed to generate embedding for {example.Id}";
                            _logger.LogWarning(errorMsg);
                            result.ErrorMessages.Add(errorMsg);
                            result.Errors++;
                            continue;
                        }

                        // Store embedding
                        var metadata = new Dictionary<string, string>
                        {
                            ["source"] = "knowledge_base",
                            ["file_path"] = example.FilePath,
                            ["embedding_model"] = _embeddingService.GetEmbeddingModelName(),
                            ["reinitialized_at"] = DateTime.UtcNow.ToString("o")
                        };

                        var success = await _vectorDatabaseService.StoreEmbeddingAsync(
                            example.Id,
                            embedding,
                            example.JsonContent,
                            example.Description,
                            metadata);

                        if (success)
                        {
                            result.EmbeddingsCreated++;
                        }
                        else
                        {
                            var errorMsg = $"Failed to store embedding for {example.Id}";
                            _logger.LogWarning(errorMsg);
                            result.ErrorMessages.Add(errorMsg);
                            result.Errors++;
                        }
                    }
                    catch (Exception ex)
                    {
                        var errorMsg = $"Error processing {example.Id}: {ex.Message}";
                        _logger.LogError(ex, errorMsg);
                        result.ErrorMessages.Add(errorMsg);
                        result.Errors++;
                    }
                }

                Console.WriteLine(); // New line after progress

                stopwatch.Stop();
                result.Duration = stopwatch.Elapsed;

                // Display summary
                Console.WriteLine();
                Console.WriteLine("=== Reinitialization Summary ===");
                Console.WriteLine($"Files Processed: {result.FilesProcessed}");
                Console.WriteLine($"Embeddings Cleared: {result.EmbeddingsCleared}");
                Console.WriteLine($"Embeddings Created: {result.EmbeddingsCreated}");
                Console.WriteLine($"Errors: {result.Errors}");
                Console.WriteLine($"Duration: {result.Duration.TotalSeconds:F2} seconds");

                if (result.Errors > 0)
                {
                    Console.WriteLine();
                    Console.WriteLine("Errors encountered:");
                    foreach (var error in result.ErrorMessages.Take(10))
                    {
                        Console.WriteLine($"  - {error}");
                    }
                    if (result.ErrorMessages.Count > 10)
                    {
                        Console.WriteLine($"  ... and {result.ErrorMessages.Count - 10} more errors");
                    }
                }

                _logger.LogInformation(
                    "Knowledge base reinitialization completed. " +
                    "Processed: {ProcessedCount}, Created: {CreatedCount}, Errors: {ErrorCount}, Duration: {Duration}s",
                    result.FilesProcessed, result.EmbeddingsCreated, result.Errors, result.Duration.TotalSeconds);

                return result;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                result.Duration = stopwatch.Elapsed;
                var errorMsg = $"Unexpected error during reinitialization: {ex.Message}";
                _logger.LogError(ex, errorMsg);
                result.ErrorMessages.Add(errorMsg);
                result.Errors++;
                return result;
            }
        }

        /// <summary>
        /// Validates the knowledge base by checking JSON files and their descriptions
        /// </summary>
        public async Task<KnowledgeBaseValidationResult> ValidateAsync()
        {
            var stopwatch = Stopwatch.StartNew();
            var result = new KnowledgeBaseValidationResult();

            try
            {
                _logger.LogInformation("Starting knowledge base validation...");
                Console.WriteLine("Validating knowledge base...");

                // Step 1: Scan for JSON files
                var jsonFiles = await ScanJsonFilesAsync();
                result.TotalFiles = jsonFiles.Count;

                Console.WriteLine($"Found {jsonFiles.Count} JSON files.");

                if (jsonFiles.Count == 0)
                {
                    result.Warnings.Add("No JSON files found in AppData directory.");
                    result.Duration = stopwatch.Elapsed;
                    return result;
                }

                // Step 2: Validate each file
                foreach (var filePath in jsonFiles)
                {
                    try
                    {
                        // Check if file exists
                        if (!File.Exists(filePath))
                        {
                            result.Errors.Add($"File not found: {filePath}");
                            result.InvalidFiles++;
                            continue;
                        }

                        // Try to read and parse JSON
                        var jsonContent = await File.ReadAllTextAsync(filePath);
                        
                        try
                        {
                            using var document = JsonDocument.Parse(jsonContent);
                            result.ValidFiles++;

                            // Check for description metadata
                            var hasDescription = false;
                            if (document.RootElement.TryGetProperty("_description", out _))
                            {
                                hasDescription = true;
                            }
                            else if (document.RootElement.TryGetProperty("_metadata", out var metadataProperty))
                            {
                                if (metadataProperty.TryGetProperty("description", out _))
                                {
                                    hasDescription = true;
                                }
                            }

                            if (!hasDescription)
                            {
                                result.MissingDescriptions++;
                                result.Warnings.Add($"Missing description metadata: {Path.GetFileName(filePath)}");
                            }

                            // Check for empty JSON
                            if (jsonContent.Trim().Length < 3) // "{}" is minimum valid JSON
                            {
                                result.Warnings.Add($"Nearly empty JSON file: {Path.GetFileName(filePath)}");
                            }
                        }
                        catch (JsonException ex)
                        {
                            result.InvalidFiles++;
                            result.Errors.Add($"Invalid JSON in {Path.GetFileName(filePath)}: {ex.Message}");
                        }
                    }
                    catch (Exception ex)
                    {
                        result.InvalidFiles++;
                        result.Errors.Add($"Error reading {Path.GetFileName(filePath)}: {ex.Message}");
                    }
                }

                stopwatch.Stop();
                result.Duration = stopwatch.Elapsed;

                // Display summary
                Console.WriteLine();
                Console.WriteLine("=== Validation Summary ===");
                Console.WriteLine($"Total Files: {result.TotalFiles}");
                Console.WriteLine($"Valid Files: {result.ValidFiles}");
                Console.WriteLine($"Invalid Files: {result.InvalidFiles}");
                Console.WriteLine($"Missing Descriptions: {result.MissingDescriptions}");
                Console.WriteLine($"Errors: {result.Errors.Count}");
                Console.WriteLine($"Warnings: {result.Warnings.Count}");
                Console.WriteLine($"Duration: {result.Duration.TotalSeconds:F2} seconds");

                if (result.Errors.Count > 0)
                {
                    Console.WriteLine();
                    Console.WriteLine("Errors:");
                    foreach (var error in result.Errors.Take(10))
                    {
                        Console.WriteLine($"  - {error}");
                    }
                    if (result.Errors.Count > 10)
                    {
                        Console.WriteLine($"  ... and {result.Errors.Count - 10} more errors");
                    }
                }

                if (result.Warnings.Count > 0)
                {
                    Console.WriteLine();
                    Console.WriteLine("Warnings:");
                    foreach (var warning in result.Warnings.Take(10))
                    {
                        Console.WriteLine($"  - {warning}");
                    }
                    if (result.Warnings.Count > 10)
                    {
                        Console.WriteLine($"  ... and {result.Warnings.Count - 10} more warnings");
                    }
                }

                _logger.LogInformation(
                    "Knowledge base validation completed. " +
                    "Total: {TotalFiles}, Valid: {ValidFiles}, Invalid: {InvalidFiles}, Duration: {Duration}s",
                    result.TotalFiles, result.ValidFiles, result.InvalidFiles, result.Duration.TotalSeconds);

                return result;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                result.Duration = stopwatch.Elapsed;
                var errorMsg = $"Unexpected error during validation: {ex.Message}";
                _logger.LogError(ex, errorMsg);
                result.Errors.Add(errorMsg);
                return result;
            }
        }

        /// <summary>
        /// Clears all embeddings from the vector database
        /// </summary>
        public async Task<int> ClearAllEmbeddingsAsync()
        {
            try
            {
                _logger.LogInformation("Clearing all embeddings from vector database...");
                var count = await _vectorDatabaseService.DeleteAllEmbeddingsAsync();
                _logger.LogInformation("Cleared {Count} embeddings", count);
                return count;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to clear embeddings");
                return 0;
            }
        }

        /// <summary>
        /// Scans the AppData directory for JSON files
        /// </summary>
        public async Task<List<string>> ScanJsonFilesAsync()
        {
            var jsonFiles = new List<string>();

            try
            {
                var appDataPath = Path.GetFullPath(_vectorSettings.AppDataPath);
                _logger.LogInformation("Scanning for JSON files in: {AppDataPath}", appDataPath);

                if (!Directory.Exists(appDataPath))
                {
                    _logger.LogWarning("AppData directory does not exist: {AppDataPath}", appDataPath);
                    return jsonFiles;
                }

                jsonFiles = Directory.GetFiles(appDataPath, "*.json", SearchOption.AllDirectories).ToList();
                _logger.LogInformation("Found {FileCount} JSON files", jsonFiles.Count);

                return await Task.FromResult(jsonFiles);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to scan for JSON files");
                return jsonFiles;
            }
        }
    }
}
