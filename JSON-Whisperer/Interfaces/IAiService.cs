using System.Threading.Tasks;
using JSON_Whisperer.Models;

namespace JSON_Whisperer.Interfaces
{
    /// <summary>
    /// Interface for AI service integration to generate summaries
    /// </summary>
    public interface IAiService
    {
        /// <summary>
        /// Checks if the AI service (Ollama) is available and the model is loaded
        /// </summary>
        /// <returns>True if service is available, false otherwise</returns>
        Task<bool> IsAvailableAsync();

        /// <summary>
        /// Generates a plain English summary of the JSON data using AI
        /// </summary>
        /// <param name="analysis">JSON analysis results</param>
        /// <param name="originalJson">Original JSON content</param>
        /// <param name="similarityResult">Optional similarity results for context enhancement</param>
        /// <returns>Generated summary in plain English</returns>
        Task<string> GenerateSummaryAsync(JsonAnalysisResult analysis, string originalJson, SimilarityResult? similarityResult = null);
    }
}