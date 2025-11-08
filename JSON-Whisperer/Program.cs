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

// Setup dependency injection container
var services = new ServiceCollection();
ConfigureServices(services, configuration);

// Build service provider
using var serviceProvider = services.BuildServiceProvider();

try
{
    // Get the main application service and run
    var app = serviceProvider.GetRequiredService<JsonWhispererApplication>();
    return await app.RunAsync(args);
}
catch (Exception ex)
{
    var logger = serviceProvider.GetService<ILogger<Program>>();
    logger?.LogError(ex, "Application failed with unhandled exception");
    
    var outputFormatter = serviceProvider.GetService<IOutputFormatter>();
    outputFormatter?.DisplayError($"Application error: {ex.Message}");
    
    return 1;
}

static void ConfigureServices(IServiceCollection services, IConfiguration configuration)
        {
            // Configuration services
            services.AddSingleton<IConfiguration>(configuration);
            services.AddSingleton<ConfigurationService>();

            // Get validated settings for service configuration
            using var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
            var configService = new ConfigurationService(configuration, 
                loggerFactory.CreateLogger<ConfigurationService>());
            var appSettings = configService.GetAppSettings();

            // Register settings as singleton
            services.AddSingleton(appSettings);

            // Logging configuration
            services.AddLogging(builder =>
            {
                builder.AddConfiguration(configuration.GetSection("Logging"));
                builder.AddConsole();
                
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
            
            // Register vector services
            services.AddSingleton<IVectorDatabaseService, ScyllaDbVectorService>();
            services.AddScoped<ISimilarityService, SimilarityService>();
            services.AddScoped<IKnowledgeBaseService, KnowledgeBaseService>();
            
            // Register monitoring and diagnostic services
            services.AddSingleton<PerformanceMonitoringService>();
            services.AddSingleton<DiagnosticService>();
            
            // Register main application orchestrator
            services.AddScoped<JsonWhispererApplication>();
        }

