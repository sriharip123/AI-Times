using Cassandra;
using Microsoft.Extensions.Logging;
using JSON_Whisperer.Interfaces;
using JSON_Whisperer.Models;
using System.Text.Json;

namespace JSON_Whisperer.Services
{
    /// <summary>
    /// ScyllaDB implementation of vector database service for high-performance vector storage and retrieval
    /// </summary>
    public class ScyllaDbVectorService : IVectorDatabaseService
    {
        private readonly ILogger<ScyllaDbVectorService> _logger;
        private readonly AppSettings _appSettings;
        private ISession? _session;
        private ICluster? _cluster;
        private bool _isInitialized = false;

        // Prepared statements for better performance
        private PreparedStatement? _insertStatement;
        private PreparedStatement? _selectAllStatement;
        private PreparedStatement? _existsStatement;
        private PreparedStatement? _deleteStatement;
        private PreparedStatement? _countStatement;

        public ScyllaDbVectorService(
            ILogger<ScyllaDbVectorService> logger,
            AppSettings appSettings)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _appSettings = appSettings ?? throw new ArgumentNullException(nameof(appSettings));
        }

        /// <summary>
        /// Initializes the database connection and creates necessary tables
        /// </summary>
        public async Task<bool> InitializeAsync()
        {
            if (_isInitialized)
            {
                return true;
            }

            try
            {
                _logger.LogInformation("Initializing ScyllaDB connection to {ContactPoints}:{Port}", 
                    _appSettings.ScyllaDb.ContactPoints, _appSettings.ScyllaDb.Port);

                // Build cluster configuration
                var builder = Cluster.Builder()
                    .AddContactPoints(_appSettings.ScyllaDb.ContactPoints.Split(',', StringSplitOptions.RemoveEmptyEntries))
                    .WithPort(_appSettings.ScyllaDb.Port)
                    .WithSocketOptions(new SocketOptions()
                        .SetConnectTimeoutMillis(_appSettings.ScyllaDb.ConnectionTimeoutSeconds * 1000)
                        .SetReadTimeoutMillis(_appSettings.ScyllaDb.QueryTimeoutSeconds * 1000));

                // Add authentication if provided
                if (!string.IsNullOrWhiteSpace(_appSettings.ScyllaDb.Username))
                {
                    builder = builder.WithCredentials(_appSettings.ScyllaDb.Username, _appSettings.ScyllaDb.Password);
                }

                // Build cluster and connect
                _cluster = builder.Build();
                _session = await _cluster.ConnectAsync();

                _logger.LogDebug("Connected to ScyllaDB cluster");

                // Create keyspace if it doesn't exist
                if (_appSettings.ScyllaDb.CreateKeyspaceIfNotExists)
                {
                    await CreateKeyspaceAsync();
                }

                // Use the keyspace
                await _session.ExecuteAsync(new SimpleStatement($"USE {_appSettings.ScyllaDb.Keyspace}"));

                // Create tables
                await CreateTablesAsync();

                // Prepare statements for better performance
                await PrepareStatementsAsync();

                _isInitialized = true;
                _logger.LogInformation("ScyllaDB vector service initialized successfully");

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize ScyllaDB connection");
                await DisposeAsync();
                return false;
            }
        }

        /// <summary>
        /// Stores a vector embedding in the database
        /// </summary>
        public async Task<bool> StoreEmbeddingAsync(string id, float[] embedding, string jsonContent, string description, Dictionary<string, string>? metadata = null)
        {
            if (!_isInitialized || _session == null || _insertStatement == null)
            {
                _logger.LogError("Service not initialized");
                return false;
            }

            try
            {
                var metadataJson = metadata != null ? JsonSerializer.Serialize(metadata) : null;
                var embeddingList = embedding.ToList();

                var boundStatement = _insertStatement.Bind(
                    id,
                    embeddingList,
                    jsonContent,
                    description,
                    metadataJson,
                    DateTimeOffset.UtcNow
                );

                await _session.ExecuteAsync(boundStatement);

                _logger.LogDebug("Stored embedding {Id} with {Dimensions} dimensions", id, embedding.Length);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to store embedding {Id}", id);
                return false;
            }
        }

