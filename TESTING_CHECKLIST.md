# Manual Testing Checklist

Quick reference checklist for manual testing of JSON-Whisperer diagnostic commands.

## Pre-Testing Setup

- [ ] .NET 8.0 SDK installed
- [ ] Ollama installed and running
- [ ] ScyllaDB running
- [ ] Mistral model pulled: `ollama pull mistral`
- [ ] Nomic-embed-text model pulled: `ollama pull nomic-embed-text`
- [ ] Application built: `dotnet build`

## Quick Test Commands

### Run All Automated Tests
```bash
# Windows
.\scripts\test-diagnostics.ps1

# Linux/macOS
./scripts/test-diagnostics.sh
```

### Run Specific Test Groups
```bash
# Windows
.\scripts\test-diagnostics.ps1 -TestGroup help
.\scripts\test-diagnostics.ps1 -TestGroup health
.\scripts\test-diagnostics.ps1 -TestGroup config
.\scripts\test-diagnostics.ps1 -TestGroup services
.\scripts\test-diagnostics.ps1 -TestGroup knowledge
.\scripts\test-diagnostics.ps1 -TestGroup benchmark
.\scripts\test-diagnostics.ps1 -TestGroup flags
.\scripts\test-diagnostics.ps1 -TestGroup errors

# Linux/macOS
./scripts/test-diagnostics.sh --test-group help
./scripts/test-diagnostics.sh --test-group health
./scripts/test-diagnostics.sh --test-group config
./scripts/test-diagnostics.sh --test-group services
./scripts/test-diagnostics.sh --test-group knowledge
./scripts/test-diagnostics.sh --test-group benchmark
./scripts/test-diagnostics.sh --test-group flags
./scripts/test-diagnostics.sh --test-group errors
```

### Run with Verbose Output
```bash
# Windows
.\scripts\test-diagnostics.ps1 -Verbose

# Linux/macOS
./scripts/test-diagnostics.sh --verbose
```

## Manual Test Checklist

### 1. Help and Basic Functionality ✓
- [ ] `--help` displays all options
- [ ] `-h` works as short flag
- [ ] Unknown flags show error with exit code 5

### 2. Health Check Command ✓
- [ ] `--health-check` with all services running (exit code 0)
- [ ] `--health-check --verbose` shows details
- [ ] `--health-check` with Ollama down (exit code 3)
- [ ] `--health-check` with ScyllaDB down (exit code 3)

### 3. Configuration Validation ✓
- [ ] `--validate-config` with valid config (exit code 0)
- [ ] `--validate-config --verbose` shows values
- [ ] `--validate-config` with invalid config (exit code 2)

### 4. Individual Service Testing ✓
- [ ] `--test-ollama` with service running (exit code 0)
- [ ] `--test-ollama` with service down (exit code 3)
- [ ] `--test-scylla` with service running (exit code 0)
- [ ] `--test-scylla` with service down (exit code 3)
- [ ] `--test-embedding` with service running (exit code 0)
- [ ] `--test-embedding --verbose` shows embedding values
- [ ] `--test-similarity` with data (exit code 0)
- [ ] `--test-similarity --verbose` shows results
- [ ] `--test-similarity` with empty database (exit code 0, warning)

### 5. Knowledge Base Management ✓
- [ ] `--reinitialize-knowledge-base` (exit code 0)
- [ ] `--validate-knowledge-base` (exit code 0)
- [ ] `--validate-knowledge-base --verbose` shows examples

### 6. Benchmarking ✓
- [ ] `--benchmark-similarity` (exit code 0)
- [ ] `--benchmark-similarity --verbose` shows iterations
- [ ] `--benchmark-vector-operations` (exit code 0)
- [ ] `--benchmark-embedding` (exit code 0)
- [ ] `--benchmark-all` runs all benchmarks (exit code 0)

### 7. Flag Combinations ✓
- [ ] `--verbose` overrides config setting
- [ ] `-v` works as short flag
- [ ] `--no-similarity` with JSON processing works
- [ ] `--no-similarity` with `--test-scylla` shows error (exit code 5)
- [ ] Multiple diagnostic flags (only first executes)

### 8. Error Handling ✓
- [ ] File not found error (exit code 5)
- [ ] Missing file argument (exit code 5)
- [ ] Service unavailable (exit code 3)
- [ ] Configuration error (exit code 2)

