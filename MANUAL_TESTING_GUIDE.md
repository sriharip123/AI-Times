# Manual Testing Guide for Command-Line Diagnostics

This guide provides comprehensive instructions for manually testing all diagnostic commands in JSON-Whisperer. Use this guide to validate that all features work correctly in your environment.

## Prerequisites

Before starting manual testing, ensure you have:

- [ ] .NET 8.0 SDK installed
- [ ] Ollama installed and running
- [ ] ScyllaDB running (via Docker or standalone)
- [ ] Mistral model pulled: `ollama pull mistral`
- [ ] Nomic-embed-text model pulled: `ollama pull nomic-embed-text`
- [ ] Application built: `dotnet build`
- [ ] Published application available in `publish/` directory

## Quick Start

Run the automated test script to execute all tests:

**Windows (PowerShell):**
```powershell
.\scripts\test-diagnostics.ps1
```

**Linux/macOS:**
```bash
./scripts/test-diagnostics.sh
```

## Manual Test Scenarios

### Test Group 1: Help and Basic Functionality

#### Test 1.1: Help Display
**Command:**
```bash
dotnet run --project JSON-Whisperer -- --help
```

**Expected Output:**
- Displays all command-line options
- Options grouped by category (Input, Diagnostic, Testing, Benchmark)
- Each option has a description
- Usage examples are shown
- Exit code: 0

**Verification Checklist:**
- [ ] Help text is displayed
- [ ] All diagnostic commands are listed
- [ ] Short flags (-h, -v, -f) are documented
- [ ] Examples are clear and accurate
- [ ] Exit code is 0

#### Test 1.2: Short Help Flag
**Command:**
```bash
dotnet run --project JSON-Whisperer -- -h
```

**Expected Output:**
- Same as `--help`
- Exit code: 0

**Verification Checklist:**
- [ ] Output matches `--help`
- [ ] Exit code is 0

#### Test 1.3: Unknown Flag Error
**Command:**
```bash
dotnet run --project JSON-Whisperer -- --unknown-flag
```

**Expected Output:**
- Error message: "Unknown flag: --unknown-flag"
- Suggestion to run --help
- Exit code: 5 (ArgumentError)

**Verification Checklist:**
- [ ] Error message is clear
- [ ] Help suggestion is provided
- [ ] Exit code is 5

---

### Test Group 2: Health Check Command

#### Test 2.1: Health Check with All Services Running
**Prerequisites:**
- Ollama running
- ScyllaDB running
- Models pulled

**Command:**
```bash
dotnet run --project JSON-Whisperer -- --health-check
```

**Expected Output:**
- All services show ✓ HEALTHY
- Response times displayed
- Overall status: HEALTHY
- Exit code: 0

**Verification Checklist:**
- [ ] Ollama service: HEALTHY
- [ ] ScyllaDB: HEALTHY
- [ ] Embedding service: HEALTHY
- [ ] Knowledge Base: HEALTHY or WARNING (if no examples)
- [ ] Exit code is 0

#### Test 2.2: Health Check with Verbose Mode
**Command:**
```bash
dotnet run --project JSON-Whisperer -- --health-check --verbose
```

**Expected Output:**
- Same as Test 2.1 plus:
- Ollama URL and model name
- Embedding count in database
- Number of examples loaded
- Exit code: 0

**Verification Checklist:**
- [ ] Additional details displayed
- [ ] Configuration values shown
- [ ] Exit code is 0

#### Test 2.3: Health Check with Ollama Down
**Prerequisites:**
- Stop Ollama: `ollama stop` or stop the service

**Command:**
```bash
dotnet run --project JSON-Whisperer -- --health-check
```

**Expected Output:**
- Ollama service: ✗ UNAVAILABLE or ✗ ERROR
- Other services may also fail
- Overall status: UNHEALTHY
- Exit code: 3 (ServiceUnavailable)

**Verification Checklist:**
- [ ] Ollama shows as unavailable
- [ ] Error is clearly indicated
- [ ] Exit code is 3

**Cleanup:**
- Restart Ollama

