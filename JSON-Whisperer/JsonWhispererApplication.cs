using Microsoft.Extensions.Logging;
using JSON_Whisperer.Interfaces;
using JSON_Whisperer.Models;
using JSON_Whisperer.Services;

namespace JSON_Whisperer
{
    /// <summary>
    /// Main application orchestrator that coordinates all services
    /// </summary>
    public class JsonWhispererApplication
    {
        private readonly IInputHandler _inputHandler;
        private readonly IJsonAnalyzer _jsonAnalyzer;
        private readonly IAiService _aiService;
        private readonly IOutputFormatter _outputFormatter;
        private readonly ILogger<JsonWhispererApplication> _logger;
        private readonly AppSettings _appSettings;
        private readonly PerformanceMonitoringService _performanceMonitoring;
        private readonly DiagnosticService _diagnosticService;
        private readonly IVectorDatabaseService _vectorDatabaseService;
        private readonly IKnowledgeBaseService _knowledgeBaseService;
        private readonly ISimilarityService _similarityService;

        public JsonWhispererApplication(
            IInputHandler inputHandler,
            IJsonAnalyzer jsonAnalyzer,
            IAiService aiService,
            IOutputFormatter outputFormatter,
            ILogger<JsonWhispererApplication> logger,
            AppSettings appSettings,
            PerformanceMonitoringService performanceMonitoring,
            DiagnosticService diagnosticService,
            IVectorDatabaseService vectorDatabaseService,
            IKnowledgeBaseService knowledgeBaseService,
            ISimilarityService similarityService)
        {
            _inputHandler = inputHandler ?? throw new ArgumentNullException(nameof(inputHandler));
            _jsonAnalyzer = jsonAnalyzer ?? throw new ArgumentNullException(nameof(jsonAnalyzer));
            _aiService = aiService ?? throw new ArgumentNullException(nameof(aiService));
            _outputFormatter = outputFormatter ?? throw new ArgumentNullException(nameof(outputFormatter));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _appSettings = appSettings ?? throw new ArgumentNullException(nameof(appSettings));
            _performanceMonitoring = performanceMonitoring ?? throw new ArgumentNullException(nameof(performanceMonitoring));
            _diagnosticService = diagnosticService ?? throw new ArgumentNullException(nameof(diagnosticService));
            _vectorDatabaseService = vectorDatabaseService ?? throw new ArgumentNullException(nameof(vectorDatabaseService));
            _knowledgeBaseService = knowledgeBaseService ?? throw new ArgumentNullException(nameof(knowledgeBaseService));
            _similarityService = similarityService ?? throw new ArgumentNullException(nameof(similarityService));
        }

