# JSON-Whisperer Troubleshooting Guide

This guide provides detailed troubleshooting steps for common issues with JSON-Whisperer, with special focus on vector similarity and ScyllaDB-related problems.

## Table of Contents

- [Quick Diagnostics](#quick-diagnostics)
- [Vector Similarity Issues](#vector-similarity-issues)
- [ScyllaDB Database Issues](#scylladb-database-issues)
- [Ollama and Embedding Issues](#ollama-and-embedding-issues)
- [Knowledge Base Issues](#knowledge-base-issues)
- [Performance Problems](#performance-problems)
- [Configuration Issues](#configuration-issues)
- [Network and Connectivity](#network-and-connectivity)
- [Memory and Resource Issues](#memory-and-resource-issues)
- [Logging and Monitoring](#logging-and-monitoring)

## Quick Diagnostics

### System Health Check
```bash
# Run comprehensive health check (checks all services)
dotnet JSON-Whisperer.dll --health-check

# Check individual components
dotnet JSON-Whisperer.dll --test-ollama      # Test Ollama service
dotnet JSON-Whisperer.dll --test-scylla      # Test ScyllaDB
dotnet JSON-Whisperer.dll --test-embedding   # Test embedding generation
dotnet JSON-Whisperer.dll --test-similarity  # Test similarity search

# Validate configuration
dotnet JSON-Whisperer.dll --validate-config

# Validate knowledge base
dotnet JSON-Whisperer.dll --validate-knowledge-base
```

**Health Check Exit Codes:**
- Exit code 0: All services healthy
- Exit code 1: One or more services unhealthy

**Example Usage in Scripts:**
```bash
# Check health before running application
if ! dotnet JSON-Whisperer.dll --health-check; then
  echo "ERROR: Health check failed. Please check service status."
  exit 1
fi

# Validate configuration before deployment
if ! dotnet JSON-Whisperer.dll --validate-config; then
  echo "ERROR: Configuration validation failed."
  exit 1
fi
```

### Component Status Overview
```bash
# Check all services status
docker ps | grep -E "(ollama|scylla|json-whisperer)"

# Test basic functionality
echo '{"test": "data"}' | dotnet JSON-Whisperer.dll --verbose

# Run all diagnostic commands
dotnet JSON-Whisperer.dll --health-check
dotnet JSON-Whisperer.dll --validate-config
dotnet JSON-Whisperer.dll --validate-knowledge-base
```

## Vector Similarity Issues

### Problem: No Similar Results Found

**Symptoms:**
- Application runs but shows "No similar examples found"
- Similarity matching appears disabled
- Empty similarity results in verbose mode

**Diagnosis:**
```bash
# Check if similarity matching is enabled
grep -i "EnableSimilarityMatching" appsettings.json

# Verify knowledge base initialization
dotnet JSON-Whisperer.dll --validate-knowledge-base

# Check similarity threshold
grep -i "SimilarityThreshold" appsettings.json
```

**Solutions:**
1. **Enable similarity matching:**
   ```json
   {
     "Vector": {
       "EnableSimilarityMatching": true
     }
   }
   ```

2. **Lower similarity threshold for testing:**
   ```json
   {
     "Vector": {
       "SimilarityThreshold": 0.3
     }
   }
   ```

3. **Reinitialize knowledge base:**
   ```bash
   dotnet JSON-Whisperer.dll --reinitialize-knowledge-base
   ```

### Problem: Similarity Search Timeout

**Symptoms:**
- Application hangs during similarity search
- Timeout errors in logs
- Slow response times

**Diagnosis:**
```bash
# Check ScyllaDB query performance
docker exec -it scylla-container cqlsh -e "SELECT COUNT(*) FROM json_whisperer.embeddings;"

# Monitor ScyllaDB performance
docker exec -it scylla-container nodetool cfstats json_whisperer
```

**Solutions:**
1. **Increase query timeout:**
   ```json
   {
     "ScyllaDb": {
       "QueryTimeoutSeconds": 60
     }
   }
   ```

2. **Optimize ScyllaDB configuration:**
   ```json
   {
     "ScyllaDb": {
       "MaxConnectionsPerHost": 16,
       "MaxRequestsPerConnection": 32768,
       "EnableCompression": true
     }
   }
   ```

3. **Reduce result set size:**
   ```json
   {
     "Vector": {
       "MaxSimilarResults": 3
     }
   }
   ```

### Problem: Incorrect Similarity Scores

**Symptoms:**
- All similarity scores are very low or very high
- Unexpected similarity matches
- Inconsistent results

**Diagnosis:**
```bash
# Test embedding generation consistency
dotnet JSON-Whisperer.dll --test-embedding-consistency

# Check vector normalization
grep -i "VectorNormalization" appsettings.json
```

**Solutions:**
1. **Verify vector normalization:**
   ```json
   {
     "Vector": {
       "VectorNormalization": "L2"
     }
   }
   ```

2. **Adjust similarity calculation:**
   ```json
   {
     "Vector": {
       "MinSimilarityScore": 0.1,
       "SimilarityThreshold": 0.7
     }
   }
   ```

3. **Regenerate embeddings:**
   ```bash
   # Clear existing embeddings and regenerate
   docker exec -it scylla-container cqlsh -e "TRUNCATE json_whisperer.embeddings;"
   dotnet JSON-Whisperer.dll --reinitialize-knowledge-base
   ```

## ScyllaDB Database Issues

### Problem: Connection Failed

**Symptoms:**
- "No hosts available" error
- Connection timeout errors
- Authentication failures

**Diagnosis:**
```bash
# Test basic connectivity
telnet scylla-host 9042

# Check ScyllaDB status
docker exec -it scylla-container nodetool status

# Test authentication
docker exec -it scylla-container cqlsh -u username -p password
```

**Solutions:**
1. **Verify connection settings:**
   ```json
   {
     "ScyllaDb": {
       "ContactPoints": "127.0.0.1",
       "Port": 9042,
       "ConnectionTimeoutSeconds": 30
     }
   }
   ```

2. **Check network connectivity:**
   ```bash
   # Test from application container
   docker exec -it json-whisperer-container telnet scylla-host 9042
   
   # Check Docker network
   docker network ls
   docker network inspect bridge
   ```

3. **Verify credentials:**
   ```bash
   # Test authentication
   export SCYLLADB_USERNAME="your_username"
   export SCYLLADB_PASSWORD="your_password"
   ```

### Problem: Keyspace Not Found

**Symptoms:**
- "Keyspace 'json_whisperer' does not exist" error
- Schema-related errors
- Table not found errors

**Diagnosis:**
```bash
# Check existing keyspaces
docker exec -it scylla-container cqlsh -e "DESCRIBE KEYSPACES;"

# Check keyspace configuration
grep -i "CreateKeyspaceIfNotExists" appsettings.json
```

**Solutions:**
1. **Enable automatic keyspace creation:**
   ```json
   {
     "ScyllaDb": {
       "CreateKeyspaceIfNotExists": true,
       "ReplicationFactor": 1
     }
   }
   ```

2. **Create keyspace manually:**
   ```sql
   CREATE KEYSPACE IF NOT EXISTS json_whisperer 
   WITH REPLICATION = {
     'class': 'SimpleStrategy',
     'replication_factor': 1
   };
   ```

3. **Verify table schema:**
   ```sql
   USE json_whisperer;
   DESCRIBE TABLES;
   DESCRIBE TABLE embeddings;
   ```

### Problem: Performance Issues

**Symptoms:**
- Slow query execution
- High CPU usage on ScyllaDB nodes
- Memory pressure warnings

**Diagnosis:**
```bash
# Check ScyllaDB metrics
docker exec -it scylla-container nodetool cfstats
docker exec -it scylla-container nodetool tpstats

# Monitor resource usage
docker stats scylla-container
```

**Solutions:**
1. **Optimize connection pooling:**
   ```json
   {
     "ScyllaDb": {
       "PoolingOptions": {
         "CoreConnectionsPerHost": 4,
         "MaxConnectionsPerHost": 16,
         "MaxRequestsPerConnection": 32768
       }
     }
   }
   ```

2. **Enable compression:**
   ```json
   {
     "ScyllaDb": {
       "EnableCompression": true,
       "CompressionType": "LZ4"
     }
   }
   ```

3. **Tune consistency level:**
   ```json
   {
     "ScyllaDb": {
       "ConsistencyLevel": "LocalOne"
     }
   }
   ```

## Ollama and Embedding Issues

### Problem: Embedding Model Not Available

**Symptoms:**
- "Model 'nomic-embed-text' not found" error
- Embedding generation failures
- Model loading errors

**Diagnosis:**
```bash
# Check installed models
ollama list

# Test model availability
curl http://localhost:11434/api/tags
```

**Solutions:**
1. **Install embedding model:**
   ```bash
   ollama pull nomic-embed-text
   ```

2. **Verify model configuration:**
   ```json
   {
     "Ollama": {
       "EmbeddingModel": "nomic-embed-text"
     }
   }
   ```

3. **Test embedding generation:**
   ```bash
   curl -X POST http://localhost:11434/api/embeddings \
     -H "Content-Type: application/json" \
     -d '{"model": "nomic-embed-text", "prompt": "test"}'
   ```

### Problem: Embedding Generation Timeout

**Symptoms:**
- Timeout errors during embedding generation
- Slow embedding API responses
- Application hangs during initialization

**Diagnosis:**
```bash
# Check Ollama service status
curl http://localhost:11434/api/ps

# Monitor Ollama resource usage
docker stats ollama-container
```

**Solutions:**
1. **Increase timeout settings:**
   ```json
   {
     "Ollama": {
       "TimeoutSeconds": 120,
       "MaxConcurrentRequests": 3
     }
   }
   ```

2. **Reduce batch size:**
   ```json
   {
     "Vector": {
       "BatchSize": 50
     }
   }
   ```

3. **Enable embedding caching:**
   ```json
   {
     "Vector": {
       "CacheEmbeddings": true,
       "EmbeddingCacheExpirationHours": 24
     }
   }
   ```

### Problem: Inconsistent Embeddings

**Symptoms:**
- Different embeddings for same input
- Similarity scores vary between runs
- Unexpected similarity results

**Diagnosis:**
```bash
# Test embedding consistency
for i in {1..5}; do
  curl -X POST http://localhost:11434/api/embeddings \
    -H "Content-Type: application/json" \
    -d '{"model": "nomic-embed-text", "prompt": "test"}' | jq '.embedding[0:5]'
done
```

**Solutions:**
1. **Verify model version:**
   ```bash
   ollama show nomic-embed-text
   ```

2. **Use consistent preprocessing:**
   ```json
   {
     "Vector": {
       "VectorNormalization": "L2",
       "EnableVectorCompression": false
     }
   }
   ```

3. **Clear embedding cache:**
   ```bash
   # Clear application cache and regenerate
   dotnet JSON-Whisperer.dll --clear-embedding-cache
   ```

## Knowledge Base Issues

### Problem: JSON Files Not Loading

**Symptoms:**
- "No JSON examples found" message
- Knowledge base initialization fails
- Empty knowledge base

**Diagnosis:**
```bash
# Check AppData directory structure
find AppData -name "*.json" -type f

# Validate JSON files
find AppData -name "*.json" -exec echo "Checking {}" \; -exec jq . {} \;

# Check file permissions
ls -la AppData/examples/
```

**Solutions:**
1. **Verify directory structure:**
   ```bash
   mkdir -p AppData/examples
   ```

2. **Add sample JSON files:**
   ```bash
   # Create sample files
   echo '{"user": {"name": "John", "age": 30}}' > AppData/examples/user.json
   echo "User profile data with personal information" > AppData/examples/user.json.description.txt
   ```

3. **Fix file permissions:**
   ```bash
   chmod -R 755 AppData/
   chown -R $(whoami) AppData/
   ```

### Problem: Missing Description Files

**Symptoms:**
- Warning messages about missing descriptions
- Incomplete knowledge base initialization
- Poor similarity matching quality

**Diagnosis:**
```bash
# Find JSON files without descriptions
for json_file in AppData/examples/*.json; do
  desc_file="${json_file}.description.txt"
  if [ ! -f "$desc_file" ]; then
    echo "Missing: $desc_file"
  fi
done
```

**Solutions:**
1. **Create missing description files:**
   ```bash
   # For each JSON file, create corresponding description
   for json_file in AppData/examples/*.json; do
     desc_file="${json_file}.description.txt"
     if [ ! -f "$desc_file" ]; then
       echo "Description for $(basename $json_file)" > "$desc_file"
     fi
   done
   ```

2. **Validate description content:**
   ```bash
   # Check description files are not empty
   find AppData/examples -name "*.description.txt" -empty
   ```

### Problem: Invalid JSON Format

**Symptoms:**
- JSON parsing errors during initialization
- "Invalid JSON" error messages
- Knowledge base loading failures

**Diagnosis:**
```bash
# Validate all JSON files
find AppData/examples -name "*.json" -exec echo "Validating {}" \; -exec jq empty {} \;
```

**Solutions:**
1. **Fix JSON syntax errors:**
   ```bash
   # Use jq to format and validate JSON
   for json_file in AppData/examples/*.json; do
     if ! jq empty "$json_file" 2>/dev/null; then
       echo "Invalid JSON: $json_file"
       # Fix manually or regenerate
     fi
   done
   ```

2. **Use JSON formatter:**
   ```bash
   # Format JSON files
   for json_file in AppData/examples/*.json; do
     jq . "$json_file" > "${json_file}.tmp" && mv "${json_file}.tmp" "$json_file"
   done
   ```

## Performance Problems

### Problem: High Memory Usage

**Symptoms:**
- Out of memory errors
- Slow application performance
- System becomes unresponsive

**Diagnosis:**
```bash
# Monitor memory usage
docker stats json-whisperer-container

# Check application memory metrics
dotnet JSON-Whisperer.dll --memory-diagnostics
```

**Solutions:**
1. **Reduce batch size:**
   ```json
   {
     "Vector": {
       "BatchSize": 25
     }
   }
   ```

2. **Enable vector compression:**
   ```json
   {
     "Vector": {
       "EnableVectorCompression": true
     }
   }
   ```

3. **Limit concurrent operations:**
   ```json
   {
     "Ollama": {
       "MaxConcurrentRequests": 2
     }
   }
   ```

### Problem: Slow Similarity Search

**Symptoms:**
- Long response times for similarity queries
- Application appears to hang
- Timeout errors

**Diagnosis:**
```bash
# Benchmark similarity search
time dotnet JSON-Whisperer.dll --benchmark-similarity

# Check ScyllaDB query performance
docker exec -it scylla-container nodetool cfstats json_whisperer.embeddings
```

**Solutions:**
1. **Optimize ScyllaDB queries:**
   ```json
   {
     "ScyllaDb": {
       "MaxConnectionsPerHost": 16,
       "EnableCompression": true
     }
   }
   ```

2. **Reduce search scope:**
   ```json
   {
     "Vector": {
       "MaxSimilarResults": 3,
       "SimilarityThreshold": 0.8
     }
   }
   ```

3. **Enable result caching:**
   ```json
   {
     "Application": {
       "EnableCaching": true,
       "CacheExpirationMinutes": 30
     }
   }
   ```

## Configuration Issues

### Problem: Environment Variables Not Working

**Symptoms:**
- Configuration values not being overridden
- Default values used instead of environment variables
- Inconsistent behavior between environments

**Diagnosis:**
```bash
# Check environment variables
env | grep -E "(OLLAMA|SCYLLADB|VECTOR)_"

# Test configuration loading
dotnet JSON-Whisperer.dll --dump-config
```

**Solutions:**
1. **Verify environment variable names:**
   ```bash
   # Correct format for nested configuration
   export ScyllaDb__ContactPoints="scylla-host"
   export Vector__SimilarityThreshold="0.8"
   export Ollama__BaseUrl="http://ollama:11434"
   ```

2. **Check configuration precedence:**
   ```bash
   # Order: Environment Variables > appsettings.{Environment}.json > appsettings.json
   export ASPNETCORE_ENVIRONMENT=Production
   ```

### Problem: SSL/TLS Configuration Issues

**Symptoms:**
- SSL connection errors to ScyllaDB
- Certificate validation failures
- Secure connection timeouts

**Diagnosis:**
```bash
# Test SSL connection
openssl s_client -connect scylla-host:9042 -servername scylla-host

# Check certificate files
ls -la /etc/ssl/certs/scylla-cert.pem
ls -la /etc/ssl/private/scylla-key.pem
```

**Solutions:**
1. **Configure SSL properly:**
   ```json
   {
     "ScyllaDb": {
       "EnableSSL": true,
       "SSLCertificatePath": "/etc/ssl/certs/scylla-cert.pem",
       "SSLKeyPath": "/etc/ssl/private/scylla-key.pem"
     }
   }
   ```

2. **Disable SSL for testing:**
   ```json
   {
     "ScyllaDb": {
       "EnableSSL": false
     }
   }
   ```

## Network and Connectivity

### Problem: Service Discovery Issues

**Symptoms:**
- "Host not found" errors
- Connection refused errors
- Intermittent connectivity issues

**Diagnosis:**
```bash
# Test DNS resolution
nslookup ollama-service
nslookup scylla-host

# Check network connectivity
telnet ollama-service 11434
telnet scylla-host 9042

# Verify Docker network
docker network ls
docker network inspect bridge
```

**Solutions:**
1. **Use IP addresses instead of hostnames:**
   ```json
   {
     "Ollama": {
       "BaseUrl": "http://192.168.1.100:11434"
     },
     "ScyllaDb": {
       "ContactPoints": "192.168.1.101"
     }
   }
   ```

2. **Configure Docker networking:**
   ```yaml
   # docker-compose.yml
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

## Memory and Resource Issues

### Problem: Container Resource Limits

**Symptoms:**
- Container killed by OOM killer
- Performance degradation under load
- Resource limit exceeded errors

**Diagnosis:**
```bash
# Check container resource usage
docker stats --no-stream

# Check system resources
free -h
df -h
```

**Solutions:**
1. **Increase container memory limits:**
   ```yaml
   # docker-compose.yml
   services:
     json-whisperer:
       deploy:
         resources:
           limits:
             memory: 4G
           reservations:
             memory: 2G
   ```

2. **Optimize application memory usage:**
   ```json
   {
     "Performance": {
       "MaxMemoryUsageMB": 2048,
       "EnableGarbageCollectionMetrics": true
     }
   }
   ```

## Logging and Monitoring

### Problem: Insufficient Logging

**Symptoms:**
- Difficult to diagnose issues
- Missing error details
- No performance metrics

**Solutions:**
1. **Enable detailed logging:**
   ```json
   {
     "Logging": {
       "LogLevel": {
         "Default": "Debug",
         "JSON_Whisperer": "Debug"
       }
     }
   }
   ```

2. **Enable file logging:**
   ```json
   {
     "Logging": {
       "File": {
         "Enabled": true,
         "Path": "/var/log/json-whisperer/app.log"
       }
     }
   }
   ```

3. **Enable performance metrics:**
   ```json
   {
     "Performance": {
       "EnableTiming": true,
       "EnableMemoryTracking": true
     }
   }
   ```

### Useful Diagnostic Commands

All diagnostic commands return exit code 0 on success and exit code 1 on failure, making them suitable for automation and CI/CD pipelines.

```bash
# System Health and Configuration
dotnet JSON-Whisperer.dll --health-check              # Check all services (exit 0 if healthy)
dotnet JSON-Whisperer.dll --validate-config           # Validate configuration (exit 0 if valid)

# Component-Specific Tests
dotnet JSON-Whisperer.dll --test-ollama               # Test Ollama service (exit 0 if passing)
dotnet JSON-Whisperer.dll --test-scylla               # Test ScyllaDB (exit 0 if passing)
dotnet JSON-Whisperer.dll --test-embedding            # Test embedding generation (exit 0 if passing)
dotnet JSON-Whisperer.dll --test-similarity           # Test similarity search (exit 0 if passing)

# Knowledge Base Management
dotnet JSON-Whisperer.dll --validate-knowledge-base   # Validate JSON files (exit 0 if valid)
dotnet JSON-Whisperer.dll --reinitialize-knowledge-base  # Regenerate embeddings (exit 0 if successful)

# Performance Benchmarks
dotnet JSON-Whisperer.dll --benchmark-all             # Run all benchmarks (exit 0 on completion)
dotnet JSON-Whisperer.dll --benchmark-similarity      # Benchmark similarity search
dotnet JSON-Whisperer.dll --benchmark-vector-operations  # Benchmark vector operations
dotnet JSON-Whisperer.dll --benchmark-embedding       # Benchmark embedding generation
```

**Using Diagnostic Commands in Scripts:**

```bash
#!/bin/bash
# Pre-deployment health check script

echo "Running pre-deployment diagnostics..."

# Check configuration
if ! dotnet JSON-Whisperer.dll --validate-config; then
  echo "❌ Configuration validation failed"
  exit 1
fi
echo "✓ Configuration valid"

# Check all services
if ! dotnet JSON-Whisperer.dll --health-check; then
  echo "❌ Health check failed"
  exit 1
fi
echo "✓ All services healthy"

# Validate knowledge base
if ! dotnet JSON-Whisperer.dll --validate-knowledge-base; then
  echo "❌ Knowledge base validation failed"
  exit 1
fi
echo "✓ Knowledge base valid"

echo "✓ All pre-deployment checks passed"
exit 0
```

**CI/CD Pipeline Integration:**

```yaml
# Example GitHub Actions workflow
- name: Validate Configuration
  run: dotnet JSON-Whisperer.dll --validate-config

- name: Health Check
  run: dotnet JSON-Whisperer.dll --health-check

- name: Test Services
  run: |
    dotnet JSON-Whisperer.dll --test-ollama
    dotnet JSON-Whisperer.dll --test-scylla
    dotnet JSON-Whisperer.dll --test-embedding

- name: Run Benchmarks
  run: dotnet JSON-Whisperer.dll --benchmark-all > benchmark-results.txt
```

This troubleshooting guide should help you diagnose and resolve most issues with JSON-Whisperer, especially those related to vector similarity matching and ScyllaDB integration.