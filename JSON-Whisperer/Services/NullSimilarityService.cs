using JSON_Whisperer.Interfaces;
using JSON_Whisperer.Models;

namespace JSON_Whisperer.Services
{
    /// <summary>
    /// Null implementation of ISimilarityService used when --no-similarity flag is set
    /// </summary>
    public class NullSimilarityService : ISimilarityService
    {
        public Task<bool> IsAvailableAsync()
        {
            return Task.FromResult(false);
        }

        public Task<SimilarityResult> FindSimilarJsonAsync(string jsonInput)
        {
            return Task.FromResult(new SimilarityResult
            {
                Matches = new List<SimilarityMatch>(),
                HighestScore = 0.0f,
                TotalMatches = 0,
                ProcessingTime = TimeSpan.Zero,
                ThresholdUsed = 0.0f,
                MaxResultsRequested = 0
            });
        }

        public float CalculateCosineSimilarity(float[] vector1, float[] vector2)
        {
            return 0.0f;
        }

        public SimilarityConfiguration GetConfiguration()
        {
            return new SimilarityConfiguration
            {
                Threshold = 0.0f,
                MaxResults = 0,
                IsEnabled = false,
                EmbeddingModel = "none",
                KnowledgeBaseSize = 0
            };
        }
    }
}
