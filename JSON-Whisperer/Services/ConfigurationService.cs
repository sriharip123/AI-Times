using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using JSON_Whisperer.Models;

namespace JSON_Whisperer.Services
{
    /// <summary>
    /// Service for managing application configuration with validation and environment variable support
    /// </summary>
    public class ConfigurationService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<ConfigurationService> _logger;
        private AppSettings? _appSettings;

        public ConfigurationService(IConfiguration configuration, ILogger<ConfigurationService> logger)
        {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Gets the validated application settings
        /// </summary>
        public AppSettings GetAppSettings()
        {
            if (_appSettings == null)
            {
                _appSettings = LoadAndValidateSettings();
            }
            return _appSettings;
        }

        /// <summary>
        /// Loads configuration from multiple sources and validates it
        /// </summary>
        private AppSettings LoadAndValidateSettings()
        {
            _logger.LogInformation("Loading application configuration...");

            var settings = new AppSettings();
            
            try
            {
                // Bind configuration sections
                _configuration.GetSection("Ollama").Bind(settings.Ollama);
                _configuration.GetSection("Application").Bind(settings.Application);
                _configuration.GetSection("Performance").Bind(settings.Performance);

                // Override with environment variables if present
                ApplyEnvironmentVariableOverrides(settings);

                // Validate the configuration
                settings.Validate();

                _logger.LogInformation("Configuration loaded and validated successfully");
                LogConfigurationSummary(settings);

                return settings;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Configuration validation failed");
                throw new InvalidOperationException($"Invalid configuration: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Applies environment variable overrides to configuration
        /// </summary>
        private void ApplyEnvironmentVariableOverrides(AppSettings settings)
        {
            // Ollama settings
            if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("OLLAMA_BASE_URL")))
            {
                settings.Ollama.BaseUrl = Environment.GetEnvironmentVariable("OLLAMA_BASE_URL")!;
                _logger.LogInformation("Ollama BaseUrl overridden by environment variable");
            }

            if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("OLLAMA_MODEL_NAME")))
            {
                settings.Ollama.ModelName = Environment.GetEnvironmentVariable("OLLAMA_MODEL_NAME")!;
                _logger.LogInformation("Ollama ModelName overridden by environment variable");
            }

            if (int.TryParse(Environment.GetEnvironmentVariable("OLLAMA_TIMEOUT_SECONDS"), out int timeout))
            {
                settings.Ollama.TimeoutSeconds = timeout;
                _logger.LogInformation("Ollama TimeoutSeconds overridden by environment variable");
            }

            if (int.TryParse(Environment.GetEnvironmentVariable("OLLAMA_RETRY_ATTEMPTS"), out int retryAttempts))
            {
                settings.Ollama.RetryAttempts = retryAttempts;
                _logger.LogInformation("Ollama RetryAttempts overridden by environment variable");
            }

            // Application settings
            if (bool.TryParse(Environment.GetEnvironmentVariable("APP_VERBOSE_MODE"), out bool verboseMode))
            {
                settings.Application.VerboseMode = verboseMode;
                _logger.LogInformation("Application VerboseMode overridden by environment variable");
            }

            if (long.TryParse(Environment.GetEnvironmentVariable("APP_MAX_JSON_SIZE_BYTES"), out long maxSize))
            {
                settings.Application.MaxJsonSizeBytes = maxSize;
                _logger.LogInformation("Application MaxJsonSizeBytes overridden by environment variable");
            }

            if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("APP_OUTPUT_FORMAT")))
            {
                settings.Application.OutputFormat = Environment.GetEnvironmentVariable("APP_OUTPUT_FORMAT")!;
                _logger.LogInformation("Application OutputFormat overridden by environment variable");
            }

            // Performance settings
            if (bool.TryParse(Environment.GetEnvironmentVariable("PERF_ENABLE_TIMING"), out bool enableTiming))
            {
                settings.Performance.EnableTiming = enableTiming;
                _logger.LogInformation("Performance EnableTiming overridden by environment variable");
            }

            if (bool.TryParse(Environment.GetEnvironmentVariable("PERF_ENABLE_MEMORY_TRACKING"), out bool enableMemory))
            {
                settings.Performance.EnableMemoryTracking = enableMemory;
                _logger.LogInformation("Performance EnableMemoryTracking overridden by environment variable");
            }
        }

        /// <summary>
        /// Logs a summary of the current configuration
        /// </summary>
        private void LogConfigurationSummary(AppSettings settings)
        {
            _logger.LogInformation("Configuration Summary:");
            _logger.LogInformation("  Ollama BaseUrl: {BaseUrl}", settings.Ollama.BaseUrl);
            _logger.LogInformation("  Ollama Model: {ModelName}", settings.Ollama.ModelName);
            _logger.LogInformation("  Ollama Timeout: {TimeoutSeconds}s", settings.Ollama.TimeoutSeconds);
            _logger.LogInformation("  Verbose Mode: {VerboseMode}", settings.Application.VerboseMode);
            _logger.LogInformation("  Max JSON Size: {MaxSize} bytes", settings.Application.MaxJsonSizeBytes);
            _logger.LogInformation("  Performance Timing: {EnableTiming}", settings.Performance.EnableTiming);
        }

        /// <summary>
        /// Gets configuration value with fallback
        /// </summary>
        public T GetValue<T>(string key, T defaultValue = default!)
        {
            return _configuration.GetValue(key, defaultValue);
        }

        /// <summary>
        /// Gets connection string
        /// </summary>
        public string GetConnectionString(string name)
        {
            return _configuration.GetConnectionString(name) ?? string.Empty;
        }
    }
}