using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Http;
using JSON_Whisperer.Interfaces;
using JSON_Whisperer.Services;
using JSON_Whisperer.Models;
using JSON_Whisperer;

Console.WriteLine("JSON-Whisperer starting...");
Console.Out.Flush();

// Build configuration
var configuration = new ConfigurationBuilder()
    .SetBasePath(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) ?? Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddEnvironmentVariables()
    .Build();

// Parse command-line arguments before building service provider
var commandLineParser = new CommandLineParser();
var options = commandLineParser.Parse(args);

// Validate command-line options
if (!commandLineParser.IsValid(options, out string errorMessage))
{
    Console.Error.WriteLine($"ERROR: Invalid command-line arguments");
    Console.Error.WriteLine(errorMessage);
    Console.Error.WriteLine();
    Console.Error.WriteLine("For help, run: dotnet JSON-Whisperer.dll --help");
    return ExitCodes.ArgumentError;
}

// Handle help mode
if (options.Mode == ExecutionMode.Help)
{
    var helpFormatter = new HelpFormatter();
    helpFormatter.DisplayHelp();
    return ExitCodes.Success;
}

// Setup dependency injection container
var services = new ServiceCollection();
ConfigureServices(services, configuration, options);

// Build service provider
using var serviceProvider = services.BuildServiceProvider();

try
{
    // Route to diagnostic executor for diagnostic commands
    if (options.Mode == ExecutionMode.Diagnostic && options.DiagnosticCommand.HasValue)
    {
        var diagnosticExecutor = serviceProvider.GetRequiredService<IDiagnosticCommandExecutor>();
        return await diagnosticExecutor.ExecuteAsync(options.DiagnosticCommand.Value, options);
    }

    // Route to normal execution for JSON processing
    var app = serviceProvider.GetRequiredService<JsonWhispererApplication>();
    return await app.RunAsync(args, options);
}
catch (Exception ex)
{
    var logger = serviceProvider.GetService<ILogger<Program>>();
    logger?.LogError(ex, "Application failed with unhandled exception");
    
    var outputFormatter = serviceProvider.GetService<IOutputFormatter>();
    outputFormatter?.DisplayError($"Application error: {ex.Message}");
    
    return ExitCodes.GeneralError;
}

static void ConfigureServices(IServiceCollection services, IConfiguration configuration, CommandLineOptions options)
        {
            // Configuration services
            services.AddSingleton<IConfiguration>(configuration);
            services.AddSingleton<ConfigurationService>();

            // Get validated settings for service configuration
            using var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
            var configService = new ConfigurationService(configuration, 
                loggerFactory.CreateLogger<ConfigurationService>());
            var appSettings = configService.GetAppSettings();

            // Apply command-line overrides to settings
            if (options.VerboseMode)
            {
                appSettings.Application.VerboseMode = true;
            }

            // Register settings as singleton
            services.AddSingleton(appSettings);

            // Register command-line options as singleton
            services.AddSingleton(options);

            // Logging configuration
            services.AddLogging(builder =>
            {
                builder.AddConfiguration(configuration.GetSection("Logging"));
                builder.AddConsole();
                
                // Override log level if verbose mode is enabled
                if (options.VerboseMode)
                {
                    builder.SetMinimumLevel(LogLevel.Debug);
                }
                
                // Add file logging if enabled
                var fileLoggingEnabled = configuration.GetValue("Logging:File:Enabled", false);
                if (fileLoggingEnabled)
                {
                    // File logging would require additional package like Serilog
                    // For now, we'll just use console logging
                    builder.SetMinimumLevel(LogLevel.Debug);
                }
            });

            // HttpClient for Ollama service
            services.AddHttpClient<IAiService, OllamaService>(client =>
            {
                client.BaseAddress = new Uri(appSettings.Ollama.BaseUrl);
                client.Timeout = TimeSpan.FromSeconds(appSettings.Ollama.TimeoutSeconds);
            });

            // HttpClient for Ollama embedding service
            services.AddHttpClient<IEmbeddingService, OllamaEmbeddingService>(client =>
            {
                client.BaseAddress = new Uri(appSettings.Ollama.BaseUrl);
                client.Timeout = TimeSpan.FromSeconds(appSettings.Ollama.TimeoutSeconds);
            });

            // Register application services
            services.AddScoped<IInputHandler, InputHandler>();
            services.AddScoped<IJsonAnalyzer, JsonAnalyzer>();
            services.AddScoped<IAiService, OllamaService>();
            services.AddScoped<IEmbeddingService, OllamaEmbeddingService>();
            services.AddScoped<IOutputFormatter, OutputFormatter>();
            
            // Register vector services (conditionally skip if --no-similarity flag is set)
            if (!options.NoSimilarity)
            {
                services.AddSingleton<IVectorDatabaseService, ScyllaDbVectorService>();
                services.AddScoped<ISimilarityService, SimilarityService>();
                services.AddScoped<IKnowledgeBaseService, KnowledgeBaseService>();
            }
            else
            {
                // Register null/stub implementations when similarity is disabled
                services.AddSingleton<IVectorDatabaseService>(sp => new NullVectorDatabaseService());
                services.AddScoped<ISimilarityService>(sp => new NullSimilarityService());
                services.AddScoped<IKnowledgeBaseService>(sp => new NullKnowledgeBaseService());
            }
            
            // Register monitoring and diagnostic services
            services.AddSingleton<PerformanceMonitoringService>();
            services.AddSingleton<DiagnosticService>();
            
            // Register command-line parsing services
            services.AddSingleton<ICommandLineParser, CommandLineParser>();
            services.AddSingleton<IHelpFormatter, HelpFormatter>();
            
            // Register diagnostic command services
            services.AddScoped<IDiagnosticCommandExecutor, DiagnosticCommandExecutor>();
            services.AddScoped<IHealthCheckService, HealthCheckService>();
            services.AddScoped<IConfigurationValidationService, ConfigurationValidationService>();
            services.AddScoped<IServiceTestingService, ServiceTestingService>();
            services.AddScoped<IKnowledgeBaseManagementService, KnowledgeBaseManagementService>();
            services.AddScoped<IBenchmarkService, BenchmarkService>();
            
            // Register main application orchestrator
            services.AddScoped<JsonWhispererApplication>();
        }

