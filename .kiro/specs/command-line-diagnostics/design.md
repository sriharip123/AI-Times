# Design Document

## Overview

This design document outlines the implementation approach for adding comprehensive command-line diagnostic and testing features to JSON-Whisperer. The solution will extend the existing command-line argument parsing system and add new diagnostic services that can be invoked independently of the main JSON processing workflow.

## Architecture

### High-Level Architecture

```
┌─────────────────────────────────────────────────────────────┐
│                     Program.cs (Entry Point)                 │
└────────────────────┬────────────────────────────────────────┘
                     │
                     ▼
┌─────────────────────────────────────────────────────────────┐
│              CommandLineParser (New)                         │
│  - Parses all arguments                                      │
│  - Determines execution mode (Normal/Diagnostic)             │
│  - Validates argument combinations                           │
└────────────────────┬────────────────────────────────────────┘
                     │
         ┌───────────┴───────────┐
         │                       │
         ▼                       ▼
┌─────────────────┐    ┌──────────────────────────┐
│ Normal Mode     │    │  Diagnostic Mode         │
│ (Existing)      │    │  (New)                   │
└─────────────────┘    └────────┬─────────────────┘
                                │
                                ▼
                    ┌───────────────────────────┐
                    │ DiagnosticCommandExecutor │
                    │  (New)                    │
                    └────────┬──────────────────┘
                             │
         ┌───────────────────┼───────────────────┐
         │                   │                   │
         ▼                   ▼                   ▼
┌─────────────────┐  ┌──────────────┐  ┌─────────────────┐
│ HealthCheckCmd  │  │ TestCommands │  │ BenchmarkCmd    │
│ (New)           │  │ (New)        │  │ (New)           │
└─────────────────┘  └──────────────┘  └─────────────────┘
```

### Component Interaction

```
User Input → CommandLineParser → DiagnosticCommandExecutor → Diagnostic Services
                                                            ↓
                                                    Exit with Status Code
```

## Components and Interfaces

### 1. CommandLineParser (New)

**Purpose**: Parse and validate all command-line arguments, determine execution mode.

**Interface**:
```csharp
public interface ICommandLineParser
{
    CommandLineOptions Parse(string[] args);
    bool IsValid(out string errorMessage);
}

public class CommandLineOptions
{
    // Execution Mode
    public ExecutionMode Mode { get; set; }
    
    // Input Options
    public string? FilePath { get; set; }
    public string? JsonContent { get; set; }
    
    // Flags
    public bool VerboseMode { get; set; }
    public bool NoSimilarity { get; set; }
    public bool HelpRequested { get; set; }
    
    // Diagnostic Commands
    public DiagnosticCommand? DiagnosticCommand { get; set; }
}

public enum ExecutionMode
{
    Normal,      // Process JSON
    Diagnostic,  // Run diagnostic command
    Help         // Show help
}

public enum DiagnosticCommand
{
    HealthCheck,
    ValidateConfig,
    TestOllama,
    TestScylla,
    TestEmbedding,
    TestSimilarity,
    ReinitializeKnowledgeBase,
    ValidateKnowledgeBase,
    BenchmarkAll,
    BenchmarkSimilarity,
    BenchmarkVectorOperations,
    BenchmarkEmbedding
}
```

**Responsibilities**:
- Parse command-line arguments into structured options
- Validate argument combinations
- Detect conflicting flags
- Determine execution mode based on arguments

### 2. DiagnosticCommandExecutor (New)

**Purpose**: Execute diagnostic commands and return appropriate exit codes.

**Interface**:
```csharp
public interface IDiagnosticCommandExecutor
{
    Task<int> ExecuteAsync(DiagnosticCommand command, CommandLineOptions options);
}
```

**Responsibilities**:
- Route diagnostic commands to appropriate handlers
- Coordinate service dependencies
- Format and display results
- Return appropriate exit codes

### 3. HealthCheckService (New)

**Purpose**: Perform comprehensive health checks on all services.

