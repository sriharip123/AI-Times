using JSON_Whisperer.Models;

namespace JSON_Whisperer.Interfaces
{
    /// <summary>
    /// Interface for executing diagnostic commands
    /// </summary>
    public interface IDiagnosticCommandExecutor
    {
        /// <summary>
        /// Executes a diagnostic command and returns an exit code
        /// </summary>
        /// <param name="command">The diagnostic command to execute</param>
        /// <param name="options">The parsed command-line options</param>
        /// <returns>Exit code indicating success or failure</returns>
        Task<int> ExecuteAsync(DiagnosticCommand command, CommandLineOptions options);
    }
}
