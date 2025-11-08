using System.Text.Json;
using JSON_Whisperer.Models;

namespace JSON_Whisperer.Interfaces
{
    /// <summary>
    /// Interface for analyzing JSON structure and extracting metadata
    /// </summary>
    public interface IJsonAnalyzer
    {
        /// <summary>
        /// Analyzes the structure of JSON content and returns analysis results
        /// </summary>
        /// <param name="jsonContent">JSON content to analyze</param>
        /// <returns>Analysis results containing structure metadata</returns>
        JsonAnalysisResult AnalyzeStructure(string jsonContent);

        /// <summary>
        /// Parses JSON content into a JsonDocument for further processing
        /// </summary>
        /// <param name="jsonContent">JSON content to parse</param>
        /// <returns>Parsed JsonDocument</returns>
        JsonDocument ParseJson(string jsonContent);
    }
}