**Interface**:
```csharp
public interface IHealthCheckService
{
    Task<HealthCheckResult> CheckAllServicesAsync();
    Task<ServiceHealthStatus> CheckOllamaAsync();
    Task<ServiceHealthStatus> CheckScyllaDbAsync();
    Task<ServiceHealthStatus> CheckEmbeddingServiceAsync();
    Task<ServiceHealthStatus> CheckKnowledgeBaseAsync();
}

public class HealthCheckResult
{
    public Dictionary<string, ServiceHealthStatus> ServiceStatuses { get; set; }
    public bool AllHealthy => ServiceStatuses.Values.All(s => s.IsHealthy);
    public TimeSpan TotalCheckDuration { get; set; }
}

public class ServiceHealthStatus
{
    public string ServiceName { get; set; }
    public bool IsHealthy { get; set; }
    public string Message { get; set; }
    public TimeSpan ResponseTime { get; set; }
    public Dictionary<string, string> Details { get; set; }
}
```

### 4. ConfigurationValidationService (New)

**Purpose**: Validate all configuration settings.

**Interface**:
```csharp
public interface IConfigurationValidationService
{
    Task<ConfigurationValidationResult> ValidateAsync();
    ValidationResult ValidateOllamaConfig(OllamaSettings settings);
    ValidationResult ValidateScyllaDbConfig(ScyllaDbSettings settings);
    ValidationResult ValidateVectorConfig(VectorSettings settings);
    ValidationResult ValidateApplicationConfig(ApplicationSettings settings);
}

public class ConfigurationValidationResult
{
    public List<ValidationResult> Results { get; set; }
    public bool IsValid => Results.All(r => r.IsValid);
    public List<string> Errors => Results.Where(r => !r.IsValid)
                                         .SelectMany(r => r.Errors)
                                         .ToList();
}

public class ValidationResult
{
    public string Section { get; set; }
    public bool IsValid { get; set; }
    public List<string> Errors { get; set; }
    public List<string> Warnings { get; set; }
}
```

### 5. ServiceTestingService (New)

**Purpose**: Test individual services independently.

**Interface**:
```csharp
public interface IServiceTestingService
{
    Task<TestResult> TestOllamaAsync();
    Task<TestResult> TestScyllaDbAsync();
    Task<TestResult> TestEmbeddingAsync();
    Task<TestResult> TestSimilarityAsync();
}

public class TestResult
{
    public string TestName { get; set; }
    public bool Success { get; set; }
    public string Message { get; set; }
    public TimeSpan Duration { get; set; }
    public Dictionary<string, object> Metrics { get; set; }
}
```

### 6. KnowledgeBaseManagementService (New)

**Purpose**: Manage knowledge base operations.

**Interface**:
```csharp
public interface IKnowledgeBaseManagementService
{
    Task<ReinitializeResult> ReinitializeAsync();
    Task<ValidationResult> ValidateAsync();
    Task<int> ClearAllEmbeddingsAsync();
    Task<List<string>> ScanJsonFilesAsync();
}

public class ReinitializeResult
{
    public int FilesProcessed { get; set; }
    public int EmbeddingsCreated { get; set; }
    public int Errors { get; set; }
    public TimeSpan Duration { get; set; }
    public List<string> ErrorMessages { get; set; }
}
```

### 7. BenchmarkService (New)

**Purpose**: Execute performance benchmarks.

**Interface**:
```csharp
public interface IBenchmarkService
{
    Task<BenchmarkResult> RunAllBenchmarksAsync();
    Task<BenchmarkResult> BenchmarkSimilarityAsync();
    Task<BenchmarkResult> BenchmarkVectorOperationsAsync();
    Task<BenchmarkResult> BenchmarkEmbeddingAsync();
}

public class BenchmarkResult
{
    public string BenchmarkName { get; set; }
    public int Iterations { get; set; }
    public TimeSpan TotalDuration { get; set; }
    public double AverageDurationMs { get; set; }
    public double OperationsPerSecond { get; set; }
    public long MemoryUsedBytes { get; set; }
    public Dictionary<string, double> AdditionalMetrics { get; set; }
}
```