#### Test 2.4: Health Check with ScyllaDB Down
**Prerequisites:**
- Stop ScyllaDB: `docker-compose stop scylla` or stop the service

**Command:**
```bash
dotnet run --project JSON-Whisperer -- --health-check
```

**Expected Output:**
- ScyllaDB: ✗ UNAVAILABLE or ✗ ERROR
- Overall status: UNHEALTHY
- Exit code: 3 (ServiceUnavailable)

**Verification Checklist:**
- [ ] ScyllaDB shows as unavailable
- [ ] Error is clearly indicated
- [ ] Exit code is 3

**Cleanup:**
- Restart ScyllaDB: `docker-compose start scylla`

---

### Test Group 3: Configuration Validation

#### Test 3.1: Validate Valid Configuration
**Command:**
```bash
dotnet run --project JSON-Whisperer -- --validate-config
```

**Expected Output:**
- All configuration sections validated
- Success message: "✓ Configuration is valid"
- Exit code: 0

**Verification Checklist:**
- [ ] Ollama configuration validated
- [ ] Application configuration validated
- [ ] No errors reported
- [ ] Exit code is 0

#### Test 3.2: Validate Configuration with Verbose Mode
**Command:**
```bash
dotnet run --project JSON-Whisperer -- --validate-config --verbose
```

**Expected Output:**
- Same as Test 3.1 plus:
- Individual configuration values displayed
- Each validated setting shown with ✓
- Exit code: 0

**Verification Checklist:**
- [ ] Configuration values displayed
- [ ] Each setting marked as valid
- [ ] Exit code is 0

#### Test 3.3: Validate Invalid Configuration
**Prerequisites:**
- Temporarily modify `appsettings.json` to have invalid values:
  - Set `Ollama.BaseUrl` to an invalid URL (e.g., "not-a-url")
  - Or set `Ollama.TimeoutSeconds` to -1

**Command:**
```bash
dotnet run --project JSON-Whisperer -- --validate-config
```

**Expected Output:**
- Validation errors listed
- Specific error messages for each invalid setting
- Exit code: 2 (ConfigurationError)

**Verification Checklist:**
- [ ] Errors are clearly listed
- [ ] Each error is specific and actionable
- [ ] Exit code is 2

**Cleanup:**
- Restore `appsettings.json` to valid values

---

### Test Group 4: Individual Service Testing

#### Test 4.1: Test Ollama Service
**Prerequisites:**
- Ollama running
- Mistral model pulled

**Command:**
```bash
dotnet run --project JSON-Whisperer -- --test-ollama
```

**Expected Output:**
- Connection URL displayed
- Model name displayed
- Success message: "✓ Ollama service is available"
- Model ready message
- Exit code: 0

**Verification Checklist:**
- [ ] Service is available
- [ ] Model is loaded and ready
- [ ] Response time displayed
- [ ] Exit code is 0

#### Test 4.2: Test Ollama with Service Down
**Prerequisites:**
- Stop Ollama

**Command:**
```bash
dotnet run --project JSON-Whisperer -- --test-ollama
```

**Expected Output:**
- Error message: "✗ Ollama service is not available"
- Troubleshooting suggestions displayed
- Exit code: 3 (ServiceUnavailable)

**Verification Checklist:**
- [ ] Error is clear
- [ ] Troubleshooting steps provided
- [ ] Exit code is 3

**Cleanup:**
- Restart Ollama

#### Test 4.3: Test ScyllaDB
**Prerequisites:**
- ScyllaDB running

**Command:**
```bash
dotnet run --project JSON-Whisperer -- --test-scylla
```

**Expected Output:**
- Connection message
- Success: "✓ ScyllaDB is connected"
- Embedding count displayed
- Exit code: 0

**Verification Checklist:**
- [ ] Connection successful
- [ ] Embedding count shown
- [ ] Response time displayed
- [ ] Exit code is 0

#### Test 4.4: Test ScyllaDB with Service Down
**Prerequisites:**
- Stop ScyllaDB

