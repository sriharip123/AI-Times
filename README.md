# JSON-Whisperer

![Tests](https://github.com/sriharip123/AI-Times/actions/workflows/test.yml/badge.svg)
![CI](https://github.com/sriharip123/AI-Times/actions/workflows/ci.yml/badge.svg)
![.NET](https://img.shields.io/badge/.NET-9.0-512BD4)
![License](https://img.shields.io/badge/license-MIT-blue.svg)

A C# .NET 9 console application that analyzes JSON objects and generates human-readable summaries in plain English using the Ollama Mistral language model.

## Features

- 🔍 **JSON Analysis**: Parses and analyzes JSON structure, depth, and complexity
- 🤖 **AI-Powered Summaries**: Generates business-friendly explanations using Ollama Mistral
- 📊 **Performance Monitoring**: Built-in timing and memory usage tracking
- 🛠️ **Flexible Input**: Supports command line arguments, file paths, and stdin
- ⚙️ **Configurable**: Environment variable support for deployment scenarios
- 📝 **Comprehensive Logging**: Structured logging with diagnostic information

## Quick Start

### Prerequisites

1. **.NET 9 SDK** - [Download here](https://dotnet.microsoft.com/download/dotnet/9.0)
2. **Ollama** - [Installation guide](#ollama-setup)
3. **Mistral Model** - [Setup instructions](#model-installation)

### Installation

1. Clone the repository:
   ```bash
   git clone <repository-url>
   cd JSON-Whisperer
   ```

2. Build the application:
   ```bash
   dotnet build
   ```

3. Run the application:
   ```bash
   dotnet run --project JSON-Whisperer -- '{"name": "John", "age": 30}'
   ```

## Ollama Setup

### Windows

1. Download Ollama from [ollama.ai](https://ollama.ai)
2. Run the installer
3. Open Command Prompt or PowerShell
4. Verify installation:
   ```cmd
   ollama --version
   ```

### macOS

1. Download Ollama from [ollama.ai](https://ollama.ai)
2. Install the .dmg file
3. Open Terminal
4. Verify installation:
   ```bash
   ollama --version
   ```

### Linux

1. Install using curl:
   ```bash
   curl -fsSL https://ollama.ai/install.sh | sh
   ```
2. Verify installation:
   ```bash
   ollama --version
   ```

### Starting Ollama Service

After installation, start the Ollama service:

```bash
# Start Ollama (runs on http://localhost:11434 by default)
ollama serve
```

The service will run in the background. You can verify it's running by visiting http://localhost:11434 in your browser.

## Model Installation

Install the Mistral model (required for JSON-Whisperer):

```bash
# Pull the Mistral model (this may take a few minutes)
ollama pull mistral

# Verify the model is installed
ollama list
```

You should see `mistral` in the list of installed models.

## Usage

### Command Line Arguments

```bash
# Analyze JSON from command line argument
dotnet run --project JSON-Whisperer -- '{"user": {"name": "Alice", "preferences": {"theme": "dark"}}}'

# Analyze JSON from file (using --file flag)
dotnet run --project JSON-Whisperer -- --file data.json
dotnet run --project JSON-Whisperer -- -f data.json  # Short form

# Analyze JSON from file (direct path - backward compatibility)
dotnet run --project JSON-Whisperer -- data.json

# Read JSON from stdin
echo '{"status": "active", "count": 42}' | dotnet run --project JSON-Whisperer

# Enable verbose mode for detailed output
dotnet run --project JSON-Whisperer -- --verbose '{"data": [1,2,3]}'
dotnet run --project JSON-Whisperer -- -v '{"data": [1,2,3]}'  # Short form

# Combine flags
dotnet run --project JSON-Whisperer -- --file data.json --verbose

# Show help
dotnet run --project JSON-Whisperer -- --help
dotnet run --project JSON-Whisperer -- -h
```

### Input Methods

1. **Direct JSON**: Pass JSON as a command line argument
2. **File Input**: Use `--file <path>` or `-f <path>` to read from a file
3. **File Path**: Provide a file path directly (backward compatibility)
4. **Stdin**: Pipe JSON data to the application
5. **Interactive**: Run without arguments to enter JSON interactively

### Command Line Flags

#### Input and Display Options

| Flag | Short | Description | Example |
|------|-------|-------------|---------|
| `--file` | `-f` | Read JSON from specified file | `--file data.json` |
| `--verbose` | `-v` | Enable verbose output with detailed logging | `--verbose` |
| `--no-similarity` | | Disable vector similarity matching for faster processing | `--no-similarity` |
| `--help` | `-h` | Show help information | `--help` |

#### Diagnostic Commands

| Flag | Description | Exit Code |
|------|-------------|-----------|
| `--health-check` | Verify all services (Ollama, ScyllaDB, embeddings) are operational | 0 = healthy, 1 = unhealthy |
| `--validate-config` | Validate configuration settings and service connectivity | 0 = valid, 1 = invalid |
| `--test-ollama` | Test Ollama service connectivity and model availability | 0 = success, 1 = failure |
| `--test-scylla` | Test ScyllaDB connectivity and keyspace verification | 0 = success, 1 = failure |
| `--test-embedding` | Test embedding generation with nomic-embed-text model | 0 = success, 1 = failure |
| `--test-similarity` | Test similarity search functionality | 0 = success, 1 = failure |

#### Knowledge Base Management

| Flag | Description | Exit Code |
|------|-------------|-----------|
| `--reinitialize-knowledge-base` | Clear and regenerate all embeddings from AppData files | 0 = success, 1 = failure |
| `--validate-knowledge-base` | Verify JSON files and description files are valid | 0 = valid, 1 = invalid |

#### Performance Benchmarks

| Flag | Description | Exit Code |
|------|-------------|-----------|
| `--benchmark-all` | Run all available performance benchmarks | 0 = success |
| `--benchmark-similarity` | Benchmark similarity search performance | 0 = success |
| `--benchmark-vector-operations` | Benchmark embedding generation and storage | 0 = success |
| `--benchmark-embedding` | Benchmark embedding generation speed | 0 = success |

**Note**: Always use `--` to separate dotnet run arguments from application arguments:
```bash
dotnet run --project JSON-Whisperer -- [application arguments]
```

### Example Output

```
🔍 JSON-Whisperer Analysis Results
═══════════════════════════════════════════════════════════════════════════════

📄 Original JSON Structure:
───────────────────────────────────────────────────────────────────────────────
{
  "user": {
    "name": "Alice",
    "age": 28,
    "preferences": {
      "theme": "dark",
      "notifications": true
    }
  }
}

🤖 AI-Generated Summary:
───────────────────────────────────────────────────────────────────────────────
This JSON represents a user profile system containing personal information and 
user preferences. The data structure includes a user's basic details like name 
and age, along with their application settings such as theme preference and 
notification settings. This type of data is commonly used in web applications 
for personalizing the user experience and storing user-specific configurations.

📊 Analysis Metadata:
───────────────────────────────────────────────────────────────────────────────
📏 Size: 156 bytes
🏗️  Structure: 4 properties, 3 levels deep
📋 Arrays: 0 array field(s)
🗂️  Objects: 2 nested object(s)
⏱️  Processing Time: 1,234ms
🕐 Analyzed: 2024-11-02 15:30:45 UTC

═══════════════════════════════════════════════════════════════════════════════
✅ Analysis completed at 2024-11-02 15:30:45
```

## Configuration

### Configuration File (appsettings.json)

```json
{
  "Ollama": {
    "BaseUrl": "http://localhost:11434",
    "ModelName": "mistral",
    "TimeoutSeconds": 30,
    "RetryAttempts": 3,
    "RetryDelaySeconds": 2
  },
  "Application": {
    "VerboseMode": false,
    "MaxJsonSizeBytes": 10485760,
    "EnablePerformanceMetrics": true,
    "OutputFormat": "standard"
  },
  "Performance": {
    "EnableTiming": true,
    "EnableMemoryTracking": false,
    "WarnOnSlowOperationsMs": 5000
  }
}
```

### Environment Variables

Override configuration using environment variables:

| Variable | Description | Example |
|----------|-------------|---------|
| `OLLAMA_BASE_URL` | Ollama service URL | `http://localhost:11434` |
| `OLLAMA_MODEL_NAME` | Model to use | `mistral` |
| `OLLAMA_TIMEOUT_SECONDS` | Request timeout | `30` |
| `OLLAMA_RETRY_ATTEMPTS` | Retry attempts | `3` |
| `APP_VERBOSE_MODE` | Enable verbose output | `true` |
| `APP_MAX_JSON_SIZE_BYTES` | Max JSON size | `10485760` |
| `APP_OUTPUT_FORMAT` | Output format | `standard` |
| `PERF_ENABLE_TIMING` | Enable performance timing | `true` |

### Example Environment Setup

```bash
# Windows (Command Prompt)
set OLLAMA_BASE_URL=http://remote-ollama:11434
set APP_VERBOSE_MODE=true
dotnet run --project JSON-Whisperer

# Windows (PowerShell)
$env:OLLAMA_BASE_URL="http://remote-ollama:11434"
$env:APP_VERBOSE_MODE="true"
dotnet run --project JSON-Whisperer

# Linux/macOS
export OLLAMA_BASE_URL=http://remote-ollama:11434
export APP_VERBOSE_MODE=true
dotnet run --project JSON-Whisperer
```

## Deployment

### Standalone Deployment

Create a self-contained deployment:

```bash
# Build for current platform
dotnet publish -c Release --self-contained true

# Build for specific platform
dotnet publish -c Release -r win-x64 --self-contained true
dotnet publish -c Release -r linux-x64 --self-contained true
dotnet publish -c Release -r osx-x64 --self-contained true
```

### Docker Deployment

Create a `Dockerfile`:

```dockerfile
FROM mcr.microsoft.com/dotnet/runtime:9.0
WORKDIR /app
COPY bin/Release/net9.0/publish/ .
ENTRYPOINT ["dotnet", "JSON-Whisperer.dll"]
```

Build and run:

```bash
docker build -t json-whisperer .
docker run -e OLLAMA_BASE_URL=http://host.docker.internal:11434 json-whisperer '{"test": true}'
```

### Production Considerations

1. **Ollama Service**: Ensure Ollama is running and accessible
2. **Model Availability**: Verify the Mistral model is installed
3. **Network Access**: Configure firewall rules for Ollama port (11434)
4. **Resource Limits**: Set appropriate memory limits for large JSON files
5. **Logging**: Configure structured logging for production monitoring

## Troubleshooting

### Common Issues

#### 1. "Ollama service is not available"

**Symptoms**: Application fails with connection error

**Solutions**:
- Verify Ollama is running: `ollama list`
- Check service URL: `curl http://localhost:11434/api/tags`
- Restart Ollama service: `ollama serve`
- Check firewall settings

#### 2. "Model 'mistral' is not available"

**Symptoms**: Model not found error

**Solutions**:
- Install the model: `ollama pull mistral`
- Verify installation: `ollama list`
- Check model name in configuration

#### 3. "JSON input is too large"

**Symptoms**: File size error

**Solutions**:
- Increase `MaxJsonSizeBytes` in configuration
- Use environment variable: `APP_MAX_JSON_SIZE_BYTES=20971520`
- Split large JSON files into smaller chunks

#### 4. "Invalid JSON format"

**Symptoms**: JSON parsing error

**Solutions**:
- Validate JSON syntax using online tools
- Check for trailing commas or missing quotes
- Ensure proper escaping of special characters
- Verify file path is correct when using `--file` flag
- Use absolute paths if relative paths don't work:
  ```bash
  # Use absolute path
  dotnet run --project JSON-Whisperer -- --file /full/path/to/data.json
  ```

#### 5. Performance Issues

**Symptoms**: Slow processing or high memory usage

**Solutions**:
- Enable performance monitoring: `APP_ENABLE_PERFORMANCE_METRICS=true`
- Reduce JSON complexity or size
- Increase Ollama timeout: `OLLAMA_TIMEOUT_SECONDS=60`
- Monitor system resources

### Diagnostic Commands

The application includes comprehensive diagnostic commands for testing and troubleshooting:

```bash
# System Health Check - Verify all services are operational
dotnet run --project JSON-Whisperer -- --health-check

# Configuration Validation - Check all settings are valid
dotnet run --project JSON-Whisperer -- --validate-config

# Test Individual Services
dotnet run --project JSON-Whisperer -- --test-ollama      # Test Ollama connectivity
dotnet run --project JSON-Whisperer -- --test-scylla      # Test ScyllaDB connectivity
dotnet run --project JSON-Whisperer -- --test-embedding   # Test embedding generation
dotnet run --project JSON-Whisperer -- --test-similarity  # Test similarity search

# Knowledge Base Management
dotnet run --project JSON-Whisperer -- --validate-knowledge-base      # Verify JSON files
dotnet run --project JSON-Whisperer -- --reinitialize-knowledge-base  # Regenerate embeddings

# Performance Benchmarks
dotnet run --project JSON-Whisperer -- --benchmark-all              # Run all benchmarks
dotnet run --project JSON-Whisperer -- --benchmark-similarity       # Benchmark similarity search
dotnet run --project JSON-Whisperer -- --benchmark-vector-operations # Benchmark vector ops
dotnet run --project JSON-Whisperer -- --benchmark-embedding        # Benchmark embeddings

# Basic Testing
echo '{"test": true}' | dotnet run --project JSON-Whisperer
dotnet run --project JSON-Whisperer -- --file test.json --verbose
dotnet run --project JSON-Whisperer -- --verbose '{"debug": true}'
dotnet run --project JSON-Whisperer -- --help
```

#### Exit Codes

The application uses standard exit codes for automation and CI/CD integration:

| Exit Code | Meaning | When Returned |
|-----------|---------|---------------|
| 0 | Success | All operations completed successfully, all health checks passed, all tests passed |
| 1 | Failure | Service unavailable, health check failed, test failed, validation error, or unexpected error |

**Exit Code Usage Examples:**

```bash
# Example 1: Check health before processing
if dotnet JSON-Whisperer.dll --health-check; then
  echo "All services healthy, proceeding..."
  dotnet JSON-Whisperer.dll --file data.json
else
  echo "Health check failed, aborting"
  exit 1
fi

# Example 2: Validate configuration in CI/CD pipeline
dotnet JSON-Whisperer.dll --validate-config
if [ $? -eq 0 ]; then
  echo "Configuration valid"
else
  echo "Configuration validation failed"
  exit 1
fi

# Example 3: Test services before deployment
dotnet JSON-Whisperer.dll --test-ollama && \
dotnet JSON-Whisperer.dll --test-scylla && \
dotnet JSON-Whisperer.dll --test-embedding
if [ $? -eq 0 ]; then
  echo "All service tests passed"
else
  echo "Service tests failed"
  exit 1
fi

# Example 4: Run benchmarks and capture results
if dotnet JSON-Whisperer.dll --benchmark-all > benchmark-results.txt; then
  echo "Benchmarks completed successfully"
  cat benchmark-results.txt
else
  echo "Benchmarks failed"
  exit 1
fi
```

### Log Analysis

Enable detailed logging by setting environment variables:

```bash
export Logging__LogLevel__Default=Debug
export Logging__LogLevel__JSON_Whisperer=Debug
dotnet run --project JSON-Whisperer
```

## Development

### Building from Source

```bash
# Clone repository
git clone <repository-url>
cd JSON-Whisperer

# Restore dependencies
dotnet restore

# Build solution
dotnet build

# Run tests
dotnet test

# Run application
dotnet run --project JSON-Whisperer
```

### Project Structure

```
JSON-Whisperer/
├── JSON-Whisperer/           # Main application
│   ├── Interfaces/           # Service interfaces
│   ├── Models/              # Data models
│   ├── Services/            # Service implementations
│   ├── Program.cs           # Application entry point
│   └── appsettings.json     # Configuration
├── JSON-Whisperer.Tests/    # Unit tests
└── README.md               # This file
```

### Contributing

1. Fork the repository
2. Create a feature branch
3. Make your changes
4. Add tests for new functionality
5. Ensure all tests pass
6. Submit a pull request

## License

[Add your license information here]

## Exit Code Reference

The application uses consistent exit codes for all operations, making it suitable for automation, scripting, and CI/CD pipelines.

### Exit Code Summary

| Exit Code | Status | Description |
|-----------|--------|-------------|
| 0 | Success | Operation completed successfully |
| 1 | Failure | Operation failed (see specific scenarios below) |

### Exit Code Scenarios

#### Exit Code 0 (Success)
- Normal JSON processing completed successfully
- Health check passed (all services healthy)
- Configuration validation passed (all settings valid)
- Service test passed (service is operational)
- Knowledge base validation passed (all files valid)
- Knowledge base reinitialization completed successfully
- Benchmark completed successfully
- Help information displayed

#### Exit Code 1 (Failure)
- JSON parsing error
- Ollama service unavailable or unreachable
- ScyllaDB connection failed
- Embedding generation failed
- Health check failed (one or more services unhealthy)
- Configuration validation failed (invalid settings)
- Service test failed (service not operational)
- Knowledge base validation failed (invalid files)
- Knowledge base reinitialization failed
- Invalid command-line arguments
- Unexpected error or exception

### Using Exit Codes in Scripts

```bash
# Example 1: Simple health check
if dotnet JSON-Whisperer.dll --health-check; then
  echo "Services are healthy"
else
  echo "Services are unhealthy"
  exit 1
fi

# Example 2: Pre-deployment validation
dotnet JSON-Whisperer.dll --validate-config || exit 1
dotnet JSON-Whisperer.dll --health-check || exit 1
echo "Deployment validation passed"

# Example 3: Conditional processing
if dotnet JSON-Whisperer.dll --test-ollama; then
  echo "Processing JSON with Ollama..."
  dotnet JSON-Whisperer.dll --file data.json
else
  echo "Ollama unavailable, skipping processing"
fi

# Example 4: CI/CD pipeline
set -e  # Exit on any failure
dotnet JSON-Whisperer.dll --validate-config
dotnet JSON-Whisperer.dll --health-check
dotnet JSON-Whisperer.dll --test-ollama
dotnet JSON-Whisperer.dll --test-scylla
echo "All checks passed, proceeding with deployment"
```

## Support

For issues and questions:
- Check the [troubleshooting section](#troubleshooting)
- Run diagnostic commands to identify issues
- Review application logs with verbose mode enabled
- Verify Ollama service status and model availability
- Use `--health-check` to verify all services are operational