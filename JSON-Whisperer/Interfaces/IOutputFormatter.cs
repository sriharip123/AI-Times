using JSON_Whisperer.Models;

namespace JSON_Whisperer.Interfaces
{
    /// <summary>
    /// Interface for formatting and displaying output to the user
    /// </summary>
    public interface IOutputFormatter
    {
        /// <summary>
        /// Displays the results including original JSON, summary, and analysis metadata
        /// </summary>
        /// <param name="originalJson">Original JSON input</param>
        /// <param name="summary">Generated AI summary</param>
        /// <param name="analysis">JSON analysis results</param>
        /// <param name="similarityResult">Optional similarity matching results</param>
        void DisplayResults(string originalJson, string summary, JsonAnalysisResult analysis, SimilarityResult? similarityResult = null);

        /// <summary>
        /// Displays error messages in a user-friendly format
        /// </summary>
        /// <param name="errorMessage">Error message to display</param>
        void DisplayError(string errorMessage);
    }
}