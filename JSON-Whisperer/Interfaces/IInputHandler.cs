using System.Threading.Tasks;

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
        Task<string> GetJsonInputAsync(string[] args);

        /// <summary>
        /// Validates that the input contains valid JSON content
        /// </summary>
        /// <param name="jsonContent">JSON content to validate</param>
        /// <returns>True if valid JSON, false otherwise</returns>
        bool ValidateInput(string jsonContent);
    }
}