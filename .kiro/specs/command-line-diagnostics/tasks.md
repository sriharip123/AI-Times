# Implementation Plan

- [x] 1. Create core command-line parsing infrastructure





  - Create `Models/CommandLineOptions.cs` with all option properties
  - Create `Models/ExecutionMode.cs` enum
  - Create `Models/DiagnosticCommand.cs` enum
  - Create `Models/ExitCodes.cs` with exit code constants
  - _Requirements: 9.1, 9.2, 10.1, 10.2, 10.3, 10.4, 10.5, 10.6, 10.7_

- [x] 2. Implement CommandLineParser service






  - [x] 2.1 Create `Interfaces/ICommandLineParser.cs` interface

    - Define `Parse(string[] args)` method
    - Define `IsValid(out string errorMessage)` method
    - _Requirements: 9.1, 9.2_
  

  - [x] 2.2 Create `Services/CommandLineParser.cs` implementation

    - Implement argument parsing logic
    - Handle all diagnostic flags (`--health-check`, `--validate-config`, etc.)
    - Handle input flags (`--file`, `--verbose`, `--no-similarity`, `--help`)
    - Detect execution mode based on arguments
    - _Requirements: 9.1, 9.2, 9.3, 9.4, 9.5_
  

  - [x] 2.3 Implement argument validation

    - Validate flag combinations
    - Detect conflicting flags
    - Validate required arguments for each flag
    - _Requirements: 9.1, 9.2, 9.3, 9.4_

- [x] 3. Create DiagnosticCommandExecutor




  - [x] 3.1 Create `Interfaces/IDiagnosticCommandExecutor.cs` interface


    - Define `ExecuteAsync(DiagnosticCommand command, CommandLineOptions options)` method
    - _Requirements: All diagnostic requirements_
  
  - [x] 3.2 Create `Services/DiagnosticCommandExecutor.cs` implementation





    - Implement command routing logic
    - Handle each diagnostic command type
    - Format and display results
    - Return appropriate exit codes
    - _Requirements: All diagnostic requirements_

- [x] 4. Implement HealthCheckService





  - [x] 4.1 Create health check models


    - Create `Models/HealthCheckResult.cs`
    - Create `Models/ServiceHealthStatus.cs`
    - _Requirements: 1.1, 1.2, 1.3, 1.4, 1.5, 1.6_
  
  - [x] 4.2 Create `Interfaces/IHealthCheckService.cs` interface


    - Define `CheckAllServicesAsync()` method
    - Define individual service check methods
    - _Requirements: 1.1, 1.2, 1.3, 1.4_
  
  - [x] 4.3 Create `Services/HealthCheckService.cs` implementation


    - Implement Ollama health check
    - Implement ScyllaDB health check
    - Implement embedding service health check
    - Implement knowledge base health check
    - Implement aggregate health check with parallel execution
    - _Requirements: 1.1, 1.2, 1.3, 1.4, 1.5, 1.6_

- [x] 5. Implement ConfigurationValidationService




  - [x] 5.1 Create validation models


    - Create `Models/ConfigurationValidationResult.cs`
    - Create `Models/ValidationResult.cs`
    - _Requirements: 2.1, 2.2, 2.3, 2.4, 2.5, 2.6_


  
  - [x] 5.2 Create `Interfaces/IConfigurationValidationService.cs` interface





    - Define `ValidateAsync()` method


    - Define section-specific validation methods
    - _Requirements: 2.1, 2.2, 2.3, 2.4_
  
  - [x] 5.3 Create `Services/ConfigurationValidationService.cs` implementation





    - Implement Ollama configuration validation
    - Implement ScyllaDB configuration validation
    - Implement Vector configuration validation
    - Implement Application configuration validation
    - Implement URL format validation
    - Implement numeric range validation
    - _Requirements: 2.1, 2.2, 2.3, 2.4, 2.5, 2.6_