**Command:**
```bash
dotnet run --project JSON-Whisperer -- --test-scylla
```

**Expected Output:**
- Error message: "✗ ScyllaDB is not connected"
- Troubleshooting suggestions
- Exit code: 3 (ServiceUnavailable)

**Verification Checklist:**
- [ ] Error is clear
- [ ] Troubleshooting steps provided
- [ ] Exit code is 3

**Cleanup:**
- Restart ScyllaDB

#### Test 4.5: Test Embedding Service
**Prerequisites:**
- Ollama running
- nomic-embed-text model pulled

**Command:**
```bash
dotnet run --project JSON-Whisperer -- --test-embedding
```

**Expected Output:**
- Model name displayed
- Service available message
- Test embedding generated
- Embedding dimensions shown
- Exit code: 0

**Verification Checklist:**
- [ ] Service is available
- [ ] Test embedding generated successfully
- [ ] Dimensions are correct (typically 768 or 384)
- [ ] Response time displayed
- [ ] Exit code is 0

#### Test 4.6: Test Embedding with Verbose Mode
**Command:**
```bash
dotnet run --project JSON-Whisperer -- --test-embedding --verbose
```

**Expected Output:**
- Same as Test 4.5 plus:
- First 5 embedding values displayed
- Exit code: 0

**Verification Checklist:**
- [ ] Embedding values shown
- [ ] Values are floating-point numbers
- [ ] Exit code is 0

#### Test 4.7: Test Similarity Search
**Prerequisites:**
- Ollama running
- ScyllaDB running
- Knowledge base initialized (run `--reinitialize-knowledge-base` first if needed)

**Command:**
```bash
dotnet run --project JSON-Whisperer -- --test-similarity
```

**Expected Output:**
- Embedding count displayed
- Test query embedding generated
- Similarity search completed
- Number of results shown
- Exit code: 0

**Verification Checklist:**
- [ ] Search completes successfully
- [ ] Results count displayed
- [ ] Response time shown
- [ ] Exit code is 0

#### Test 4.8: Test Similarity with Verbose Mode
**Command:**
```bash
dotnet run --project JSON-Whisperer -- --test-similarity --verbose
```

**Expected Output:**
- Same as Test 4.7 plus:
- Top results with similarity scores
- Descriptions of matched examples
- Exit code: 0

**Verification Checklist:**
- [ ] Top results displayed
- [ ] Similarity scores shown (0.0 to 1.0)
- [ ] Descriptions are readable
- [ ] Exit code is 0

#### Test 4.9: Test Similarity with Empty Database
**Prerequisites:**
- Empty database (or run `--reinitialize-knowledge-base` with no examples)

**Command:**
```bash
dotnet run --project JSON-Whisperer -- --test-similarity
```

**Expected Output:**
- Warning: "⚠ No embeddings in database to search"
- Suggestion to run --reinitialize-knowledge-base
- Exit code: 0 (not an error)

**Verification Checklist:**
- [ ] Warning message displayed
- [ ] Helpful suggestion provided
- [ ] Exit code is 0

---

### Test Group 5: Knowledge Base Management

#### Test 5.1: Reinitialize Knowledge Base
**Prerequisites:**
- Ollama running
- ScyllaDB running
- Example JSON files in AppData/examples directory

**Command:**
```bash
dotnet run --project JSON-Whisperer -- --reinitialize-knowledge-base
```

**Expected Output:**
- Warning about clearing existing embeddings
- Initialization progress
- Success message
- Total embeddings count
- Exit code: 0

**Verification Checklist:**
- [ ] Warning displayed
- [ ] Initialization completes
- [ ] Embedding count shown
- [ ] Exit code is 0

#### Test 5.2: Validate Knowledge Base
**Prerequisites:**
- Example JSON files in AppData/examples directory

**Command:**
```bash
dotnet run --project JSON-Whisperer -- --validate-knowledge-base
```

**Expected Output:**
- Number of examples found
- Database embedding count
- Success message
- Exit code: 0

