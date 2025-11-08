using System.ComponentModel.DataAnnotations;

namespace JSON_Whisperer.Models
{
    /// <summary>
    /// Application configuration settings
    /// </summary>
    public class AppSettings
    {
        /// <summary>
        /// Ollama service configuration
        /// </summary>
        public OllamaSettings Ollama { get; set; } = new();

        /// <summary>
        /// Application behavior configuration
        /// </summary>
        public ApplicationSettings Application { get; set; } = new();

        /// <summary>
        /// Performance monitoring configuration
        /// </summary>
        public PerformanceSettings Performance { get; set; } = new();

        /// <summary>
        /// Vector similarity configuration
        /// </summary>
        public VectorSettings Vector { get; set; } = new();

        /// <summary>
        /// ScyllaDB database configuration
        /// </summary>
        public ScyllaDbSettings ScyllaDb { get; set; } = new();

        /// <summary>
        /// Validates the configuration settings
        /// </summary>
        public void Validate()
        {
            Ollama.Validate();
            Application.Validate();
            Performance.Validate();
            Vector.Validate();
            ScyllaDb.Validate();
        }
    }

    /// <summary>
    /// Ollama service configuration
    /// </summary>
    public class OllamaSettings
    {
        /// <summary>
        /// Base URL for the Ollama service (default: http://localhost:11434)
        /// </summary>
        [Required]
        [Url]
        public string BaseUrl { get; set; } = "http://localhost:11434";

        /// <summary>
        /// Name of the model to use (default: mistral)
        /// </summary>
        [Required]
        [MinLength(1)]
        public string ModelName { get; set; } = "mistral";

        /// <summary>
        /// Name of the embedding model to use (default: mistral)
        /// </summary>
        [Required]
        [MinLength(1)]
        public string EmbeddingModel { get; set; } = "mistral";

        /// <summary>
        /// Timeout for API requests in seconds (default: 30)
        /// </summary>
        [Range(1, 300)]
        public int TimeoutSeconds { get; set; } = 30;

        /// <summary>
        /// Number of retry attempts for failed requests (default: 3)
        /// </summary>
        [Range(0, 10)]
        public int RetryAttempts { get; set; } = 3;

        /// <summary>
        /// Delay between retry attempts in seconds (default: 2)
        /// </summary>
        [Range(1, 60)]
        public int RetryDelaySeconds { get; set; } = 2;

        public void Validate()
        {
            if (string.IsNullOrWhiteSpace(BaseUrl))
                throw new ArgumentException("Ollama BaseUrl cannot be empty");

            if (string.IsNullOrWhiteSpace(ModelName))
                throw new ArgumentException("Ollama ModelName cannot be empty");

            if (string.IsNullOrWhiteSpace(EmbeddingModel))
                throw new ArgumentException("Ollama EmbeddingModel cannot be empty");

            if (!Uri.TryCreate(BaseUrl, UriKind.Absolute, out _))
                throw new ArgumentException($"Invalid Ollama BaseUrl: {BaseUrl}");
        }
    }

    /// <summary>
    /// Application behavior configuration
    /// </summary>
    public class ApplicationSettings
    {
        /// <summary>
        /// Whether to enable verbose output mode (default: false)
        /// </summary>
        public bool VerboseMode { get; set; } = false;

        /// <summary>
        /// Maximum JSON file size in bytes (default: 10MB)
        /// </summary>
        [Range(1024, 104857600)] // 1KB to 100MB
        public long MaxJsonSizeBytes { get; set; } = 10485760; // 10MB

        /// <summary>
        /// Whether to enable performance metrics collection (default: true)
        /// </summary>
        public bool EnablePerformanceMetrics { get; set; } = true;

        /// <summary>
        /// Output format style (default: standard)
        /// </summary>
        public string OutputFormat { get; set; } = "standard";

        public void Validate()
        {
            if (MaxJsonSizeBytes <= 0)
                throw new ArgumentException("MaxJsonSizeBytes must be greater than 0");

            var validFormats = new[] { "standard", "compact", "detailed" };
            if (!validFormats.Contains(OutputFormat?.ToLowerInvariant()))
                throw new ArgumentException($"Invalid OutputFormat. Valid values: {string.Join(", ", validFormats)}");
        }
    }

