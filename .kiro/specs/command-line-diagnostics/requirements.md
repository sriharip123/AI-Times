# Requirements Document

## Introduction

This specification defines the requirements for implementing comprehensive command-line diagnostic and testing features for JSON-Whisperer. Currently, the documentation references several diagnostic commands that are not implemented in the application. This feature will add these missing command-line options to enable users to test, validate, and troubleshoot the application without processing JSON data.

## Glossary

- **Application**: The JSON-Whisperer console application
- **Diagnostic Command**: A command-line flag that performs system checks or tests without processing JSON
- **Health Check**: A verification that all required services (Ollama, ScyllaDB) are available and functioning
- **Benchmark**: A performance test that measures operation speed and resource usage
- **Configuration Validation**: Verification that all configuration settings are valid and services are reachable
- **Knowledge Base**: The collection of example JSON files and their embeddings stored in ScyllaDB
- **Vector Services**: The combination of ScyllaDB database and embedding generation services
- **Ollama Service**: The AI service that provides text generation and embedding capabilities
- **ScyllaDB**: The vector database used for similarity matching

## Requirements

### Requirement 1: Health Check Command

**User Story:** As a system administrator, I want to run a health check command, so that I can verify all services are operational before processing JSON data.

#### Acceptance Criteria

1. WHEN the user executes the Application with the `--health-check` flag, THE Application SHALL verify Ollama service connectivity and return status
2. WHEN the user executes the Application with the `--health-check` flag, THE Application SHALL verify ScyllaDB connectivity and return status
3. WHEN the user executes the Application with the `--health-check` flag, THE Application SHALL verify embedding service availability and return status
4. WHEN the user executes the Application with the `--health-check` flag, THE Application SHALL verify knowledge base initialization status and return status
5. WHEN all health checks pass, THE Application SHALL exit with code 0
6. WHEN any health check fails, THE Application SHALL exit with code 1 and display which service failed

### Requirement 2: Configuration Validation Command

**User Story:** As a developer, I want to validate my configuration settings, so that I can identify configuration errors before running the application.

#### Acceptance Criteria

1. WHEN the user executes the Application with the `--validate-config` flag, THE Application SHALL load all configuration settings from appsettings.json and environment variables
2. WHEN the user executes the Application with the `--validate-config` flag, THE Application SHALL verify all required configuration values are present
3. WHEN the user executes the Application with the `--validate-config` flag, THE Application SHALL validate URL formats for Ollama and ScyllaDB endpoints
4. WHEN the user executes the Application with the `--validate-config` flag, THE Application SHALL verify numeric configuration values are within valid ranges
5. WHEN configuration is valid, THE Application SHALL display a success message and exit with code 0
6. WHEN configuration is invalid, THE Application SHALL display specific validation errors and exit with code 1

### Requirement 3: Individual Service Testing Commands

**User Story:** As a DevOps engineer, I want to test individual services independently, so that I can isolate connectivity issues during troubleshooting.

#### Acceptance Criteria

1. WHEN the user executes the Application with the `--test-ollama` flag, THE Application SHALL test connectivity to the Ollama service
2. WHEN the user executes the Application with the `--test-ollama` flag, THE Application SHALL verify the Mistral model is available
3. WHEN the user executes the Application with the `--test-scylla` flag, THE Application SHALL test connectivity to ScyllaDB
4. WHEN the user executes the Application with the `--test-scylla` flag, THE Application SHALL verify the keyspace exists or can be created
5. WHEN the user executes the Application with the `--test-embedding` flag, THE Application SHALL test embedding generation with the nomic-embed-text model
6. WHEN the user executes the Application with the `--test-embedding` flag, THE Application SHALL verify embedding dimensions match configuration
7. WHEN a service test passes, THE Application SHALL display success details and exit with code 0
8. WHEN a service test fails, THE Application SHALL display error details and exit with code 1

### Requirement 4: Knowledge Base Management Commands

**User Story:** As a data administrator, I want to reinitialize the knowledge base, so that I can refresh embeddings after updating example JSON files.

#### Acceptance Criteria

1. WHEN the user executes the Application with the `--reinitialize-knowledge-base` flag, THE Application SHALL clear all existing embeddings from ScyllaDB
2. WHEN the user executes the Application with the `--reinitialize-knowledge-base` flag, THE Application SHALL scan the AppData directory for JSON files
3. WHEN the user executes the Application with the `--reinitialize-knowledge-base` flag, THE Application SHALL generate embeddings for all discovered JSON files
4. WHEN the user executes the Application with the `--reinitialize-knowledge-base` flag, THE Application SHALL store new embeddings in ScyllaDB
5. WHEN the user executes the Application with the `--validate-knowledge-base` flag, THE Application SHALL verify all JSON files have corresponding description files
6. WHEN the user executes the Application with the `--validate-knowledge-base` flag, THE Application SHALL verify all JSON files are valid JSON format
7. WHEN the user executes the Application with the `--validate-knowledge-base` flag, THE Application SHALL report any missing or invalid files
8. WHEN knowledge base operations complete successfully, THE Application SHALL display summary statistics and exit with code 0