- [x] 6. Implement ServiceTestingService






  - [x] 6.1 Create test result models

    - Create `Models/TestResult.cs`
    - _Requirements: 3.1, 3.2, 3.3, 3.4, 3.5, 3.6, 3.7, 3.8_
  
  - [x] 6.2 Create `Interfaces/IServiceTestingService.cs` interface


    - Define individual service test methods
    - _Requirements: 3.1, 3.3, 3.5_
  
  - [x] 6.3 Create `Services/ServiceTestingService.cs` implementation





    - Implement Ollama service test with model verification
    - Implement ScyllaDB test with keyspace verification
    - Implement embedding service test with dimension verification
    - Implement similarity search test
    - _Requirements: 3.1, 3.2, 3.3, 3.4, 3.5, 3.6, 3.7, 3.8_

- [x] 7. Implement KnowledgeBaseManagementService




  - [x] 7.1 Create knowledge base models


    - Create `Models/ReinitializeResult.cs`
    - Create `Models/KnowledgeBaseValidationResult.cs`
    - _Requirements: 4.1, 4.2, 4.3, 4.4, 4.5, 4.6, 4.7, 4.8_
  
  - [x] 7.2 Create `Interfaces/IKnowledgeBaseManagementService.cs` interface


    - Define `ReinitializeAsync()` method
    - Define `ValidateAsync()` method
    - Define helper methods for file scanning and clearing
    - _Requirements: 4.1, 4.2, 4.3, 4.4, 4.5, 4.6, 4.7_
  
  - [x] 7.3 Create `Services/KnowledgeBaseManagementService.cs` implementation


    - Implement embedding clearing functionality
    - Implement JSON file scanning
    - Implement embedding generation and storage
    - Implement validation for JSON files and descriptions
    - Display progress and summary statistics
    - _Requirements: 4.1, 4.2, 4.3, 4.4, 4.5, 4.6, 4.7, 4.8_

- [x] 8. Implement BenchmarkService





  - [x] 8.1 Create benchmark models

    - Create `Models/BenchmarkResult.cs`
    - _Requirements: 5.1, 5.2, 5.3, 5.4, 5.5, 5.6, 5.7, 5.8_
  

  - [x] 8.2 Create `Interfaces/IBenchmarkService.cs` interface

    - Define benchmark methods for each operation type
    - _Requirements: 5.1, 5.2, 5.3, 5.4_
  

  - [x] 8.3 Create `Services/BenchmarkService.cs` implementation

    - Implement similarity search benchmark
    - Implement vector operations benchmark
    - Implement embedding generation benchmark
    - Implement aggregate benchmark runner
    - Collect timing, memory, and throughput metrics
    - _Requirements: 5.1, 5.2, 5.3, 5.4, 5.5, 5.6, 5.7, 5.8_

- [x] 9. Implement HelpFormatter service






  - [x] 9.1 Create `Interfaces/IHelpFormatter.cs` interface

    - Define `DisplayHelp()` method
    - Define `DisplayUsageExamples()` method
    - _Requirements: 8.1, 8.2, 8.3, 8.4, 8.5, 8.6_
  

  - [x] 9.2 Create `Services/HelpFormatter.cs` implementation

    - Format and display all command-line options
    - Group options by category (Input, Diagnostic, Testing, Benchmark)
    - Display descriptions and examples
    - Display usage patterns
    - _Requirements: 8.1, 8.2, 8.3, 8.4, 8.5, 8.6_

- [x] 10. Update Program.cs to integrate diagnostic commands





  - Modify `ConfigureServices` to register new services
  - Add command-line parsing before service provider build
  - Route to diagnostic executor when diagnostic mode detected
  - Route to normal execution when normal mode detected
  - Route to help display when help requested
  - Handle verbose mode override
  - Handle no-similarity flag
  - _Requirements: 6.1, 6.2, 6.3, 6.4, 6.5, 7.1, 7.2, 7.3, 7.4, 7.5_