**Verification Checklist:**
- [ ] Example count displayed
- [ ] Database count displayed
- [ ] Counts match (or warning if they don't)
- [ ] Exit code is 0

#### Test 5.3: Validate Knowledge Base with Verbose Mode
**Command:**
```bash
dotnet run --project JSON-Whisperer -- --validate-knowledge-base --verbose
```

**Expected Output:**
- Same as Test 5.2 plus:
- List of all examples with IDs and descriptions
- Exit code: 0

**Verification Checklist:**
- [ ] All examples listed
- [ ] IDs and descriptions shown
- [ ] Exit code is 0

---

### Test Group 6: Benchmarking

#### Test 6.1: Benchmark Similarity Search
**Prerequisites:**
- All services running
- Knowledge base initialized with data

**Command:**
```bash
dotnet run --project JSON-Whisperer -- --benchmark-similarity
```

**Expected Output:**
- Benchmark progress
- Statistics: iterations, average, min, max, throughput
- Success message
- Exit code: 0

**Verification Checklist:**
- [ ] Benchmark completes
- [ ] Statistics displayed
- [ ] Throughput (ops/sec) shown
- [ ] Exit code is 0

#### Test 6.2: Benchmark Similarity with Verbose Mode
**Command:**
```bash
dotnet run --project JSON-Whisperer -- --benchmark-similarity --verbose
```

**Expected Output:**
- Same as Test 6.1 plus:
- Individual iteration times displayed
- Exit code: 0

**Verification Checklist:**
- [ ] Each iteration time shown
- [ ] Times are reasonable
- [ ] Exit code is 0

#### Test 6.3: Benchmark Vector Operations
**Prerequisites:**
- ScyllaDB running

**Command:**
```bash
dotnet run --project JSON-Whisperer -- --benchmark-vector-operations
```

**Expected Output:**
- Benchmark progress
- Statistics for storage operations
- Success message
- Exit code: 0

**Verification Checklist:**
- [ ] Benchmark completes
- [ ] Statistics displayed
- [ ] Throughput shown
- [ ] Exit code is 0

#### Test 6.4: Benchmark Embedding Generation
**Prerequisites:**
- Ollama running
- Embedding model available

**Command:**
```bash
dotnet run --project JSON-Whisperer -- --benchmark-embedding
```

**Expected Output:**
- Model name displayed
- Benchmark progress
- Statistics for embedding generation
- Success message
- Exit code: 0

**Verification Checklist:**
- [ ] Benchmark completes
- [ ] Statistics displayed
- [ ] Throughput shown
- [ ] Exit code is 0

#### Test 6.5: Benchmark All
**Prerequisites:**
- All services running
- Knowledge base initialized

**Command:**
```bash
dotnet run --project JSON-Whisperer -- --benchmark-all
```

**Expected Output:**
- All three benchmarks run sequentially
- Statistics for each benchmark
- Overall success message
- Exit code: 0

**Verification Checklist:**
- [ ] Similarity benchmark runs
- [ ] Vector operations benchmark runs
- [ ] Embedding benchmark runs
- [ ] Overall status displayed
- [ ] Exit code is 0

---

### Test Group 7: Flag Combinations and Overrides

#### Test 7.1: Verbose Mode Override
**Prerequisites:**
- Ensure `appsettings.json` has `VerboseMode: false`

**Command:**
```bash
dotnet run --project JSON-Whisperer -- --health-check --verbose
```

**Expected Output:**
- Verbose output displayed despite configuration setting
- Additional details shown
- Exit code: 0

**Verification Checklist:**
- [ ] Verbose output shown
- [ ] Configuration overridden
- [ ] Exit code is 0

#### Test 7.2: Short Verbose Flag
**Command:**
```bash
dotnet run --project JSON-Whisperer -- --health-check -v
```

**Expected Output:**
- Same as Test 7.1
- Exit code: 0

**Verification Checklist:**
- [ ] Verbose output shown
- [ ] Exit code is 0

#### Test 7.3: No-Similarity Flag with Normal Processing
**Command:**
```bash
echo '{"test": "data"}' | dotnet run --project JSON-Whisperer -- --no-similarity
```

**Expected Output:**
- JSON processed without similarity matching
- No ScyllaDB connection attempted
- Faster execution
- Exit code: 0

**Verification Checklist:**
- [ ] Processing completes
- [ ] No similarity context in output
- [ ] No ScyllaDB errors even if ScyllaDB is down
- [ ] Exit code is 0

#### Test 7.4: Conflicting Flags (No-Similarity with Test-Scylla)
**Command:**
```bash
dotnet run --project JSON-Whisperer -- --test-scylla --no-similarity
```

**Expected Output:**
- Error message about conflicting flags
- Explanation that --test-scylla requires similarity services
- Exit code: 5 (ArgumentError)

**Verification Checklist:**
- [ ] Error message is clear
- [ ] Conflict explained
- [ ] Exit code is 5

#### Test 7.5: Multiple Diagnostic Flags
**Command:**
```bash
dotnet run --project JSON-Whisperer -- --health-check --test-ollama
```

**Expected Output:**
- Only first diagnostic command executes (--health-check)
- Second flag ignored
- Exit code: 0

**Verification Checklist:**
- [ ] Only health check runs
- [ ] No error about multiple flags
- [ ] Exit code is 0

---

### Test Group 8: Error Messages and Exit Codes

#### Test 8.1: File Not Found Error
**Command:**
```bash
dotnet run --project JSON-Whisperer -- --file nonexistent.json
```

**Expected Output:**
- Error: "File not found: nonexistent.json"
- Exit code: 5 (ArgumentError)

**Verification Checklist:**
- [ ] Error message is clear
- [ ] File path shown in error
- [ ] Exit code is 5

#### Test 8.2: Missing File Argument
**Command:**
```bash
dotnet run --project JSON-Whisperer -- --file
```

**Expected Output:**
- Error: "Flag '--file' requires a file path argument"
- Exit code: 5 (ArgumentError)

**Verification Checklist:**
- [ ] Error message is clear
- [ ] Exit code is 5

#### Test 8.3: Service Unavailable Exit Code
**Prerequisites:**
- Stop Ollama

**Command:**
```bash
dotnet run --project JSON-Whisperer -- --test-ollama
```

**Expected Output:**
- Service unavailable error
- Exit code: 3 (ServiceUnavailable)

**Verification Checklist:**
- [ ] Error displayed
- [ ] Exit code is 3

**Cleanup:**
- Restart Ollama

---

### Test Group 9: Docker Environment Testing

#### Test 9.1: Health Check in Docker
**Prerequisites:**
- Docker and docker-compose installed
- Application containerized

**Command:**
```bash
docker-compose run --rm json-whisperer --health-check
```

**Expected Output:**
- Health check runs inside container
- Services accessible from container
- Exit code: 0

**Verification Checklist:**
- [ ] Container starts
- [ ] Health check completes
- [ ] Services are reachable
- [ ] Exit code is 0

#### Test 9.2: Benchmark in Docker
**Command:**
```bash
docker-compose run --rm json-whisperer --benchmark-all
```

**Expected Output:**
- All benchmarks run
- Performance metrics displayed
- Exit code: 0

**Verification Checklist:**
- [ ] Benchmarks complete
- [ ] Performance is reasonable for containerized environment
- [ ] Exit code is 0

#### Test 9.3: Configuration Validation in Docker
**Command:**
```bash
docker-compose run --rm json-whisperer --validate-config
```

**Expected Output:**
- Configuration validated
- Container environment variables applied
- Exit code: 0

**Verification Checklist:**
- [ ] Validation completes
- [ ] Environment variables recognized
- [ ] Exit code is 0

---

### Test Group 10: Backward Compatibility

#### Test 10.1: Normal JSON Processing Still Works
**Command:**
```bash
echo '{"test": "data"}' | dotnet run --project JSON-Whisperer
```

**Expected Output:**
- JSON processed normally
- Summary generated
- Exit code: 0

**Verification Checklist:**
- [ ] Processing works as before
- [ ] No diagnostic commands interfere
- [ ] Exit code is 0

#### Test 10.2: File Input Still Works
**Prerequisites:**
- Create test file: `echo '{"test": "data"}' > test.json`

**Command:**
```bash
dotnet run --project JSON-Whisperer -- --file test.json
```

**Expected Output:**
- File processed normally
- Summary generated
- Exit code: 0

**Verification Checklist:**
- [ ] File input works
- [ ] Processing completes
- [ ] Exit code is 0

**Cleanup:**
- Remove test file: `rm test.json`

#### Test 10.3: Existing Verbose Flag Still Works
**Command:**
```bash
echo '{"test": "data"}' | dotnet run --project JSON-Whisperer -- --verbose
```

**Expected Output:**
- Verbose output during processing
- Additional details shown
- Exit code: 0

**Verification Checklist:**
- [ ] Verbose mode works
- [ ] Extra details displayed
- [ ] Exit code is 0

---

## Exit Code Reference

| Exit Code | Constant | Meaning |
|-----------|----------|---------|
| 0 | Success | Operation completed successfully |
| 1 | GeneralError | Unexpected error occurred |
| 2 | ConfigurationError | Configuration validation failed |
| 3 | ServiceUnavailable | Required service is not available |
| 4 | ValidationError | Data validation failed |
| 5 | ArgumentError | Invalid command-line arguments |

## Testing Checklist Summary

Use this checklist to track your testing progress:

### Core Functionality
- [ ] Help display (--help, -h)
- [ ] Unknown flag handling
- [ ] Exit codes are correct

### Health Checks
- [ ] All services healthy
- [ ] Ollama down scenario
- [ ] ScyllaDB down scenario
- [ ] Verbose mode

### Configuration
- [ ] Valid configuration
- [ ] Invalid configuration
- [ ] Verbose mode

### Service Testing
- [ ] Test Ollama (up and down)
- [ ] Test ScyllaDB (up and down)
- [ ] Test Embedding (up and down)
- [ ] Test Similarity (with and without data)

### Knowledge Base
- [ ] Reinitialize knowledge base
- [ ] Validate knowledge base
- [ ] Verbose mode

### Benchmarking
- [ ] Benchmark similarity
- [ ] Benchmark vector operations
- [ ] Benchmark embedding
- [ ] Benchmark all
- [ ] Verbose mode

### Flags and Combinations
- [ ] Verbose override (--verbose, -v)
- [ ] No-similarity flag
- [ ] Conflicting flags
- [ ] Multiple diagnostic flags

### Error Handling
- [ ] File not found
- [ ] Missing arguments
- [ ] Service unavailable
- [ ] Configuration errors

### Docker Environment
- [ ] Health check in Docker
- [ ] Benchmarks in Docker
- [ ] Configuration in Docker

### Backward Compatibility
- [ ] Normal JSON processing
- [ ] File input
- [ ] Existing verbose flag

## Troubleshooting

### Common Issues

**Issue: Ollama not responding**
- Check if Ollama is running: `ollama list`
- Verify the URL in appsettings.json
- Check firewall settings

**Issue: ScyllaDB connection fails**
- Verify ScyllaDB is running: `docker ps | grep scylla`
- Check connection settings
- Verify network connectivity

**Issue: Models not found**
- Pull required models:
  - `ollama pull mistral`
  - `ollama pull nomic-embed-text`

**Issue: Exit codes not as expected**
- Check PowerShell: `$LASTEXITCODE`
- Check Bash: `echo $?`
- Verify error messages match expected output

## Reporting Issues

When reporting issues found during testing, include:

1. Test number and name
2. Command executed
3. Expected output
4. Actual output
5. Exit code received
6. Environment details (OS, .NET version, Docker version if applicable)
7. Service status (Ollama, ScyllaDB)
8. Configuration settings (if relevant)

## Next Steps

After completing manual testing:

1. Document any issues found
2. Verify all exit codes are correct
3. Confirm error messages are helpful
4. Test in production-like environment
5. Update documentation based on findings