    /// <summary>
    /// Performance monitoring configuration
    /// </summary>
    public class PerformanceSettings
    {
        /// <summary>
        /// Whether to enable operation timing (default: true)
        /// </summary>
        public bool EnableTiming { get; set; } = true;

        /// <summary>
        /// Whether to enable memory usage tracking (default: false)
        /// </summary>
        public bool EnableMemoryTracking { get; set; } = false;

        /// <summary>
        /// Threshold in milliseconds to warn about slow operations (default: 5000)
        /// </summary>
        [Range(100, 60000)]
        public int WarnOnSlowOperationsMs { get; set; } = 5000;

        public void Validate()
        {
            if (WarnOnSlowOperationsMs <= 0)
                throw new ArgumentException("WarnOnSlowOperationsMs must be greater than 0");
        }
    }

    /// <summary>
    /// Vector similarity configuration
    /// </summary>
    public class VectorSettings
    {
        /// <summary>
        /// Similarity threshold for matching (0.0 to 1.0, default: 0.7)
        /// </summary>
        [Range(0.0, 1.0)]
        public float SimilarityThreshold { get; set; } = 0.7f;

        /// <summary>
        /// Maximum number of similar results to retrieve (default: 5)
        /// </summary>
        [Range(1, 50)]
        public int MaxSimilarResults { get; set; } = 5;

        /// <summary>
        /// Whether to enable similarity matching (default: true)
        /// </summary>
        public bool EnableSimilarityMatching { get; set; } = true;

        /// <summary>
        /// Path to the AppData directory containing JSON examples (default: AppData)
        /// </summary>
        public string AppDataPath { get; set; } = "AppData";

        /// <summary>
        /// Whether to initialize the knowledge base on startup (default: true)
        /// </summary>
        public bool InitializeKnowledgeBase { get; set; } = true;

        public void Validate()
        {
            if (SimilarityThreshold < 0.0f || SimilarityThreshold > 1.0f)
                throw new ArgumentException("SimilarityThreshold must be between 0.0 and 1.0");

            if (MaxSimilarResults <= 0)
                throw new ArgumentException("MaxSimilarResults must be greater than 0");

            if (string.IsNullOrWhiteSpace(AppDataPath))
                throw new ArgumentException("AppDataPath cannot be empty");
        }
    }

    /// <summary>
    /// ScyllaDB database configuration
    /// </summary>
    public class ScyllaDbSettings
    {
        /// <summary>
        /// Comma-separated list of contact points (default: 127.0.0.1)
        /// </summary>
        [Required]
        public string ContactPoints { get; set; } = "127.0.0.1";

        /// <summary>
        /// Port number for ScyllaDB (default: 9042)
        /// </summary>
        [Range(1, 65535)]
        public int Port { get; set; } = 9042;

        /// <summary>
        /// Keyspace name for the application (default: json_whisperer)
        /// </summary>
        [Required]
        [MinLength(1)]
        public string Keyspace { get; set; } = "json_whisperer";

        /// <summary>
        /// Username for authentication (optional)
        /// </summary>
        public string Username { get; set; } = string.Empty;

        /// <summary>
        /// Password for authentication (optional)
        /// </summary>
        public string Password { get; set; } = string.Empty;

        /// <summary>
        /// Data center name (default: datacenter1)
        /// </summary>
        public string DataCenter { get; set; } = "datacenter1";

        /// <summary>
        /// Connection timeout in seconds (default: 10)
        /// </summary>
        [Range(1, 300)]
        public int ConnectionTimeoutSeconds { get; set; } = 10;

        /// <summary>
        /// Query timeout in seconds (default: 30)
        /// </summary>
        [Range(1, 300)]
        public int QueryTimeoutSeconds { get; set; } = 30;

        /// <summary>
        /// Whether to create keyspace if it doesn't exist (default: true)
        /// </summary>
        public bool CreateKeyspaceIfNotExists { get; set; } = true;

        public void Validate()
        {
            if (string.IsNullOrWhiteSpace(ContactPoints))
                throw new ArgumentException("ContactPoints cannot be empty");

            if (string.IsNullOrWhiteSpace(Keyspace))
                throw new ArgumentException("Keyspace cannot be empty");

            if (Port <= 0 || Port > 65535)
                throw new ArgumentException("Port must be between 1 and 65535");
        }
    }
}