        /// <summary>
        /// Main application entry point that orchestrates the entire workflow
        /// </summary>
        /// <param name="args">Command line arguments</param>
        /// <returns>Exit code (0 for success, 1 for error)</returns>
        public async Task<int> RunAsync(string[] args)
        {
            using var overallTimer = _performanceMonitoring.StartOperation("Application.RunAsync");
            
            try
            {
                _logger.LogInformation("JSON-Whisperer application starting...");
                
                // Log diagnostic information if verbose mode is enabled
                if (_appSettings.Application.VerboseMode)
                {
                    _diagnosticService.LogDiagnosticInfo();
                }

                // Record initial memory usage
                _performanceMonitoring.RecordMemoryUsage("Application Start");

                // Initialize vector database and knowledge base if enabled
                await InitializeVectorServicesAsync();

                // Step 1: Get JSON input
                _logger.LogDebug("Getting JSON input from arguments or stdin");
                string jsonInput;
                using (var inputTimer = _performanceMonitoring.StartOperation("Input.GetJsonInput"))
                {
                    try
                    {
                        jsonInput = await _inputHandler.GetJsonInputAsync(args);
                        _logger.LogInformation("JSON input received, size: {Size} bytes", jsonInput.Length);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to get JSON input");
                        _outputFormatter.DisplayError($"Input error: {ex.Message}");
                        return 1;
                    }
                }

                // Step 2: Validate JSON input
                _logger.LogDebug("Validating JSON input");
                using (var validationTimer = _performanceMonitoring.StartOperation("Input.ValidateInput"))
                {
                    if (!_inputHandler.ValidateInput(jsonInput))
                    {
                        _logger.LogError("Invalid JSON input provided");
                        _outputFormatter.DisplayError("Invalid JSON format. Please provide valid JSON input.");
                        return 1;
                    }
                }

                // Check JSON size limit
                if (jsonInput.Length > _appSettings.Application.MaxJsonSizeBytes)
                {
                    _logger.LogWarning("JSON input size ({Size} bytes) exceeds maximum allowed size ({MaxSize} bytes)", 
                        jsonInput.Length, _appSettings.Application.MaxJsonSizeBytes);
                    _outputFormatter.DisplayError($"JSON input is too large. Maximum size allowed: {_appSettings.Application.MaxJsonSizeBytes} bytes");
                    return 1;
                }

                // Step 3: Analyze JSON structure
                _logger.LogDebug("Analyzing JSON structure");
                JsonAnalysisResult analysis;
                using (var analysisTimer = _performanceMonitoring.StartOperation("JsonAnalyzer.AnalyzeStructure"))
                {
                    try
                    {
                        analysis = _jsonAnalyzer.AnalyzeStructure(jsonInput);
                        _logger.LogInformation("JSON analysis completed. Properties: {PropertyCount}, Max Depth: {MaxDepth}, Size: {Size} bytes", 
                            analysis.TotalProperties, analysis.MaxDepth, analysis.EstimatedSize);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to analyze JSON structure");
                        _outputFormatter.DisplayError($"JSON analysis error: {ex.Message}");
                        return 1;
                    }
                }

                // Record memory usage after analysis
                _performanceMonitoring.RecordMemoryUsage("After JSON Analysis");

                // Step 4: Perform similarity matching (if enabled)
                _logger.LogDebug("Performing similarity matching");
                SimilarityResult? similarityResult = null;
                using (var similarityTimer = _performanceMonitoring.StartOperation("SimilarityService.FindSimilar"))
                {
                    try
                    {
                        if (_appSettings.Vector.EnableSimilarityMatching && await _similarityService.IsAvailableAsync())
                        {
                            similarityResult = await _similarityService.FindSimilarJsonAsync(jsonInput);
                            _logger.LogInformation("Similarity matching completed. Found {MatchCount} matches with highest score {HighestScore:F3}", 
                                similarityResult.Matches.Count, similarityResult.HighestScore);
                        }
                        else
                        {
                            _logger.LogDebug("Similarity matching is disabled or unavailable");
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Similarity matching failed, continuing without similarity results: {ErrorMessage}", ex.Message);
                        // Continue without similarity results - this is not a critical failure
                    }
                }

                // Record memory usage after similarity matching
                _performanceMonitoring.RecordMemoryUsage("After Similarity Matching");

                // Step 6: Check AI service availability
                _logger.LogDebug("Checking AI service availability");
                bool aiAvailable;
                using (var availabilityTimer = _performanceMonitoring.StartOperation("AiService.IsAvailable"))
                {
                    aiAvailable = await _aiService.IsAvailableAsync();
                }

                if (!aiAvailable)
                {
                    _logger.LogWarning("AI service (Ollama) is not available");
                    _outputFormatter.DisplayError(
                        $"Ollama service is not available at {_appSettings.Ollama.BaseUrl}. " +
                        "Please ensure Ollama is running and the Mistral model is installed.");
                    return 1;
                }

                // Step 7: Generate AI summary
                _logger.LogDebug("Generating AI summary");
                string summary;
                using (var summaryTimer = _performanceMonitoring.StartOperation("AiService.GenerateSummary"))
                {
                    try
                    {
                        summary = await _aiService.GenerateSummaryAsync(analysis, jsonInput);
                        _logger.LogInformation("AI summary generated successfully, length: {Length} characters", summary.Length);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to generate AI summary");
                        _outputFormatter.DisplayError($"AI service error: {ex.Message}");
                        return 1;
                    }
                }

                // Record memory usage after AI processing
                _performanceMonitoring.RecordMemoryUsage("After AI Summary Generation");

                // Step 8: Display results
                _logger.LogDebug("Displaying results to user");
                using (var outputTimer = _performanceMonitoring.StartOperation("OutputFormatter.DisplayResults"))
                {
                    _outputFormatter.DisplayResults(jsonInput, summary, analysis, similarityResult);
                }

                // Log performance summary if enabled
                if (_appSettings.Application.EnablePerformanceMetrics)
                {
                    _performanceMonitoring.LogPerformanceSummary();
                }

                _logger.LogInformation("JSON-Whisperer application completed successfully");
                return 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled exception in application workflow");
                
                // Log diagnostic information on error
                if (_appSettings.Application.VerboseMode)
                {
                    _logger.LogError("Collecting diagnostic information due to error...");
                    _diagnosticService.LogDiagnosticInfo();
                }
                
                _outputFormatter.DisplayError($"Unexpected error: {ex.Message}");
                return 1;
            }
        }

        /// <summary>
        /// Initializes vector database and knowledge base services with graceful fallback
        /// </summary>
        /// <returns>Task representing the initialization operation</returns>
        private async Task InitializeVectorServicesAsync()
        {
            // Skip initialization if similarity matching is disabled
            if (!_appSettings.Vector.EnableSimilarityMatching)
            {
                _logger.LogInformation("Vector similarity matching is disabled. Skipping vector services initialization.");
                return;
            }

            // Skip initialization if knowledge base initialization is disabled
            if (!_appSettings.Vector.InitializeKnowledgeBase)
            {
                _logger.LogInformation("Knowledge base initialization is disabled. Skipping vector database seeding.");
                return;
            }

            using var initTimer = _performanceMonitoring.StartOperation("VectorServices.Initialize");

            try
            {
                _logger.LogInformation("Initializing vector database and knowledge base services...");

                // Step 1: Initialize vector database connection
                _logger.LogDebug("Initializing vector database connection");
                bool databaseInitialized;
                using (var dbInitTimer = _performanceMonitoring.StartOperation("VectorDatabase.Initialize"))
                {
                    databaseInitialized = await _vectorDatabaseService.InitializeAsync();
                }

                if (!databaseInitialized)
                {
                    _logger.LogWarning("Vector database initialization failed. Vector similarity features will be unavailable.");
                    return;
                }

                // Step 2: Check database connectivity
                _logger.LogDebug("Checking vector database connectivity");
                bool isConnected = await _vectorDatabaseService.IsConnectedAsync();
                if (!isConnected)
                {
                    _logger.LogWarning("Vector database is not connected. Vector similarity features will be unavailable.");
                    return;
                }

                // Step 3: Get current embedding count to check if database is already seeded
                _logger.LogDebug("Checking existing embeddings in database");
                long existingEmbeddingCount = await _vectorDatabaseService.GetEmbeddingCountAsync();
                _logger.LogInformation("Found {ExistingCount} existing embeddings in vector database", existingEmbeddingCount);

                // Step 4: Initialize knowledge base (this will check for duplicates internally)
                _logger.LogDebug("Initializing knowledge base with JSON examples");
                using (var kbInitTimer = _performanceMonitoring.StartOperation("KnowledgeBase.Initialize"))
                {
                    await _knowledgeBaseService.InitializeVectorDatabaseAsync();
                }

                // Step 5: Get final embedding count
                long finalEmbeddingCount = await _vectorDatabaseService.GetEmbeddingCountAsync();
                long newEmbeddingsAdded = finalEmbeddingCount - existingEmbeddingCount;

                _logger.LogInformation(
                    "Vector services initialization completed successfully. " +
                    "Total embeddings: {TotalCount}, New embeddings added: {NewCount}",
                    finalEmbeddingCount, newEmbeddingsAdded);

                // Record memory usage after vector initialization
                _performanceMonitoring.RecordMemoryUsage("After Vector Services Initialization");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, 
                    "Vector services initialization failed. The application will continue without vector similarity features. " +
                    "Error: {ErrorMessage}", ex.Message);

                // Log additional diagnostic information in verbose mode
                if (_appSettings.Application.VerboseMode)
                {
                    _logger.LogDebug("Vector services initialization failure details: {ExceptionDetails}", ex.ToString());
                }

                // Continue execution - vector services are optional
                // The application should work without similarity matching
            }
        }
    }
}