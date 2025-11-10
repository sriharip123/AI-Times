using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using JSON_Whisperer.Interfaces;
using JSON_Whisperer.Models;
using System.Text.RegularExpressions;

namespace JSON_Whisperer.Services
{
    /// <summary>
    /// Service for validating application configuration settings
    /// </summary>
    public class ConfigurationValidationService : IConfigurationValidationService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<ConfigurationValidationService> _logger;

        public ConfigurationValidationService(
            IConfiguration configuration,
            ILogger<ConfigurationValidationService> logger)
        {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Validates all configuration sections and returns a comprehensive result
        /// </summary>
        public async Task<ConfigurationValidationResult> ValidateAsync()
        {
            _logger.LogInformation("Starting configuration validation...");

            var result = new ConfigurationValidationResult();

            try
            {
                // Load settings from configuration
                var ollamaSettings = new OllamaSettings();
                var scyllaDbSettings = new ScyllaDbSettings();
                var vectorSettings = new VectorSettings();
                var applicationSettings = new ApplicationSettings();

                _configuration.GetSection("Ollama").Bind(ollamaSettings);
                _configuration.GetSection("ScyllaDb").Bind(scyllaDbSettings);
                _configuration.GetSection("Vector").Bind(vectorSettings);
                _configuration.GetSection("Application").Bind(applicationSettings);

                // Validate each section
                result.Results.Add(ValidateOllamaConfig(ollamaSettings));
                result.Results.Add(ValidateScyllaDbConfig(scyllaDbSettings));
                result.Results.Add(ValidateVectorConfig(vectorSettings));
                result.Results.Add(ValidateApplicationConfig(applicationSettings));

                _logger.LogInformation(
                    "Configuration validation completed. Valid: {ValidSections}/{TotalSections}",
                    result.ValidSections,
                    result.TotalSections);

                return await Task.FromResult(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Configuration validation failed with exception");
                
                var errorResult = new ValidationResult
                {
                    Section = "General",
                    IsValid = false
                };
                errorResult.Errors.Add($"Configuration validation failed: {ex.Message}");
                result.Results.Add(errorResult);

                return result;
            }
        }

        /// <summary>
        /// Validates the Ollama service configuration section
        /// </summary>
        public ValidationResult ValidateOllamaConfig(OllamaSettings settings)
        {
            var result = new ValidationResult
            {
                Section = "Ollama",
                IsValid = true
            };

            // Validate BaseUrl
            if (string.IsNullOrWhiteSpace(settings.BaseUrl))
            {
                result.IsValid = false;
                result.Errors.Add("Ollama BaseUrl is required and cannot be empty");
            }
            else if (!IsValidUrl(settings.BaseUrl))
            {
                result.IsValid = false;
                result.Errors.Add($"Ollama BaseUrl is not a valid URL: {settings.BaseUrl}");
            }
            else if (!settings.BaseUrl.StartsWith("http://") && !settings.BaseUrl.StartsWith("https://"))
            {
                result.IsValid = false;
                result.Errors.Add($"Ollama BaseUrl must start with http:// or https://: {settings.BaseUrl}");
            }

            // Validate ModelName
            if (string.IsNullOrWhiteSpace(settings.ModelName))
            {
                result.IsValid = false;
                result.Errors.Add("Ollama ModelName is required and cannot be empty");
            }

            // Validate EmbeddingModel
            if (string.IsNullOrWhiteSpace(settings.EmbeddingModel))
            {
                result.IsValid = false;
                result.Errors.Add("Ollama EmbeddingModel is required and cannot be empty");
            }

            // Validate TimeoutSeconds
            if (settings.TimeoutSeconds < 1 || settings.TimeoutSeconds > 300)
            {
                result.IsValid = false;
                result.Errors.Add($"Ollama TimeoutSeconds must be between 1 and 300, got: {settings.TimeoutSeconds}");
            }
            else if (settings.TimeoutSeconds < 10)
            {
                result.Warnings.Add($"Ollama TimeoutSeconds is very low ({settings.TimeoutSeconds}s). Consider increasing for better reliability.");
            }

            // Validate RetryAttempts
            if (settings.RetryAttempts < 0 || settings.RetryAttempts > 10)
            {
                result.IsValid = false;
                result.Errors.Add($"Ollama RetryAttempts must be between 0 and 10, got: {settings.RetryAttempts}");
            }

            // Validate RetryDelaySeconds
            if (settings.RetryDelaySeconds < 1 || settings.RetryDelaySeconds > 60)
            {
                result.IsValid = false;
                result.Errors.Add($"Ollama RetryDelaySeconds must be between 1 and 60, got: {settings.RetryDelaySeconds}");
            }

            return result;
        }

        /// <summary>
        /// Validates the ScyllaDB database configuration section
        /// </summary>
        public ValidationResult ValidateScyllaDbConfig(ScyllaDbSettings settings)
        {
            var result = new ValidationResult
            {
                Section = "ScyllaDB",
                IsValid = true
            };

            // Validate ContactPoints
            if (string.IsNullOrWhiteSpace(settings.ContactPoints))
            {
                result.IsValid = false;
                result.Errors.Add("ScyllaDB ContactPoints is required and cannot be empty");
            }
            else
            {
                // Validate each contact point
                var contactPoints = settings.ContactPoints.Split(',', StringSplitOptions.RemoveEmptyEntries);
                if (contactPoints.Length == 0)
                {
                    result.IsValid = false;
                    result.Errors.Add("ScyllaDB ContactPoints must contain at least one valid contact point");
                }
                else
                {
                    foreach (var contactPoint in contactPoints)
                    {
                        var trimmed = contactPoint.Trim();
                        if (!IsValidHostname(trimmed) && !IsValidIpAddress(trimmed))
                        {
                            result.IsValid = false;
                            result.Errors.Add($"Invalid ScyllaDB contact point: {trimmed}");
                        }
                    }
                }
            }

            // Validate Port
            if (settings.Port < 1 || settings.Port > 65535)
            {
                result.IsValid = false;
                result.Errors.Add($"ScyllaDB Port must be between 1 and 65535, got: {settings.Port}");
            }

            // Validate Keyspace
            if (string.IsNullOrWhiteSpace(settings.Keyspace))
            {
                result.IsValid = false;
                result.Errors.Add("ScyllaDB Keyspace is required and cannot be empty");
            }
            else if (!IsValidKeyspaceName(settings.Keyspace))
            {
                result.IsValid = false;
                result.Errors.Add($"ScyllaDB Keyspace name is invalid: {settings.Keyspace}. Must contain only alphanumeric characters and underscores.");
            }

            // Validate DataCenter
            if (string.IsNullOrWhiteSpace(settings.DataCenter))
            {
                result.Warnings.Add("ScyllaDB DataCenter is not specified. Using default 'datacenter1'.");
            }

            // Validate ConnectionTimeoutSeconds
            if (settings.ConnectionTimeoutSeconds < 1 || settings.ConnectionTimeoutSeconds > 300)
            {
                result.IsValid = false;
                result.Errors.Add($"ScyllaDB ConnectionTimeoutSeconds must be between 1 and 300, got: {settings.ConnectionTimeoutSeconds}");
            }

            // Validate QueryTimeoutSeconds
            if (settings.QueryTimeoutSeconds < 1 || settings.QueryTimeoutSeconds > 300)
            {
                result.IsValid = false;
                result.Errors.Add($"ScyllaDB QueryTimeoutSeconds must be between 1 and 300, got: {settings.QueryTimeoutSeconds}");
            }

            // Validate authentication
            if (!string.IsNullOrWhiteSpace(settings.Username) && string.IsNullOrWhiteSpace(settings.Password))
            {
                result.Warnings.Add("ScyllaDB Username is specified but Password is empty. Authentication may fail.");
            }

            return result;
        }

        /// <summary>
        /// Validates the Vector similarity configuration section
        /// </summary>
        public ValidationResult ValidateVectorConfig(VectorSettings settings)
        {
            var result = new ValidationResult
            {
                Section = "Vector",
                IsValid = true
            };

            // Validate SimilarityThreshold
            if (settings.SimilarityThreshold < 0.0f || settings.SimilarityThreshold > 1.0f)
            {
                result.IsValid = false;
                result.Errors.Add($"Vector SimilarityThreshold must be between 0.0 and 1.0, got: {settings.SimilarityThreshold}");
            }
            else if (settings.SimilarityThreshold < 0.3f)
            {
                result.Warnings.Add($"Vector SimilarityThreshold is very low ({settings.SimilarityThreshold}). This may return too many irrelevant results.");
            }
            else if (settings.SimilarityThreshold > 0.95f)
            {
                result.Warnings.Add($"Vector SimilarityThreshold is very high ({settings.SimilarityThreshold}). This may return too few results.");
            }

            // Validate MaxSimilarResults
            if (settings.MaxSimilarResults < 1 || settings.MaxSimilarResults > 50)
            {
                result.IsValid = false;
                result.Errors.Add($"Vector MaxSimilarResults must be between 1 and 50, got: {settings.MaxSimilarResults}");
            }

            // Validate AppDataPath
            if (string.IsNullOrWhiteSpace(settings.AppDataPath))
            {
                result.IsValid = false;
                result.Errors.Add("Vector AppDataPath is required and cannot be empty");
            }
            else
            {
                // Check if path exists (warning only, not an error)
                var fullPath = Path.IsPathRooted(settings.AppDataPath)
                    ? settings.AppDataPath
                    : Path.Combine(Directory.GetCurrentDirectory(), settings.AppDataPath);

                if (!Directory.Exists(fullPath))
                {
                    result.Warnings.Add($"Vector AppDataPath does not exist: {fullPath}. It will be created if needed.");
                }
            }

            // Validate EnableSimilarityMatching consistency
            if (!settings.EnableSimilarityMatching && settings.InitializeKnowledgeBase)
            {
                result.Warnings.Add("Vector similarity matching is disabled but InitializeKnowledgeBase is enabled. Knowledge base initialization will be skipped.");
            }

            return result;
        }

        /// <summary>
        /// Validates the Application behavior configuration section
        /// </summary>
        public ValidationResult ValidateApplicationConfig(ApplicationSettings settings)
        {
            var result = new ValidationResult
            {
                Section = "Application",
                IsValid = true
            };

            // Validate MaxJsonSizeBytes
            if (settings.MaxJsonSizeBytes < 1024 || settings.MaxJsonSizeBytes > 104857600)
            {
                result.IsValid = false;
                result.Errors.Add($"Application MaxJsonSizeBytes must be between 1024 (1KB) and 104857600 (100MB), got: {settings.MaxJsonSizeBytes}");
            }
            else if (settings.MaxJsonSizeBytes < 10240)
            {
                result.Warnings.Add($"Application MaxJsonSizeBytes is very small ({settings.MaxJsonSizeBytes} bytes). This may reject valid JSON files.");
            }

            // Validate OutputFormat
            var validFormats = new[] { "standard", "compact", "detailed" };
            if (string.IsNullOrWhiteSpace(settings.OutputFormat))
            {
                result.IsValid = false;
                result.Errors.Add("Application OutputFormat is required and cannot be empty");
            }
            else if (!validFormats.Contains(settings.OutputFormat.ToLowerInvariant()))
            {
                result.IsValid = false;
                result.Errors.Add($"Application OutputFormat must be one of: {string.Join(", ", validFormats)}. Got: {settings.OutputFormat}");
            }

            return result;
        }

        /// <summary>
        /// Validates if a string is a valid URL
        /// </summary>
        private bool IsValidUrl(string url)
        {
            return Uri.TryCreate(url, UriKind.Absolute, out var uriResult)
                && (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps);
        }

        /// <summary>
        /// Validates if a string is a valid hostname
        /// </summary>
        private bool IsValidHostname(string hostname)
        {
            if (string.IsNullOrWhiteSpace(hostname))
                return false;

            // Hostname regex pattern
            var hostnamePattern = @"^([a-zA-Z0-9]|[a-zA-Z0-9][a-zA-Z0-9\-]*[a-zA-Z0-9])(\.([a-zA-Z0-9]|[a-zA-Z0-9][a-zA-Z0-9\-]*[a-zA-Z0-9]))*$";
            return Regex.IsMatch(hostname, hostnamePattern);
        }

        /// <summary>
        /// Validates if a string is a valid IP address
        /// </summary>
        private bool IsValidIpAddress(string ipAddress)
        {
            return System.Net.IPAddress.TryParse(ipAddress, out _);
        }

        /// <summary>
        /// Validates if a string is a valid keyspace name
        /// </summary>
        private bool IsValidKeyspaceName(string keyspaceName)
        {
            if (string.IsNullOrWhiteSpace(keyspaceName))
                return false;

            // Keyspace names must contain only alphanumeric characters and underscores
            var keyspacePattern = @"^[a-zA-Z][a-zA-Z0-9_]*$";
            return Regex.IsMatch(keyspaceName, keyspacePattern);
        }
    }
}