### 8. HelpFormatter (New)

**Purpose**: Format and display help information.

**Interface**:
```csharp
public interface IHelpFormatter
{
    void DisplayHelp();
    void DisplayUsageExamples();
    void DisplayDiagnosticCommands();
}
```

## Data Models

### CommandLineOptions Enhancement

The existing `ParsedArguments` class in `InputHandler` will be replaced with a more comprehensive `CommandLineOptions` class that supports all diagnostic commands.

### Exit Code Constants

```csharp
public static class ExitCodes
{
    public const int Success = 0;
    public const int GeneralError = 1;
    public const int ConfigurationError = 2;
    public const int ServiceUnavailable = 3;
    public const int ValidationError = 4;
    public const int ArgumentError = 5;
}
```

## Error Handling

### Error Handling Strategy

1. **Argument Parsing Errors**:
   - Display clear error message
   - Show correct usage
   - Exit with code 5 (ArgumentError)

2. **Service Connection Errors**:
   - Display service name and error details
   - Suggest troubleshooting steps
   - Exit with code 3 (ServiceUnavailable)

3. **Configuration Errors**:
   - Display specific configuration issues
   - Show expected format/values
   - Exit with code 2 (ConfigurationError)

4. **Validation Errors**:
   - Display all validation failures
   - Provide actionable recommendations
   - Exit with code 4 (ValidationError)

5. **Unexpected Errors**:
   - Log full exception details
   - Display user-friendly error message
   - Exit with code 1 (GeneralError)

### Error Message Format

```
ERROR: [Error Type]
Description: [Detailed description]
Suggestion: [How to fix]

For more help, run: dotnet JSON-Whisperer.dll --help
```

## Testing Strategy

### Unit Tests

1. **CommandLineParser Tests**:
   - Test all flag combinations
   - Test invalid argument detection
   - Test conflicting flag detection
   - Test help flag behavior

2. **HealthCheckService Tests**:
   - Mock service dependencies
   - Test individual health checks
   - Test aggregate health check
   - Test timeout handling

3. **ConfigurationValidationService Tests**:
   - Test valid configurations
   - Test invalid URL formats
   - Test missing required values
   - Test out-of-range numeric values

4. **ServiceTestingService Tests**:
   - Mock Ollama service
   - Mock ScyllaDB service
   - Test connection failures
   - Test successful connections

5. **BenchmarkService Tests**:
   - Test benchmark execution
   - Test metrics collection
   - Test result formatting

### Integration Tests

1. **End-to-End Diagnostic Commands**:
   - Test `--health-check` with real services
   - Test `--validate-config` with various configurations
   - Test `--test-ollama` with running Ollama
   - Test `--test-scylla` with running ScyllaDB

2. **Command-Line Argument Integration**:
   - Test diagnostic commands override normal execution
   - Test verbose mode with diagnostic commands
   - Test help display

### Manual Testing Scenarios

1. **Health Check Scenarios**:
   - All services healthy
   - Ollama unavailable
   - ScyllaDB unavailable
   - Knowledge base not initialized

2. **Configuration Validation Scenarios**:
   - Valid configuration
   - Invalid Ollama URL
   - Missing required settings
   - Out-of-range values

3. **Service Testing Scenarios**:
   - Test each service independently
   - Test with services down
   - Test with incorrect credentials

4. **Benchmark Scenarios**:
   - Run benchmarks with small dataset
   - Run benchmarks with large dataset
   - Compare performance across runs

## Implementation Phases

### Phase 1: Core Infrastructure (Foundation)
- Create `CommandLineParser` and `CommandLineOptions`
- Create `DiagnosticCommandExecutor`
- Update `Program.cs` to route to diagnostic executor
- Create exit code constants

