using JSON_Whisperer.Models;

namespace JSON_Whisperer.Interfaces
{
    /// <summary>
    /// Interface for parsing and validating command-line arguments
    /// </summary>
    public interface ICommandLineParser
    {
        /// <summary>
        /// Parses command-line arguments into structured options
        /// </summary>
        /// <param name="args">Command-line arguments to parse</param>
        /// <returns>Parsed command-line options</returns>
        CommandLineOptions Parse(string[] args);

        /// <summary>
        /// Validates the parsed command-line options
        /// </summary>
        /// <param name="options">Options to validate</param>
        /// <param name="errorMessage">Error message if validation fails</param>
        /// <returns>True if options are valid, false otherwise</returns>
        bool IsValid(CommandLineOptions options, out string errorMessage);
    }
}