        /// <summary>
        /// Finds similar embeddings using cosine similarity
        /// </summary>
        public async Task<List<SimilarityMatch>> FindSimilarAsync(float[] queryEmbedding, int maxResults = 5, float threshold = 0.7f)
        {
            if (!_isInitialized || _session == null || _selectAllStatement == null)
            {
                _logger.LogError("Service not initialized");
                return new List<SimilarityMatch>();
            }

            try
            {
                _logger.LogDebug("Finding similar embeddings with threshold {Threshold}, max results {MaxResults}", threshold, maxResults);

                // Get all embeddings (ScyllaDB doesn't have built-in vector similarity, so we need to compute it)
                var result = await _session.ExecuteAsync(_selectAllStatement.Bind());
                var matches = new List<SimilarityMatch>();

                foreach (var row in result)
                {
                    var storedEmbedding = row.GetValue<IList<float>>("embedding").ToArray();
                    var similarity = CalculateCosineSimilarity(queryEmbedding, storedEmbedding);

                    if (similarity >= threshold)
                    {
                        var metadataJson = row.GetValue<string>("metadata");
                        Dictionary<string, string>? metadata = null;
                        
                        if (!string.IsNullOrEmpty(metadataJson))
                        {
                            try
                            {
                                metadata = JsonSerializer.Deserialize<Dictionary<string, string>>(metadataJson);
                            }
                            catch (JsonException ex)
                            {
                                _logger.LogWarning(ex, "Failed to deserialize metadata for embedding {Id}", row.GetValue<string>("id"));
                            }
                        }

                        matches.Add(new SimilarityMatch
                        {
                            Id = row.GetValue<string>("id"),
                            JsonContent = row.GetValue<string>("json_content"),
                            Description = row.GetValue<string>("description"),
                            SimilarityScore = similarity,
                            Metadata = metadata
                        });
                    }
                }

                // Sort by similarity score (descending) and take top results
                var topMatches = matches
                    .OrderByDescending(m => m.SimilarityScore)
                    .Take(maxResults)
                    .ToList();

                _logger.LogDebug("Found {Count} similar embeddings above threshold {Threshold}", topMatches.Count, threshold);
                return topMatches;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to find similar embeddings");
                return new List<SimilarityMatch>();
            }
        }

        /// <summary>
        /// Checks if the database connection is active
        /// </summary>
        public async Task<bool> IsConnectedAsync()
        {
            if (_session == null || _cluster == null)
            {
                return false;
            }

            try
            {
                // Simple query to test connection
                await _session.ExecuteAsync(new SimpleStatement("SELECT now() FROM system.local"));
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Database connection check failed");
                return false;
            }
        }

        /// <summary>
        /// Gets the total number of embeddings stored in the database
        /// </summary>
        public async Task<long> GetEmbeddingCountAsync()
        {
            if (!_isInitialized || _session == null || _countStatement == null)
            {
                return 0;
            }

            try
            {
                var result = await _session.ExecuteAsync(_countStatement.Bind());
                var row = result.FirstOrDefault();
                return row?.GetValue<long>("count") ?? 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get embedding count");
                return 0;
            }
        }

        /// <summary>
        /// Checks if an embedding with the given ID already exists
        /// </summary>
        public async Task<bool> EmbeddingExistsAsync(string id)
        {
            if (!_isInitialized || _session == null || _existsStatement == null)
            {
                return false;
            }

            try
            {
                var result = await _session.ExecuteAsync(_existsStatement.Bind(id));
                return result.Any();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to check if embedding exists: {Id}", id);
                return false;
            }
        }

        /// <summary>
        /// Deletes an embedding by ID
        /// </summary>
        public async Task<bool> DeleteEmbeddingAsync(string id)
        {
            if (!_isInitialized || _session == null || _deleteStatement == null)
            {
                return false;
            }

            try
            {
                await _session.ExecuteAsync(_deleteStatement.Bind(id));
                _logger.LogDebug("Deleted embedding {Id}", id);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete embedding {Id}", id);
                return false;
            }
        }

        /// <summary>
        /// Deletes all embeddings from the database
        /// </summary>
        public async Task<int> DeleteAllEmbeddingsAsync()
        {
            if (!_isInitialized || _session == null)
            {
                return 0;
            }

            try
            {
                // Get count before deletion
                var countBefore = await GetEmbeddingCountAsync();

                // Truncate the table for efficient deletion
                await _session.ExecuteAsync(new SimpleStatement("TRUNCATE embeddings"));

                _logger.LogInformation("Deleted all {Count} embeddings from database", countBefore);
                return (int)countBefore;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete all embeddings");
                return 0;
            }
        }

