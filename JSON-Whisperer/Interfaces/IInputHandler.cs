using System.Threading.Tasks;
using JSON_Whisperer.Models;

namespace JSON_Whisperer.Interfaces
{
    /// <summary>
    /// Interface for handling different types of JSON input sources
    /// </summary>
    public interface IInputHandler
    {
        /// <summary>
        /// Gets JSON input from command line arguments, file path, or stdin
        /// </summary>
        /// <param name="args">Command line arguments</param>
        /// <returns>JSON content as string</returns>
        [System.Obsolete("Use GetJsonInputAsync(CommandLineOptions) instead. This method is maintained for backward compatibility.")]
        Task<string> GetJsonInputAsync(string[] args);

        /// <summary>
        /// Gets JSON input from command line options, file path, or stdin
        /// </summary>
        /// <param name="options">Parsed command line options</param>
        /// <returns>JSON content as string</returns>
        Task<string> GetJsonInputAsync(CommandLineOptions options);

        /// <summary>
        /// Validates that the input contains valid JSON content
        /// </summary>
        /// <param name="jsonContent">JSON content to validate</param>
        /// <returns>True if valid JSON, false otherwise</returns>
        bool ValidateInput(string jsonContent);
    }
}