### Phase 2: Health Check Implementation
- Create `HealthCheckService`
- Implement individual service health checks
- Implement aggregate health check
- Add health check command handler

### Phase 3: Configuration Validation
- Create `ConfigurationValidationService`
- Implement validation for each configuration section
- Add configuration validation command handler

### Phase 4: Service Testing
- Create `ServiceTestingService`
- Implement Ollama testing
- Implement ScyllaDB testing
- Implement embedding testing
- Add service test command handlers

### Phase 5: Knowledge Base Management
- Create `KnowledgeBaseManagementService`
- Implement reinitialize functionality
- Implement validation functionality
- Add knowledge base command handlers

### Phase 6: Benchmarking
- Create `BenchmarkService`
- Implement similarity benchmark
- Implement vector operations benchmark
- Implement embedding benchmark
- Add benchmark command handlers

### Phase 7: Help and Documentation
- Create `HelpFormatter`
- Implement comprehensive help display
- Add usage examples
- Update command-line documentation

### Phase 8: Verbose Mode and Flags
- Update verbose mode to override configuration
- Implement `--no-similarity` flag
- Ensure flag combinations work correctly

### Phase 9: Testing and Polish
- Write unit tests for all new components
- Write integration tests
- Perform manual testing
- Update documentation

## Performance Considerations

1. **Fast Startup for Diagnostic Commands**:
   - Diagnostic commands should not initialize unnecessary services
   - Health checks should run in parallel where possible
   - Benchmarks should be isolated from normal operations

2. **Resource Usage**:
   - Benchmarks should clean up resources after execution
   - Health checks should have reasonable timeouts
   - Service tests should not leave connections open

3. **Caching**:
   - Configuration validation results can be cached
   - Health check results should not be cached (always fresh)

## Security Considerations

1. **Configuration Display**:
   - Never display passwords or sensitive credentials
   - Mask sensitive values in validation output
   - Sanitize error messages

2. **File System Access**:
   - Validate file paths in knowledge base operations
   - Prevent directory traversal attacks
   - Check file permissions before operations

3. **Network Operations**:
   - Use timeouts for all network operations
   - Validate URLs before connection attempts
   - Handle SSL/TLS errors gracefully

## Backward Compatibility

1. **Existing Command-Line Arguments**:
   - All existing arguments (`--file`, `--verbose`, `--help`) continue to work
   - Existing behavior is preserved when no diagnostic flags are used
   - JSON processing workflow remains unchanged

2. **Configuration Files**:
   - No changes to configuration file format
   - All existing settings remain valid
   - New validation does not break existing configurations

3. **Exit Codes**:
   - Success (0) and general error (1) codes remain the same
   - New specific error codes (2-5) are additions, not changes

## Dependencies

### New NuGet Packages
- None required (all functionality can be implemented with existing dependencies)

### Service Dependencies
- Ollama service (existing)
- ScyllaDB service (existing)
- File system access (existing)

## Deployment Considerations

1. **Docker Deployment**:
   - Diagnostic commands work in containerized environment
   - Health checks can verify container networking
   - Benchmarks account for container resource limits

2. **CI/CD Integration**:
   - Health checks can be used in deployment pipelines
   - Configuration validation can run in CI
   - Exit codes enable automated testing

3. **Production Monitoring**:
   - Health check endpoint can be called by monitoring systems
   - Benchmarks can establish performance baselines
   - Service tests can verify deployment success

## Documentation Updates

The following documentation files will need updates:

1. **README.md**:
   - Update command-line options table
   - Add diagnostic commands section
   - Update troubleshooting section

2. **USAGE_EXAMPLES.md**:
   - Add examples for all diagnostic commands
   - Show common troubleshooting workflows
   - Demonstrate benchmark usage

3. **TROUBLESHOOTING.md**:
   - Reference diagnostic commands
   - Update diagnostic command examples
   - Add exit code reference

4. **DEPLOYMENT.md**:
   - Add health check usage in deployment
   - Show configuration validation in CI/CD
   - Document benchmark baseline establishment