### 9. Docker Environment ✓
- [ ] `docker-compose run --rm json-whisperer --health-check`
- [ ] `docker-compose run --rm json-whisperer --benchmark-all`
- [ ] `docker-compose run --rm json-whisperer --validate-config`

### 10. Backward Compatibility ✓
- [ ] Normal JSON processing: `echo '{"test": "data"}' | dotnet run --project JSON-Whisperer`
- [ ] File input: `dotnet run --project JSON-Whisperer -- --file test.json`
- [ ] Verbose flag: `echo '{"test": "data"}' | dotnet run --project JSON-Whisperer -- --verbose`

## Exit Code Reference

| Code | Constant | Meaning |
|------|----------|---------|
| 0 | Success | Operation completed successfully |
| 1 | GeneralError | Unexpected error occurred |
| 2 | ConfigurationError | Configuration validation failed |
| 3 | ServiceUnavailable | Required service is not available |
| 4 | ValidationError | Data validation failed |
| 5 | ArgumentError | Invalid command-line arguments |

## Common Test Commands

### Health Check
```bash
dotnet run --project JSON-Whisperer -- --health-check
dotnet run --project JSON-Whisperer -- --health-check --verbose
```

### Configuration
```bash
dotnet run --project JSON-Whisperer -- --validate-config
dotnet run --project JSON-Whisperer -- --validate-config --verbose
```

### Service Tests
```bash
dotnet run --project JSON-Whisperer -- --test-ollama
dotnet run --project JSON-Whisperer -- --test-scylla
dotnet run --project JSON-Whisperer -- --test-embedding
dotnet run --project JSON-Whisperer -- --test-similarity
```

### Knowledge Base
```bash
dotnet run --project JSON-Whisperer -- --validate-knowledge-base
dotnet run --project JSON-Whisperer -- --validate-knowledge-base --verbose
dotnet run --project JSON-Whisperer -- --reinitialize-knowledge-base
```

### Benchmarks
```bash
dotnet run --project JSON-Whisperer -- --benchmark-embedding
dotnet run --project JSON-Whisperer -- --benchmark-vector-operations
dotnet run --project JSON-Whisperer -- --benchmark-similarity
dotnet run --project JSON-Whisperer -- --benchmark-all
```

### Help
```bash
dotnet run --project JSON-Whisperer -- --help
dotnet run --project JSON-Whisperer -- -h
```

## Checking Exit Codes

### Windows (PowerShell)
```powershell
dotnet run --project JSON-Whisperer -- --health-check
echo $LASTEXITCODE
```

### Linux/macOS (Bash)
```bash
dotnet run --project JSON-Whisperer -- --health-check
echo $?
```

## Service Management

### Start Services
```bash
# Ollama
ollama serve

# ScyllaDB (Docker)
docker-compose up -d scylla
```

### Stop Services
```bash
# Ollama
# Stop the ollama process or service

# ScyllaDB (Docker)
docker-compose stop scylla
```

### Check Service Status
```bash
# Ollama
curl http://localhost:11434/api/tags

# ScyllaDB
docker ps | grep scylla
```

## Troubleshooting

### Tests Failing
1. Check services are running
2. Verify models are pulled
3. Check configuration in appsettings.json
4. Review error messages in test output

### Services Not Starting
1. Check Docker is running (for ScyllaDB)
2. Check Ollama installation
3. Verify port availability (11434 for Ollama, 9042 for ScyllaDB)

### Exit Codes Not Matching
1. Verify you're checking exit code immediately after command
2. Check for intermediate commands that might change exit code
3. Review error output for unexpected errors

## Notes

- Run tests from repository root directory
- Ensure all prerequisites are met before testing
- Some tests require services to be running
- Use `--skip-service-checks` flag to skip service availability checks
- Use `--verbose` flag for detailed test output
- Tests are designed to be idempotent (can be run multiple times)

## Reporting Issues

When reporting test failures, include:
1. Test name and number
2. Command executed
3. Expected vs actual output
4. Exit code received
5. Service status (Ollama, ScyllaDB)
6. Environment (OS, .NET version, Docker version)
7. Configuration settings (if relevant)