        /// <summary>
        /// Gets all embedding IDs from the database
        /// </summary>
        public async Task<List<string>> GetAllEmbeddingIdsAsync()
        {
            if (!_isInitialized || _session == null)
            {
                return new List<string>();
            }

            try
            {
                var result = await _session.ExecuteAsync(new SimpleStatement("SELECT id FROM embeddings"));
                var ids = new List<string>();

                foreach (var row in result)
                {
                    ids.Add(row.GetValue<string>("id"));
                }

                _logger.LogDebug("Retrieved {Count} embedding IDs", ids.Count);
                return ids;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get all embedding IDs");
                return new List<string>();
            }
        }

        /// <summary>
        /// Closes the database connection and cleans up resources
        /// </summary>
        public async Task DisposeAsync()
        {
            try
            {
                if (_session != null)
                {
                    await _session.ShutdownAsync();
                    _session = null;
                }

                if (_cluster != null)
                {
                    await _cluster.ShutdownAsync();
                    _cluster = null;
                }

                _isInitialized = false;
                _logger.LogDebug("ScyllaDB connection disposed");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error disposing ScyllaDB connection");
            }
        }

        /// <summary>
        /// Calculates cosine similarity between two vectors
        /// </summary>
        private static float CalculateCosineSimilarity(float[] vector1, float[] vector2)
        {
            if (vector1.Length != vector2.Length)
            {
                throw new ArgumentException("Vectors must have the same length");
            }

            double dotProduct = 0.0;
            double magnitude1 = 0.0;
            double magnitude2 = 0.0;

            for (int i = 0; i < vector1.Length; i++)
            {
                dotProduct += vector1[i] * vector2[i];
                magnitude1 += vector1[i] * vector1[i];
                magnitude2 += vector2[i] * vector2[i];
            }

            magnitude1 = Math.Sqrt(magnitude1);
            magnitude2 = Math.Sqrt(magnitude2);

            if (magnitude1 == 0.0 || magnitude2 == 0.0)
            {
                return 0.0f;
            }

            return (float)(dotProduct / (magnitude1 * magnitude2));
        }

        /// <summary>
        /// Creates the keyspace if it doesn't exist
        /// </summary>
        private async Task CreateKeyspaceAsync()
        {
            var createKeyspaceCql = $@"
                CREATE KEYSPACE IF NOT EXISTS {_appSettings.ScyllaDb.Keyspace}
                WITH REPLICATION = {{
                    'class': 'SimpleStrategy',
                    'replication_factor': 1
                }}";

            await _session!.ExecuteAsync(new SimpleStatement(createKeyspaceCql));
            _logger.LogDebug("Keyspace {Keyspace} created or already exists", _appSettings.ScyllaDb.Keyspace);
        }

        /// <summary>
        /// Creates the necessary tables for vector storage
        /// </summary>
        private async Task CreateTablesAsync()
        {
            // Create embeddings table optimized for ScyllaDB
            var createTableCql = @"
                CREATE TABLE IF NOT EXISTS embeddings (
                    id text PRIMARY KEY,
                    embedding list<float>,
                    json_content text,
                    description text,
                    metadata text,
                    created_at timestamp
                ) WITH 
                    compaction = {'class': 'SizeTieredCompactionStrategy'} AND
                    compression = {'sstable_compression': 'LZ4Compressor'}";

            await _session!.ExecuteAsync(new SimpleStatement(createTableCql));
            _logger.LogDebug("Embeddings table created or already exists");
        }

        /// <summary>
        /// Prepares statements for better performance
        /// </summary>
        private async Task PrepareStatementsAsync()
        {
            _insertStatement = await _session!.PrepareAsync(@"
                INSERT INTO embeddings (id, embedding, json_content, description, metadata, created_at)
                VALUES (?, ?, ?, ?, ?, ?)");

            _selectAllStatement = await _session.PrepareAsync(@"
                SELECT id, embedding, json_content, description, metadata
                FROM embeddings");

            _existsStatement = await _session.PrepareAsync(@"
                SELECT id FROM embeddings WHERE id = ?");

            _deleteStatement = await _session.PrepareAsync(@"
                DELETE FROM embeddings WHERE id = ?");

            _countStatement = await _session.PrepareAsync(@"
                SELECT COUNT(*) as count FROM embeddings");

            _logger.LogDebug("Prepared statements created");
        }
    }
}