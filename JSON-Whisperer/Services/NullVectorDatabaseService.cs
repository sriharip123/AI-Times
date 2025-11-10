using JSON_Whisperer.Interfaces;
using JSON_Whisperer.Models;

namespace JSON_Whisperer.Services
{
    /// <summary>
    /// Null implementation of IVectorDatabaseService used when --no-similarity flag is set
    /// </summary>
    public class NullVectorDatabaseService : IVectorDatabaseService
    {
        public Task<bool> InitializeAsync()
        {
            return Task.FromResult(false);
        }

        public Task<bool> IsConnectedAsync()
        {
            return Task.FromResult(false);
        }

        public Task<bool> StoreEmbeddingAsync(string id, float[] embedding, string jsonContent, string description, Dictionary<string, string>? metadata = null)
        {
            return Task.FromResult(false);
        }

        public Task<List<SimilarityMatch>> FindSimilarAsync(float[] queryEmbedding, int maxResults = 5, float threshold = 0.7f)
        {
            return Task.FromResult(new List<SimilarityMatch>());
        }

        public Task<long> GetEmbeddingCountAsync()
        {
            return Task.FromResult(0L);
        }

        public Task<bool> EmbeddingExistsAsync(string id)
        {
            return Task.FromResult(false);
        }

        public Task<bool> DeleteEmbeddingAsync(string id)
        {
            return Task.FromResult(false);
        }

        public Task<int> DeleteAllEmbeddingsAsync()
        {
            return Task.FromResult(0);
        }

        public Task<List<string>> GetAllEmbeddingIdsAsync()
        {
            return Task.FromResult(new List<string>());
        }

        public Task DisposeAsync()
        {
            return Task.CompletedTask;
        }
    }
}
