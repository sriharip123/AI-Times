using JSON_Whisperer.Interfaces;
using JSON_Whisperer.Models;

namespace JSON_Whisperer.Services
{
    /// <summary>
    /// Null implementation of IKnowledgeBaseService used when --no-similarity flag is set
    /// </summary>
    public class NullKnowledgeBaseService : IKnowledgeBaseService
    {
        public Task InitializeVectorDatabaseAsync()
        {
            return Task.CompletedTask;
        }

        public Task<List<JsonExample>> LoadExamplesAsync()
        {
            return Task.FromResult(new List<JsonExample>());
        }
    }
}
