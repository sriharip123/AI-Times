using JSON_Whisperer.Interfaces;

namespace JSON_Whisperer.Services;

/// <summary>
/// Service for formatting and displaying help information for command-line options.
/// </summary>
public class HelpFormatter : IHelpFormatter
{
    /// <summary>
    /// Displays comprehensive help information including all command-line options,
    /// descriptions, and usage patterns.
    /// </summary>
    public void DisplayHelp()
    {
        Console.WriteLine();
        Console.WriteLine("JSON-Whisperer - AI-powered JSON analysis and summarization tool");
        Console.WriteLine("=================================================================");
        Console.WriteLine();
        Console.WriteLine("USAGE:");
        Console.WriteLine("  dotnet JSON-Whisperer.dll [options]");
        Console.WriteLine();
        
        DisplayInputOptions();
        DisplayDiagnosticOptions();
        DisplayTestingOptions();
        DisplayBenchmarkOptions();
        DisplayGeneralOptions();
        
        Console.WriteLine();
        Console.WriteLine("EXIT CODES:");
        Console.WriteLine("  0 - Success");
        Console.WriteLine("  1 - General error");
        Console.WriteLine("  2 - Configuration error");
        Console.WriteLine("  3 - Service unavailable");
        Console.WriteLine("  4 - Validation error");
        Console.WriteLine("  5 - Argument error");
        Console.WriteLine();
        Console.WriteLine("For usage examples, run: dotnet JSON-Whisperer.dll --help");
        Console.WriteLine();
    }

    /// <summary>
    /// Displays usage examples demonstrating common command-line scenarios.
    /// </summary>
    public void DisplayUsageExamples()
    {
        Console.WriteLine();
        Console.WriteLine("USAGE EXAMPLES:");
        Console.WriteLine("===============");
        Console.WriteLine();
        
        Console.WriteLine("Process JSON from file:");
        Console.WriteLine("  dotnet JSON-Whisperer.dll --file input.json");
        Console.WriteLine();
        
        Console.WriteLine("Process JSON from stdin:");
        Console.WriteLine("  cat input.json | dotnet JSON-Whisperer.dll");
        Console.WriteLine();
        
        Console.WriteLine("Process JSON with verbose output:");
        Console.WriteLine("  dotnet JSON-Whisperer.dll --file input.json --verbose");
        Console.WriteLine();
        
        Console.WriteLine("Process JSON without similarity matching:");
        Console.WriteLine("  dotnet JSON-Whisperer.dll --file input.json --no-similarity");
        Console.WriteLine();
        
        Console.WriteLine("Check health of all services:");
        Console.WriteLine("  dotnet JSON-Whisperer.dll --health-check");
        Console.WriteLine();
        
        Console.WriteLine("Validate configuration:");
        Console.WriteLine("  dotnet JSON-Whisperer.dll --validate-config");
        Console.WriteLine();
        
        Console.WriteLine("Test Ollama service:");
        Console.WriteLine("  dotnet JSON-Whisperer.dll --test-ollama");
        Console.WriteLine();
        
        Console.WriteLine("Test ScyllaDB service:");
        Console.WriteLine("  dotnet JSON-Whisperer.dll --test-scylla");
        Console.WriteLine();
        
        Console.WriteLine("Test embedding generation:");
        Console.WriteLine("  dotnet JSON-Whisperer.dll --test-embedding");
        Console.WriteLine();
        
        Console.WriteLine("Test similarity search:");
        Console.WriteLine("  dotnet JSON-Whisperer.dll --test-similarity");
        Console.WriteLine();
        
        Console.WriteLine("Reinitialize knowledge base:");
        Console.WriteLine("  dotnet JSON-Whisperer.dll --reinitialize-knowledge-base");
        Console.WriteLine();
        
        Console.WriteLine("Validate knowledge base:");
        Console.WriteLine("  dotnet JSON-Whisperer.dll --validate-knowledge-base");
        Console.WriteLine();
        
        Console.WriteLine("Run all benchmarks:");
        Console.WriteLine("  dotnet JSON-Whisperer.dll --benchmark-all");
        Console.WriteLine();
        
        Console.WriteLine("Run similarity benchmark:");
        Console.WriteLine("  dotnet JSON-Whisperer.dll --benchmark-similarity");
        Console.WriteLine();
    }

    private void DisplayInputOptions()
    {
        Console.WriteLine("INPUT OPTIONS:");
        Console.WriteLine("  --file <path>          Path to JSON file to process");
        Console.WriteLine("  (stdin)                Read JSON from standard input (default if no file specified)");
        Console.WriteLine();
    }

    private void DisplayDiagnosticOptions()
    {
        Console.WriteLine("DIAGNOSTIC OPTIONS:");
        Console.WriteLine("  --health-check         Check health of all services (Ollama, ScyllaDB, embeddings)");
        Console.WriteLine("  --validate-config      Validate all configuration settings");
        Console.WriteLine();
    }

    private void DisplayTestingOptions()
    {
        Console.WriteLine("TESTING OPTIONS:");
        Console.WriteLine("  --test-ollama          Test connectivity to Ollama service and verify model availability");
        Console.WriteLine("  --test-scylla          Test connectivity to ScyllaDB and verify keyspace");
        Console.WriteLine("  --test-embedding       Test embedding generation with nomic-embed-text model");
        Console.WriteLine("  --test-similarity      Test similarity search functionality");
        Console.WriteLine("  --reinitialize-knowledge-base");
        Console.WriteLine("                         Clear and rebuild all embeddings from JSON files");
        Console.WriteLine("  --validate-knowledge-base");
        Console.WriteLine("                         Validate JSON files and their descriptions");
        Console.WriteLine();
    }

    private void DisplayBenchmarkOptions()
    {
        Console.WriteLine("BENCHMARK OPTIONS:");
        Console.WriteLine("  --benchmark-all        Run all performance benchmarks");
        Console.WriteLine("  --benchmark-similarity Benchmark similarity search performance");
        Console.WriteLine("  --benchmark-vector-operations");
        Console.WriteLine("                         Benchmark embedding generation and storage");
        Console.WriteLine("  --benchmark-embedding  Benchmark embedding generation speed");
        Console.WriteLine();
    }

    private void DisplayGeneralOptions()
    {
        Console.WriteLine("GENERAL OPTIONS:");
        Console.WriteLine("  --verbose, -v          Enable verbose output (overrides configuration)");
        Console.WriteLine("  --no-similarity        Disable similarity matching for this execution");
        Console.WriteLine("  --help, -h             Display this help information");
        Console.WriteLine();
    }
}
