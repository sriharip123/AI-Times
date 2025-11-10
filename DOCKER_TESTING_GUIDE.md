# Docker Environment Testing Guide

This guide provides specific instructions for testing JSON-Whisperer diagnostic commands in a Docker environment.

## Prerequisites

- Docker installed and running
- Docker Compose installed
- Application containerized (Dockerfile present)
- docker-compose.yml configured

## Docker Setup

### 1. Build the Docker Image

```bash
docker-compose build json-whisperer
```

### 2. Start Required Services

```bash
# Start all services
docker-compose up -d

# Or start specific services
docker-compose up -d scylla ollama
```

### 3. Verify Services are Running

```bash
docker-compose ps
```

Expected output should show:
- scylla (running)
- ollama (running)
- json-whisperer (may not be running, as it's a command-line tool)

## Running Diagnostic Commands in Docker

### Basic Command Structure

```bash
docker-compose run --rm json-whisperer [DIAGNOSTIC_COMMAND]
```

The `--rm` flag automatically removes the container after execution.

## Test Scenarios

### Test 1: Health Check in Docker

**Command:**
```bash
docker-compose run --rm json-whisperer --health-check
```

**Expected Behavior:**
- Container starts
- Connects to Ollama service via Docker network
- Connects to ScyllaDB via Docker network
- All services show as healthy
- Container exits with code 0
- Container is automatically removed

**Verification:**
```bash
# Check exit code (Linux/macOS)
docker-compose run --rm json-whisperer --health-check
echo $?

# Check exit code (Windows PowerShell)
docker-compose run --rm json-whisperer --health-check
echo $LASTEXITCODE
```

**Troubleshooting:**
- If Ollama is unreachable, check Docker network configuration
- If ScyllaDB is unreachable, ensure it's running: `docker-compose ps scylla`
- Check service URLs in appsettings.json or environment variables

### Test 2: Health Check with Verbose Mode

**Command:**
```bash
docker-compose run --rm json-whisperer --health-check --verbose
```

**Expected Output:**
- Detailed service information
- URLs used for connections (should be Docker service names)
- Response times
- Embedding counts
- Exit code: 0

**Verification Checklist:**
- [ ] Ollama URL shows Docker service name (e.g., `http://ollama:11434`)
- [ ] ScyllaDB connection uses Docker service name
- [ ] All services are reachable
- [ ] Response times are reasonable for containerized environment

### Test 3: Configuration Validation in Docker

**Command:**
```bash
docker-compose run --rm json-whisperer --validate-config
```

**Expected Behavior:**
- Configuration loaded from container
- Environment variables from docker-compose.yml applied
- All settings validated
- Exit code: 0

**Verification:**
```bash
# With verbose to see actual values
docker-compose run --rm json-whisperer --validate-config --verbose
```

**Check:**
- [ ] Ollama BaseUrl uses Docker service name
- [ ] ScyllaDB connection uses Docker service name
- [ ] Environment variables override appsettings.json values

### Test 4: Service Testing in Docker

#### Test Ollama
```bash
docker-compose run --rm json-whisperer --test-ollama
```

**Expected:**
- Connection to Ollama service via Docker network
- Model availability verified
- Exit code: 0

#### Test ScyllaDB
```bash
docker-compose run --rm json-whisperer --test-scylla
```

**Expected:**
- Connection to ScyllaDB via Docker network
- Keyspace verified
- Embedding count displayed
- Exit code: 0

#### Test Embedding
```bash
docker-compose run --rm json-whisperer --test-embedding
```

**Expected:**
- Embedding generation successful
- Dimensions displayed
- Exit code: 0

#### Test Similarity
```bash
docker-compose run --rm json-whisperer --test-similarity
```

**Expected:**
- Similarity search completes
- Results displayed (if data exists)
- Exit code: 0

### Test 5: Knowledge Base Operations in Docker

#### Reinitialize Knowledge Base
```bash
docker-compose run --rm json-whisperer --reinitialize-knowledge-base
```

**Expected:**
- Scans AppData directory in container
- Generates embeddings
- Stores in ScyllaDB
- Exit code: 0

**Note:** Ensure AppData directory is mounted as a volume in docker-compose.yml

#### Validate Knowledge Base
```bash
docker-compose run --rm json-whisperer --validate-knowledge-base
```

**Expected:**
- Lists examples found in container
- Compares with database
- Exit code: 0

### Test 6: Benchmarking in Docker

#### Benchmark Embedding
```bash
docker-compose run --rm json-whisperer --benchmark-embedding
```

**Expected:**
- 10 iterations complete
- Performance metrics displayed
- Exit code: 0

**Note:** Performance may be slower in containerized environment

#### Benchmark Vector Operations
```bash
docker-compose run --rm json-whisperer --benchmark-vector-operations
```

**Expected:**
- Storage operations benchmarked
- Metrics displayed
- Exit code: 0

#### Benchmark Similarity
```bash
docker-compose run --rm json-whisperer --benchmark-similarity
```

**Expected:**
- Similarity search benchmarked
- Requires data in database
- Exit code: 0

#### Benchmark All
```bash
docker-compose run --rm json-whisperer --benchmark-all
```

**Expected:**
- All benchmarks run sequentially
- Overall results displayed
- Exit code: 0

### Test 7: Normal JSON Processing in Docker

```bash
# Using stdin
echo '{"test": "data"}' | docker-compose run --rm -T json-whisperer

# Using file (if mounted)
docker-compose run --rm json-whisperer --file /app/test.json
```

**Note:** The `-T` flag disables pseudo-TTY allocation for piped input

### Test 8: Error Scenarios in Docker

#### Service Down Scenarios

**Stop Ollama:**
```bash
docker-compose stop ollama
docker-compose run --rm json-whisperer --health-check
# Expected: Exit code 3 (ServiceUnavailable)
docker-compose start ollama
```

**Stop ScyllaDB:**
```bash
docker-compose stop scylla
docker-compose run --rm json-whisperer --health-check
# Expected: Exit code 3 (ServiceUnavailable)
docker-compose start scylla
```

## Docker-Specific Considerations

### 1. Network Configuration

Ensure docker-compose.yml has proper network configuration:

```yaml
services:
  json-whisperer:
    networks:
      - app-network
  
  ollama:
    networks:
      - app-network
  
  scylla:
    networks:
      - app-network

networks:
  app-network:
    driver: bridge
```

### 2. Environment Variables

Configure service URLs in docker-compose.yml:

```yaml
services:
  json-whisperer:
    environment:
      - Ollama__BaseUrl=http://ollama:11434
      - ScyllaDb__ContactPoints__0=scylla
      - ScyllaDb__Port=9042
```

### 3. Volume Mounts

Ensure AppData is accessible:

```yaml
services:
  json-whisperer:
    volumes:
      - ./AppData:/app/AppData:ro
```

### 4. Resource Limits

Consider setting resource limits for benchmarking:

```yaml
services:
  json-whisperer:
    deploy:
      resources:
        limits:
          cpus: '2'
          memory: 2G
```

## Performance Expectations

### Containerized vs Native Performance

Expect some performance differences in Docker:

| Operation | Native | Docker | Notes |
|-----------|--------|--------|-------|
| Embedding Generation | Baseline | 5-10% slower | Network overhead |
| Vector Storage | Baseline | 10-15% slower | Network + I/O |
| Similarity Search | Baseline | 5-10% slower | Network overhead |
| Health Checks | Baseline | Similar | Minimal overhead |

### Benchmark Baseline Values

Establish baseline performance in your Docker environment:

```bash
# Run benchmarks multiple times
for i in {1..3}; do
  echo "Run $i:"
  docker-compose run --rm json-whisperer --benchmark-all
  echo ""
done
```

Record average values for:
- Embedding generation (ops/sec)
- Vector storage (ops/sec)
- Similarity search (ops/sec)

## Automated Docker Testing Script

Create a script to run all Docker tests:

**test-docker.sh:**
```bash
#!/bin/bash

echo "Starting Docker services..."
docker-compose up -d

echo "Waiting for services to be ready..."
sleep 10

echo "Running health check..."
docker-compose run --rm json-whisperer --health-check

echo "Running configuration validation..."
docker-compose run --rm json-whisperer --validate-config

echo "Running service tests..."
docker-compose run --rm json-whisperer --test-ollama
docker-compose run --rm json-whisperer --test-scylla
docker-compose run --rm json-whisperer --test-embedding

echo "Running benchmarks..."
docker-compose run --rm json-whisperer --benchmark-all

echo "Cleaning up..."
docker-compose down

echo "Docker tests complete!"
```

**test-docker.ps1:**
```powershell
Write-Host "Starting Docker services..."
docker-compose up -d

Write-Host "Waiting for services to be ready..."
Start-Sleep -Seconds 10

Write-Host "Running health check..."
docker-compose run --rm json-whisperer --health-check

Write-Host "Running configuration validation..."
docker-compose run --rm json-whisperer --validate-config

Write-Host "Running service tests..."
docker-compose run --rm json-whisperer --test-ollama
docker-compose run --rm json-whisperer --test-scylla
docker-compose run --rm json-whisperer --test-embedding

Write-Host "Running benchmarks..."
docker-compose run --rm json-whisperer --benchmark-all

Write-Host "Cleaning up..."
docker-compose down

Write-Host "Docker tests complete!"
```

## CI/CD Integration

### Example GitHub Actions Workflow

```yaml
name: Docker Tests

on: [push, pull_request]

jobs:
  docker-tests:
    runs-on: ubuntu-latest
    
    steps:
      - uses: actions/checkout@v2
      
      - name: Build Docker images
        run: docker-compose build
      
      - name: Start services
        run: docker-compose up -d
      
      - name: Wait for services
        run: sleep 30
      
      - name: Run health check
        run: docker-compose run --rm json-whisperer --health-check
      
      - name: Run configuration validation
        run: docker-compose run --rm json-whisperer --validate-config
      
      - name: Run service tests
        run: |
          docker-compose run --rm json-whisperer --test-ollama
          docker-compose run --rm json-whisperer --test-scylla
          docker-compose run --rm json-whisperer --test-embedding
      
      - name: Run benchmarks
        run: docker-compose run --rm json-whisperer --benchmark-all
      
      - name: Cleanup
        run: docker-compose down
```

## Troubleshooting Docker Issues

### Issue: Services Not Reachable

**Symptoms:**
- Health check fails
- Connection refused errors

**Solutions:**
1. Check Docker network: `docker network ls`
2. Inspect network: `docker network inspect [network-name]`
3. Verify service names in configuration match docker-compose.yml
4. Check if services are running: `docker-compose ps`

### Issue: Slow Performance

**Symptoms:**
- Benchmarks much slower than expected
- Timeouts

**Solutions:**
1. Increase Docker resource limits
2. Check Docker Desktop settings (CPU, Memory)
3. Verify no other containers consuming resources
4. Check host system resources

### Issue: Volume Mount Issues

**Symptoms:**
- Knowledge base validation finds no examples
- File not found errors

**Solutions:**
1. Verify volume mounts in docker-compose.yml
2. Check file permissions
3. Ensure paths are correct (absolute vs relative)
4. Test with: `docker-compose run --rm json-whisperer ls -la /app/AppData`

### Issue: Environment Variables Not Applied

**Symptoms:**
- Configuration uses wrong URLs
- Services not found

**Solutions:**
1. Check environment section in docker-compose.yml
2. Verify syntax (use `__` for nested properties)
3. Test with: `docker-compose run --rm json-whisperer --validate-config --verbose`
4. Check for .env file conflicts

## Docker Testing Checklist

- [ ] Docker and Docker Compose installed
- [ ] Images built successfully
- [ ] All services start without errors
- [ ] Health check passes in Docker
- [ ] Configuration validation passes
- [ ] All service tests pass
- [ ] Knowledge base operations work
- [ ] Benchmarks complete successfully
- [ ] Error scenarios handled correctly
- [ ] Performance is acceptable
- [ ] Cleanup works (containers removed)

## Best Practices

1. **Always use `--rm` flag** to avoid container buildup
2. **Wait for services** to be fully ready before testing
3. **Use Docker networks** for service communication
4. **Mount volumes read-only** when possible for security
5. **Set resource limits** to prevent resource exhaustion
6. **Use environment variables** for configuration
7. **Test cleanup** to ensure no orphaned containers
8. **Document baseline performance** for your environment
9. **Automate tests** in CI/CD pipeline
10. **Monitor logs** during testing: `docker-compose logs -f`

## Additional Resources

- Docker Compose documentation: https://docs.docker.com/compose/
- Docker networking: https://docs.docker.com/network/
- Docker volumes: https://docs.docker.com/storage/volumes/
- Docker resource constraints: https://docs.docker.com/config/containers/resource_constraints/