- [x] 11. Update InputHandler to use CommandLineParser





  - Refactor `GetJsonInputAsync` to accept `CommandLineOptions`
  - Remove internal argument parsing logic
  - Use options from `CommandLineParser`
  - Maintain backward compatibility
  - _Requirements: 6.1, 6.2, 6.3, 6.4, 6.5_

- [x] 12. Update JsonWhispererApplication for verbose and no-similarity flags





  - Accept `CommandLineOptions` in constructor or `RunAsync`
  - Override `VerboseMode` setting when `--verbose` flag is present
  - Skip vector services initialization when `--no-similarity` flag is present
  - _Requirements: 6.1, 6.2, 6.3, 6.4, 6.5, 7.1, 7.2, 7.3, 7.4, 7.5_

- [x] 13. Create unit tests for CommandLineParser







  - Test parsing of all diagnostic flags
  - Test parsing of input flags
  - Test execution mode detection
  - Test argument validation
  - Test conflicting flag detection
  - Test error message generation
  - _Requirements: 9.1, 9.2, 9.3, 9.4, 9.5_

- [x] 14. Create unit tests for HealthCheckService







  - Mock service dependencies
  - Test individual health checks
  - Test aggregate health check
  - Test parallel execution
  - Test timeout handling
  - Test error handling
  - _Requirements: 1.1, 1.2, 1.3, 1.4, 1.5, 1.6_
-

- [x] 15. Create unit tests for ConfigurationValidationService





  - Test valid configurations
  - Test invalid URL formats
  - Test missing required values
  - Test out-of-range numeric values
  - Test each configuration section
  - _Requirements: 2.1, 2.2, 2.3, 2.4, 2.5, 2.6_

- [x] 16. Create unit tests for ServiceTestingService










  - Mock Ollama service
  - Mock ScyllaDB service
  - Mock embedding service
  - Test connection failures
  - Test successful connections
  - Test model verification
  - _Requirements: 3.1, 3.2, 3.3, 3.4, 3.5, 3.6, 3.7, 3.8_

- [x] 17. Create unit tests for KnowledgeBaseManagementService






  - Mock vector database service
  - Mock file system operations
  - Test reinitialize functionality
  - Test validation functionality
  - Test error handling
  - _Requirements: 4.1, 4.2, 4.3, 4.4, 4.5, 4.6, 4.7, 4.8_


- [x] 18. Create unit tests for BenchmarkService







  - Test benchmark execution
  - Test metrics collection
  - Test result formatting
  - Test resource cleanup
  - _Requirements: 5.1, 5.2, 5.3, 5.4, 5.5, 5.6, 5.7, 5.8_



- [x] 19. Create unit tests for DiagnosticCommandExecutor






  - Test command routing
  - Test exit code generation
  - Test error handling
  - Test result formatting
  - _Requirements: All diagnostic requirements_

- [x] 20. Create integration tests for diagnostic commands






  - Test `--health-check` end-to-end
  - Test `--validate-config` end-to-end
  - Test `--test-ollama` with running service
  - Test `--test-scylla` with running service
  - Test `--reinitialize-knowledge-base` end-to-end
  - Test `--benchmark-all` end-to-end
  - _Requirements: All diagnostic requirements_

- [x] 21. Update documentation








  - Update README.md with new command-line options
  - Update USAGE_EXAMPLES.md with diagnostic command examples
  - Update TROUBLESHOOTING.md with diagnostic command references
  - Update DEPLOYMENT.md with health check usage
  - Add exit code reference to documentation
  - _Requirements: 8.1, 8.2, 8.3, 8.4, 8.5, 8.6_

- [x] 22. Manual testing and validation






  - Test all diagnostic commands with services running
  - Test all diagnostic commands with services down
  - Test verbose mode override
  - Test no-similarity flag
  - Test help display
  - Test error messages and exit codes
  - Test in Docker environment
  - Verify backward compatibility
  - _Requirements: All requirements_
