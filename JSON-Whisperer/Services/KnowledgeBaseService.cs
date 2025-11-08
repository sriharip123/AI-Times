using Microsoft.Extensions.Logging;
using System.Text.Json;
using JSON_Whisperer.Interfaces;
using JSON_Whisperer.Models;

namespace JSON_Whisperer.Services
{
    /// <summary>
    /// Service for managing the knowledge base of JSON examples
    /// </summary>
    public class KnowledgeBaseService : IKnowledgeBaseService
    {
        private readonly ILogger<KnowledgeBaseService> _logger;
        private readonly IEmbeddingService _embeddingService;
        private readonly IVectorDatabaseService _vectorDatabaseService;
        private readonly VectorSettings _vectorSettings;

        public KnowledgeBaseService(
            ILogger<KnowledgeBaseService> logger,
            IEmbeddingService embeddingService,
            IVectorDatabaseService vectorDatabaseService,
            AppSettings appSettings)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _embeddingService = embeddingService ?? throw new ArgumentNullException(nameof(embeddingService));
            _vectorDatabaseService = vectorDatabaseService ?? throw new ArgumentNullException(nameof(vectorDatabaseService));
            _vectorSettings = appSettings?.Vector ?? throw new ArgumentNullException(nameof(appSettings));
        }

        /// <summary>
        /// Loads JSON examples from the AppData directory
        /// </summary>
        /// <returns>List of JSON examples with their descriptions</returns>
        public async Task<List<JsonExample>> LoadExamplesAsync()
        {
            var examples = new List<JsonExample>();

            try
            {
                var appDataPath = Path.GetFullPath(_vectorSettings.AppDataPath);
                _logger.LogInformation("Loading JSON examples from: {AppDataPath}", appDataPath);

                if (!Directory.Exists(appDataPath))
                {
                    _logger.LogWarning("AppData directory does not exist: {AppDataPath}", appDataPath);
                    return examples;
                }

                var jsonFiles = Directory.GetFiles(appDataPath, "*.json", SearchOption.AllDirectories);
                _logger.LogInformation("Found {FileCount} JSON files in AppData directory", jsonFiles.Length);

                foreach (var filePath in jsonFiles)
                {
                    try
                    {
                        var example = await LoadJsonExampleFromFileAsync(filePath);
                        if (example != null)
                        {
                            examples.Add(example);
                            _logger.LogDebug("Loaded JSON example: {Id} from {FilePath}", example.Id, example.FilePath);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to load JSON example from file: {FilePath}", filePath);
                    }
                }

                _logger.LogInformation("Successfully loaded {ExampleCount} JSON examples", examples.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load JSON examples from AppData directory");
                throw;
            }

            return examples;
        }

        /// <summary>
        /// Initializes the vector database with knowledge base examples
        /// </summary>
        /// <returns>Task representing the initialization operation</returns>
        public async Task InitializeVectorDatabaseAsync()
        {
            try
            {
                _logger.LogInformation("Initializing vector database with knowledge base examples");

                // Check if vector database is available
                if (!await _vectorDatabaseService.IsConnectedAsync())
                {
                    _logger.LogWarning("Vector database is not connected. Skipping knowledge base initialization.");
                    return;
                }

                // Check if embedding service is available
                if (!await _embeddingService.IsEmbeddingServiceAvailableAsync())
                {
                    _logger.LogWarning("Embedding service is not available. Skipping knowledge base initialization.");
                    return;
                }

                // Load JSON examples from AppData directory
                var examples = await LoadExamplesAsync();
                if (examples.Count == 0)
                {
                    _logger.LogInformation("No JSON examples found to initialize vector database");
                    return;
                }

                var processedCount = 0;
                var skippedCount = 0;
                var errorCount = 0;

                foreach (var example in examples)
                {
                    try
                    {
                        // Check if embedding already exists to avoid duplicates
                        if (await _vectorDatabaseService.EmbeddingExistsAsync(example.Id))
                        {
                            _logger.LogDebug("Embedding already exists for example: {Id}", example.Id);
                            skippedCount++;
                            continue;
                        }

                        // Generate embedding for the JSON content
                        var embedding = await _embeddingService.GenerateEmbeddingAsync(example.JsonContent);
                        if (embedding == null || embedding.Length == 0)
                        {
                            _logger.LogWarning("Failed to generate embedding for example: {Id}", example.Id);
                            errorCount++;
                            continue;
                        }

                        // Store embedding in vector database
                        var metadata = new Dictionary<string, string>
                        {
                            ["source"] = "knowledge_base",
                            ["file_path"] = example.FilePath,
                            ["embedding_model"] = _embeddingService.GetEmbeddingModelName()
                        };

                        var success = await _vectorDatabaseService.StoreEmbeddingAsync(
                            example.Id,
                            embedding,
                            example.JsonContent,
                            example.Description,
                            metadata);

                        if (success)
                        {
                            processedCount++;
                            _logger.LogDebug("Successfully stored embedding for example: {Id}", example.Id);
                        }
                        else
                        {
                            _logger.LogWarning("Failed to store embedding for example: {Id}", example.Id);
                            errorCount++;
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error processing example: {Id}", example.Id);
                        errorCount++;
                    }
                }

                _logger.LogInformation(
                    "Vector database initialization completed. Processed: {ProcessedCount}, Skipped: {SkippedCount}, Errors: {ErrorCount}",
                    processedCount, skippedCount, errorCount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize vector database with knowledge base examples");
                throw;
            }
        }

        /// <summary>
        /// Loads a JSON example from a file, extracting description from various sources
        /// </summary>
        /// <param name="filePath">Path to the JSON file</param>
        /// <returns>JsonExample object or null if loading failed</returns>
        private async Task<JsonExample?> LoadJsonExampleFromFileAsync(string filePath)
        {
            try
            {
                var jsonContent = await File.ReadAllTextAsync(filePath);
                
                // Validate JSON format
                using var document = JsonDocument.Parse(jsonContent);
                
                // Generate unique ID based on file path and content hash
                var fileInfo = new FileInfo(filePath);
                var contentHash = ComputeContentHash(jsonContent);
                var id = $"{fileInfo.Name}_{contentHash}";

                // Extract description from various sources
                var description = ExtractDescription(filePath, document);

                return new JsonExample
                {
                    Id = id,
                    JsonContent = jsonContent,
                    Description = description,
                    FilePath = filePath
                };
            }
            catch (JsonException ex)
            {
                _logger.LogWarning(ex, "Invalid JSON format in file: {FilePath}", filePath);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load JSON example from file: {FilePath}", filePath);
                return null;
            }
        }

        /// <summary>
        /// Extracts description from file name, directory structure, or JSON metadata
        /// </summary>
        /// <param name="filePath">Path to the JSON file</param>
        /// <param name="document">Parsed JSON document</param>
        /// <returns>Description string</returns>
        private string ExtractDescription(string filePath, JsonDocument document)
        {
            var descriptions = new List<string>();

            // Try to extract description from JSON metadata
            if (document.RootElement.TryGetProperty("_description", out var descProperty))
            {
                descriptions.Add($"Description: {descProperty.GetString()}");
            }

            if (document.RootElement.TryGetProperty("_metadata", out var metadataProperty))
            {
                if (metadataProperty.TryGetProperty("description", out var metaDescProperty))
                {
                    descriptions.Add($"Metadata: {metaDescProperty.GetString()}");
                }
            }

            // Extract information from file path structure
            var fileInfo = new FileInfo(filePath);
            var directoryName = fileInfo.Directory?.Name;
            var fileName = Path.GetFileNameWithoutExtension(fileInfo.Name);

            // Clean up file name (replace underscores/hyphens with spaces, capitalize)
            var cleanFileName = fileName.Replace('_', ' ').Replace('-', ' ');
            cleanFileName = System.Globalization.CultureInfo.CurrentCulture.TextInfo.ToTitleCase(cleanFileName.ToLower());

            descriptions.Add($"File: {cleanFileName}");

            if (!string.IsNullOrEmpty(directoryName) && directoryName != "AppData")
            {
                var cleanDirName = directoryName.Replace('_', ' ').Replace('-', ' ');
                cleanDirName = System.Globalization.CultureInfo.CurrentCulture.TextInfo.ToTitleCase(cleanDirName.ToLower());
                descriptions.Add($"Category: {cleanDirName}");
            }

            // Analyze JSON structure to provide additional context
            var structureInfo = AnalyzeJsonStructure(document.RootElement);
            if (!string.IsNullOrEmpty(structureInfo))
            {
                descriptions.Add($"Structure: {structureInfo}");
            }

            return string.Join(". ", descriptions.Where(d => !string.IsNullOrWhiteSpace(d)));
        }

        /// <summary>
        /// Analyzes JSON structure to provide contextual information
        /// </summary>
        /// <param name="element">JSON element to analyze</param>
        /// <returns>Structure description</returns>
        private string AnalyzeJsonStructure(JsonElement element)
        {
            var info = new List<string>();

            switch (element.ValueKind)
            {
                case JsonValueKind.Object:
                    var propertyCount = element.EnumerateObject().Count();
                    info.Add($"Object with {propertyCount} properties");

                    // Look for common patterns
                    var properties = element.EnumerateObject().Select(p => p.Name.ToLower()).ToList();
                    
                    if (properties.Contains("id") || properties.Contains("_id"))
                        info.Add("contains identifier");
                    
                    if (properties.Contains("name") || properties.Contains("title"))
                        info.Add("has name/title");
                    
                    if (properties.Contains("email") || properties.Contains("phone"))
                        info.Add("contact information");
                    
                    if (properties.Contains("address") || properties.Contains("location"))
                        info.Add("location data");
                    
                    if (properties.Contains("timestamp") || properties.Contains("created_at") || properties.Contains("date"))
                        info.Add("temporal data");

                    break;

                case JsonValueKind.Array:
                    var arrayLength = element.GetArrayLength();
                    info.Add($"Array with {arrayLength} items");
                    
                    if (arrayLength > 0)
                    {
                        var firstElement = element.EnumerateArray().First();
                        info.Add($"containing {firstElement.ValueKind.ToString().ToLower()} elements");
                    }
                    break;
            }

            return string.Join(", ", info);
        }

        /// <summary>
        /// Computes a hash of the JSON content for generating unique IDs
        /// </summary>
        /// <param name="content">JSON content</param>
        /// <returns>Hash string</returns>
        private static string ComputeContentHash(string content)
        {
            using var sha256 = System.Security.Cryptography.SHA256.Create();
            var hashBytes = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(content));
            return Convert.ToHexString(hashBytes)[..8]; // Take first 8 characters
        }
    }
}