### Requirement 5: Performance Benchmark Commands

**User Story:** As a performance engineer, I want to run benchmarks, so that I can measure system performance and identify bottlenecks.

#### Acceptance Criteria

1. WHEN the user executes the Application with the `--benchmark-all` flag, THE Application SHALL execute all available benchmarks
2. WHEN the user executes the Application with the `--benchmark-similarity` flag, THE Application SHALL measure similarity search performance
3. WHEN the user executes the Application with the `--benchmark-vector-operations` flag, THE Application SHALL measure embedding generation and storage performance
4. WHEN the user executes the Application with the `--benchmark-embedding` flag, THE Application SHALL measure embedding generation speed
5. WHEN benchmarks execute, THE Application SHALL display timing metrics in milliseconds
6. WHEN benchmarks execute, THE Application SHALL display memory usage metrics
7. WHEN benchmarks execute, THE Application SHALL display operations per second metrics
8. WHEN benchmarks complete, THE Application SHALL exit with code 0

### Requirement 6: Verbose Mode Override

**User Story:** As a developer, I want the --verbose flag to override configuration settings, so that I can enable detailed logging without modifying configuration files.

#### Acceptance Criteria

1. WHEN the user executes the Application with the `--verbose` flag, THE Application SHALL override the VerboseMode configuration setting to true
2. WHEN the user executes the Application with the `--verbose` flag, THE Application SHALL display detailed diagnostic information during execution
3. WHEN the user executes the Application with the `--verbose` flag, THE Application SHALL show similarity matching results if enabled
4. WHEN the user executes the Application with the `--verbose` flag, THE Application SHALL display performance metrics
5. WHEN the user executes the Application with the `-v` short flag, THE Application SHALL behave identically to `--verbose`

### Requirement 7: Similarity Matching Control

**User Story:** As a user, I want to disable similarity matching for a single run, so that I can get faster results when I don't need similarity features.

#### Acceptance Criteria

1. WHEN the user executes the Application with the `--no-similarity` flag, THE Application SHALL disable vector similarity matching for that execution
2. WHEN the user executes the Application with the `--no-similarity` flag, THE Application SHALL skip knowledge base initialization
3. WHEN the user executes the Application with the `--no-similarity` flag, THE Application SHALL not attempt to connect to ScyllaDB
4. WHEN the user executes the Application with the `--no-similarity` flag, THE Application SHALL process JSON and generate summaries without similarity context
5. WHEN the user executes the Application with the `--no-similarity` flag, THE Application SHALL complete faster than with similarity enabled

### Requirement 8: Help and Usage Information

**User Story:** As a new user, I want comprehensive help information, so that I can understand all available command-line options.

#### Acceptance Criteria

1. WHEN the user executes the Application with the `--help` flag, THE Application SHALL display all available command-line options
2. WHEN the user executes the Application with the `--help` flag, THE Application SHALL display descriptions for each option
3. WHEN the user executes the Application with the `--help` flag, THE Application SHALL display usage examples
4. WHEN the user executes the Application with the `--help` flag, THE Application SHALL group options by category (Input, Diagnostic, Testing, Benchmark)
5. WHEN the user executes the Application with the `-h` flag, THE Application SHALL behave identically to `--help`
6. WHEN help is displayed, THE Application SHALL exit with code 0

### Requirement 9: Command-Line Argument Parsing

**User Story:** As a developer, I want robust argument parsing, so that invalid command combinations are detected and reported clearly.

#### Acceptance Criteria

1. WHEN the user provides multiple diagnostic flags, THE Application SHALL execute only the first diagnostic command and ignore others
2. WHEN the user provides a diagnostic flag with JSON input, THE Application SHALL execute the diagnostic command and ignore JSON input
3. WHEN the user provides an unknown flag, THE Application SHALL display an error message listing valid options
4. WHEN the user provides conflicting flags (e.g., `--verbose` and `--no-similarity` with `--test-scylla`), THE Application SHALL detect the conflict and display an error
5. WHEN argument parsing fails, THE Application SHALL exit with code 1

### Requirement 10: Exit Codes and Error Reporting

**User Story:** As a CI/CD pipeline developer, I want consistent exit codes, so that I can reliably detect success or failure in automated scripts.

#### Acceptance Criteria

1. WHEN any diagnostic command succeeds, THE Application SHALL exit with code 0
2. WHEN any diagnostic command fails, THE Application SHALL exit with code 1
3. WHEN configuration validation fails, THE Application SHALL exit with code 1
4. WHEN a service test fails, THE Application SHALL exit with code 1
5. WHEN argument parsing fails, THE Application SHALL exit with code 1
6. WHEN the Application encounters an unexpected error, THE Application SHALL exit with code 1 and log the error
7. WHEN the Application exits with a non-zero code, THE Application SHALL display a clear error message explaining the failure
