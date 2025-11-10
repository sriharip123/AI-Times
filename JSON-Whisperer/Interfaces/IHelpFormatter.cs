namespace JSON_Whisperer.Interfaces;

/// <summary>
/// Interface for formatting and displaying help information for command-line options.
/// </summary>
public interface IHelpFormatter
{
    /// <summary>
    /// Displays comprehensive help information including all command-line options,
    /// descriptions, and usage patterns.
    /// </summary>
    void DisplayHelp();

    /// <summary>
    /// Displays usage examples demonstrating common command-line scenarios.
    /// </summary>
    void DisplayUsageExamples();
}
