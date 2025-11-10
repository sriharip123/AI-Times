# JSON-Whisperer Usage Examples

This document provides comprehensive examples of using JSON-Whisperer, including vector similarity matching features.

## Table of Contents

- [Basic Usage](#basic-usage)
- [Vector Similarity Features](#vector-similarity-features)
- [Command Line Options](#command-line-options)
- [Configuration Examples](#configuration-examples)
- [Advanced Use Cases](#advanced-use-cases)
- [Troubleshooting Examples](#troubleshooting-examples)

## Basic Usage

### Simple JSON Analysis

```bash
# Analyze a simple JSON object
dotnet JSON-Whisperer.dll '{"name": "John Doe", "age": 30, "city": "New York"}'
```

**Output:**
```
=== JSON Analysis Results ===

Original JSON:
{
  "name": "John Doe",
  "age": 30,
  "city": "New York"
}

AI-Generated Summary:
This JSON represents a person's basic profile information containing their full name, age, and current city of residence. It appears to be a simple user record that might be used in a contact management system or user database.

Analysis Metadata:
- Total Properties: 3
- Maximum Depth: 1
- Estimated Size: 45 bytes
- Processing Time: 1.2 seconds
```

### File Input

```bash
# Create a sample JSON file
echo '{
  "product": {
    "id": "PROD-001",
    "name": "Wireless Headphones",
    "price": 99.99,
    "category": "Electronics",
    "specifications": {
      "battery_life": "20 hours",
      "connectivity": "Bluetooth 5.0",
      "weight": "250g"
    },
    "reviews": [
      {"rating": 5, "comment": "Excellent sound quality"},
      {"rating": 4, "comment": "Good value for money"}
    ]
  }
}' > product.json

# Analyze the file
dotnet JSON-Whisperer.dll --file product.json
```

### Standard Input

```bash
# Pipe JSON from another command
curl -s https://api.example.com/user/123 | dotnet JSON-Whisperer.dll

# Or use heredoc
dotnet JSON-Whisperer.dll << EOF
{
  "order": {
    "id": "ORD-12345",
    "customer": "John Smith",
    "items": [
      {"product": "Widget A", "quantity": 2, "price": 15.99},
      {"product": "Widget B", "quantity": 1, "price": 25.50}
    ],
    "total": 57.48,
    "status": "shipped"
  }
}
EOF
```

## Vector Similarity Features

### Verbose Mode with Similarity Matching

```bash
# Enable verbose mode to see similarity results
dotnet JSON-Whisperer.dll --verbose '{
  "user": {
    "id": 12345,
    "name": "Alice Johnson",
    "email": "alice@example.com",
    "profile": {
      "age": 28,
      "location": "San Francisco",
      "interests": ["photography", "travel", "technology"]
    }
  }
}'
```

**Output with Similarity Matching:**
```
=== JSON Analysis Results ===

Original JSON:
{
  "user": {
    "id": 12345,
    "name": "Alice Johnson",
    "email": "alice@example.com",
    "profile": {
      "age": 28,
      "location": "San Francisco",
      "interests": ["photography", "travel", "technology"]
    }
  }
}

Similar Examples Found:
┌─────────────────────────────────────────────────────────────────────────────┐
│ Similarity Score: 0.87                                                     │
│ Example: user-profile.json                                                  │
│ Description: User profile data containing personal information, contact     │
│ details, and user preferences for a social media platform.                 │
└─────────────────────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────────────────┐
│ Similarity Score: 0.73                                                     │
│ Example: customer-data.json                                                 │
│ Description: Customer information with demographics and preference data     │
│ used in e-commerce personalization systems.                                │
└─────────────────────────────────────────────────────────────────────────────┘

AI-Generated Summary (Enhanced with Similar Examples):
This JSON represents a comprehensive user profile similar to those found in social media and e-commerce platforms. Based on similar patterns in the knowledge base, this structure contains essential user identification (ID, name, email) combined with demographic and preference data. The profile section includes personal details like age and location, along with an interests array that suggests this data is used for content personalization or recommendation systems. This format is commonly used in user management systems where both authentication and personalization features are required.

Analysis Metadata:
- Total Properties: 6
- Maximum Depth: 2
- Array Fields: interests
- Object Fields: user, profile
- Estimated Size: 187 bytes
- Processing Time: 2.8 seconds
- Similarity Search Time: 0.4 seconds
- Similar Examples Found: 2
```

### E-commerce Product Data Example

```bash
dotnet JSON-Whisperer.dll --verbose '{
  "products": [
    {
      "sku": "LAPTOP-001",
      "name": "Gaming Laptop",
      "price": 1299.99,
      "category": "Computers",
      "specifications": {
        "cpu": "Intel i7-12700H",
        "ram": "16GB DDR4",
        "storage": "512GB SSD",
        "gpu": "RTX 3060"
      },
      "availability": {
        "in_stock": true,
        "quantity": 15,
        "warehouse": "US-WEST"
      }
    }
  ],
  "metadata": {
    "total_count": 1,
    "page": 1,
    "last_updated": "2024-11-05T10:30:00Z"
  }
}'
```

### API Response Analysis

```bash
dotnet JSON-Whisperer.dll --verbose '{
  "status": "success",
  "data": {
    "transactions": [
      {
        "id": "TXN-789012",
        "amount": 250.00,
        "currency": "USD",
        "timestamp": "2024-11-05T14:22:33Z",
        "merchant": {
          "name": "Coffee Shop Downtown",
          "category": "Food & Beverage",
          "location": "New York, NY"
        },
        "payment_method": {
          "type": "credit_card",
          "last_four": "4567",
          "brand": "Visa"
        }
      }
    ]
  },
  "pagination": {
    "current_page": 1,
    "total_pages": 5,
    "total_records": 47
  }
}'
```

## Command Line Options

### Available Options

#### Input and Processing Options

```bash
# Show help
dotnet JSON-Whisperer.dll --help

# Verbose mode (shows similarity matching results)
dotnet JSON-Whisperer.dll --verbose '{"data": "example"}'

# File input
dotnet JSON-Whisperer.dll --file path/to/file.json

# Disable similarity matching for faster processing
dotnet JSON-Whisperer.dll --no-similarity '{"data": "example"}'
```

#### Diagnostic Commands

```bash
# System Health Check - Verify all services are operational
dotnet JSON-Whisperer.dll --health-check

# Configuration Validation - Check all settings are valid
dotnet JSON-Whisperer.dll --validate-config

# Test Individual Services
dotnet JSON-Whisperer.dll --test-ollama      # Test Ollama connectivity and model
dotnet JSON-Whisperer.dll --test-scylla      # Test ScyllaDB connectivity and keyspace
dotnet JSON-Whisperer.dll --test-embedding   # Test embedding generation
dotnet JSON-Whisperer.dll --test-similarity  # Test similarity search functionality
```

**Example Health Check Output:**
```
=== System Health Check ===

✓ Ollama Service: Healthy (Response time: 45ms)
  - Base URL: http://localhost:11434
  - Model: mistral (available)
  - Embedding Model: nomic-embed-text (available)

✓ ScyllaDB: Healthy (Response time: 23ms)
  - Contact Points: 127.0.0.1:9042
  - Keyspace: json_whisperer (exists)
  - Connection: Active

✓ Embedding Service: Healthy (Response time: 156ms)
  - Model: nomic-embed-text
  - Embedding Dimensions: 768
  - Test Embedding: Generated successfully

✓ Knowledge Base: Healthy
  - JSON Files: 15 files found
  - Embeddings: 15 embeddings stored
  - Status: Initialized

=== Health Check Summary ===
All services are operational
Total check duration: 1.2 seconds
Exit code: 0
```

**Example Configuration Validation Output:**
```
=== Configuration Validation ===

✓ Ollama Configuration: Valid
  - BaseUrl: http://localhost:11434 (reachable)
  - ModelName: mistral (valid)
  - EmbeddingModel: nomic-embed-text (valid)
  - TimeoutSeconds: 30 (valid range)

✓ ScyllaDB Configuration: Valid
  - ContactPoints: 127.0.0.1 (valid)
  - Port: 9042 (valid range)
  - Keyspace: json_whisperer (valid)

✓ Vector Configuration: Valid
  - SimilarityThreshold: 0.7 (valid range: 0.0-1.0)
  - MaxSimilarResults: 5 (valid)
  - AppDataPath: AppData (exists)

✓ Application Configuration: Valid
  - MaxJsonSizeBytes: 10485760 (valid)
  - VerboseMode: false

=== Validation Summary ===
All configuration settings are valid
Exit code: 0
```

**Example Service Test Output:**
```bash
# Test Ollama service
$ dotnet JSON-Whisperer.dll --test-ollama

=== Ollama Service Test ===

Testing connection to http://localhost:11434...
✓ Connection successful (45ms)

Testing model availability...
✓ Model 'mistral' is available
✓ Model 'nomic-embed-text' is available

Testing text generation...
✓ Text generation successful (1.2s)

Testing embedding generation...
✓ Embedding generation successful (156ms)
✓ Embedding dimensions: 768 (expected: 768)

=== Test Summary ===
All Ollama tests passed
Exit code: 0
```

#### Knowledge Base Management

```bash
# Validate knowledge base files
dotnet JSON-Whisperer.dll --validate-knowledge-base

# Reinitialize knowledge base (clear and regenerate embeddings)
dotnet JSON-Whisperer.dll --reinitialize-knowledge-base
```

**Example Knowledge Base Validation Output:**
```
=== Knowledge Base Validation ===

Scanning AppData directory: AppData/examples

JSON Files Found: 15
✓ user-profile.json (valid JSON, description exists)
✓ product-catalog.json (valid JSON, description exists)
✓ order-data.json (valid JSON, description exists)
✓ api-response.json (valid JSON, description exists)
✓ customer-data.json (valid JSON, description exists)
... (10 more files)

=== Validation Summary ===
Total Files: 15
Valid JSON: 15
Valid Descriptions: 15
Errors: 0

Knowledge base is valid
Exit code: 0
```

**Example Knowledge Base Reinitialization Output:**
```
=== Knowledge Base Reinitialization ===

Step 1: Clearing existing embeddings...
✓ Cleared 15 embeddings from ScyllaDB

Step 2: Scanning JSON files...
✓ Found 15 JSON files in AppData/examples

Step 3: Generating embeddings...
[1/15] Processing user-profile.json... ✓ (234ms)
[2/15] Processing product-catalog.json... ✓ (198ms)
[3/15] Processing order-data.json... ✓ (212ms)
[4/15] Processing api-response.json... ✓ (189ms)
... (11 more files)

Step 4: Storing embeddings in ScyllaDB...
✓ Stored 15 embeddings successfully

=== Reinitialization Summary ===
Files Processed: 15
Embeddings Created: 15
Errors: 0
Total Duration: 4.5 seconds

Knowledge base reinitialized successfully
Exit code: 0
```

#### Performance Benchmarks

```bash
# Run all benchmarks
dotnet JSON-Whisperer.dll --benchmark-all

# Run specific benchmarks
dotnet JSON-Whisperer.dll --benchmark-similarity         # Similarity search performance
dotnet JSON-Whisperer.dll --benchmark-vector-operations  # Vector operations performance
dotnet JSON-Whisperer.dll --benchmark-embedding          # Embedding generation speed
```

**Example Benchmark Output:**
```
=== Performance Benchmarks ===

Benchmark 1: Similarity Search
Running 100 iterations...
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ 100%

Results:
  Total Duration: 12.5 seconds
  Average Duration: 125ms per search
  Operations/Second: 8.0
  Memory Used: 45 MB
  Min Duration: 98ms
  Max Duration: 234ms

Benchmark 2: Vector Operations
Running 100 iterations...
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ 100%

Results:
  Total Duration: 8.2 seconds
  Average Duration: 82ms per operation
  Operations/Second: 12.2
  Memory Used: 32 MB
  Min Duration: 67ms
  Max Duration: 156ms

Benchmark 3: Embedding Generation
Running 100 iterations...
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ 100%

Results:
  Total Duration: 15.6 seconds
  Average Duration: 156ms per embedding
  Operations/Second: 6.4
  Memory Used: 28 MB
  Min Duration: 134ms
  Max Duration: 289ms

=== Benchmark Summary ===
All benchmarks completed successfully
Total Duration: 36.3 seconds
Exit code: 0
```

### Combining Options

```bash
# Verbose mode with file input
dotnet JSON-Whisperer.dll --verbose --file complex-data.json

# Test mode with specific JSON
dotnet JSON-Whisperer.dll --verbose --no-similarity '{"test": "data"}'
```

## Configuration Examples

### Development Configuration

```json
{
  "Ollama": {
    "BaseUrl": "http://localhost:11434",
    "ModelName": "mistral",
    "EmbeddingModel": "nomic-embed-text",
    "TimeoutSeconds": 30
  },
  "Vector": {
    "SimilarityThreshold": 0.6,
    "MaxSimilarResults": 5,
    "EnableSimilarityMatching": true,
    "AppDataPath": "AppData"
  },
  "ScyllaDb": {
    "ContactPoints": "127.0.0.1",
    "Port": 9042,
    "Keyspace": "json_whisperer_dev",
    "CreateKeyspaceIfNotExists": true
  },
  "Application": {
    "VerboseMode": true,
    "EnablePerformanceMetrics": true
  }
}
```

### Production Configuration

```json
{
  "Ollama": {
    "BaseUrl": "http://ollama-cluster:11434",
    "ModelName": "mistral",
    "EmbeddingModel": "nomic-embed-text",
    "TimeoutSeconds": 60,
    "RetryAttempts": 5,
    "MaxConcurrentRequests": 10
  },
  "Vector": {
    "SimilarityThreshold": 0.75,
    "MaxSimilarResults": 10,
    "EnableSimilarityMatching": true,
    "AppDataPath": "/app/data",
    "BatchSize": 200,
    "CacheEmbeddings": true,
    "EmbeddingCacheExpirationHours": 48
  },
  "ScyllaDb": {
    "ContactPoints": "scylla-node1,scylla-node2,scylla-node3",
    "Port": 9042,
    "Keyspace": "json_whisperer_prod",
    "Username": "${SCYLLADB_USERNAME}",
    "Password": "${SCYLLADB_PASSWORD}",
    "ReplicationFactor": 3,
    "ConsistencyLevel": "Quorum",
    "EnableSSL": true
  }
}
```

## Advanced Use Cases

### Batch Processing Multiple Files

```bash
# Process multiple JSON files
for file in data/*.json; do
  echo "Processing $file..."
  dotnet JSON-Whisperer.dll --verbose --file "$file" > "results/$(basename $file .json)_analysis.txt"
done
```

### API Integration Example

```bash
# Process API responses in a pipeline
curl -s "https://api.example.com/users" | \
  jq '.data[]' | \
  while IFS= read -r user; do
    echo "$user" | dotnet JSON-Whisperer.dll --verbose
  done
```

### Configuration Testing

```bash
# Test different similarity thresholds
for threshold in 0.5 0.6 0.7 0.8 0.9; do
  echo "Testing threshold: $threshold"
  VECTOR__SIMILARITY_THRESHOLD=$threshold dotnet JSON-Whisperer.dll --verbose '{"test": "data"}'
done
```

### Performance Monitoring

```bash
# Monitor performance with timing
time dotnet JSON-Whisperer.dll --verbose --file large-dataset.json

# Benchmark different configurations
dotnet JSON-Whisperer.dll --benchmark-all > performance-report.txt
```

## Knowledge Base Examples

### Setting Up Knowledge Base

```bash
# Create knowledge base directory
mkdir -p AppData/examples

# Add user profile example
cat > AppData/examples/user-profile.json << EOF
{
  "user": {
    "id": 12345,
    "name": "John Doe",
    "email": "john.doe@example.com",
    "profile": {
      "age": 30,
      "location": "New York",
      "preferences": ["technology", "sports"]
    }
  }
}
EOF

cat > AppData/examples/user-profile.json.description.txt << EOF
User profile data containing personal information, contact details, and user preferences for a social media or e-commerce platform.
EOF

# Add product catalog example
cat > AppData/examples/product-catalog.json << EOF
{
  "products": [
    {
      "id": "PROD-001",
      "name": "Wireless Headphones",
      "price": 99.99,
      "category": "Electronics",
      "specifications": {
        "battery_life": "20 hours",
        "connectivity": "Bluetooth 5.0"
      }
    }
  ]
}
EOF

cat > AppData/examples/product-catalog.json.description.txt << EOF
E-commerce product catalog with detailed product information including pricing, categories, and technical specifications.
EOF

# Add API response example
cat > AppData/examples/api-response.json << EOF
{
  "status": "success",
  "data": {
    "items": [
      {"id": 1, "name": "Item 1"},
      {"id": 2, "name": "Item 2"}
    ]
  },
  "metadata": {
    "total": 2,
    "page": 1
  }
}
EOF

cat > AppData/examples/api-response.json.description.txt << EOF
Standard API response format with status indicator, data payload, and pagination metadata commonly used in REST APIs.
EOF
```

### Testing Knowledge Base

```bash
# Validate knowledge base setup
dotnet JSON-Whisperer.dll --validate-knowledge-base

# Reinitialize if needed
dotnet JSON-Whisperer.dll --reinitialize-knowledge-base

# Test similarity matching with known patterns
dotnet JSON-Whisperer.dll --verbose '{
  "user": {
    "id": 67890,
    "name": "Jane Smith",
    "email": "jane@example.com",
    "profile": {
      "age": 25,
      "location": "California",
      "preferences": ["music", "art"]
    }
  }
}'
```

## Troubleshooting Examples

### Debugging Connection Issues

```bash
# Test Ollama connectivity
curl http://localhost:11434/api/tags

# Test ScyllaDB connectivity
docker exec -it scylla-container cqlsh -e "DESCRIBE KEYSPACES;"

# Run application health check
dotnet JSON-Whisperer.dll --health-check
```

### Performance Debugging

```bash
# Enable detailed logging
export LOGGING__LOGLEVEL__DEFAULT=Debug
dotnet JSON-Whisperer.dll --verbose '{"test": "data"}'

# Monitor resource usage
docker stats json-whisperer-container

# Benchmark specific operations
dotnet JSON-Whisperer.dll --benchmark-vector-operations
```

### Configuration Debugging

```bash
# Dump current configuration
dotnet JSON-Whisperer.dll --dump-config

# Test with minimal configuration
cat > minimal-config.json << EOF
{
  "Ollama": {
    "BaseUrl": "http://localhost:11434"
  },
  "Vector": {
    "EnableSimilarityMatching": false
  }
}
EOF

dotnet JSON-Whisperer.dll --config minimal-config.json '{"test": "data"}'
```

## Exit Codes and Automation

### Understanding Exit Codes

All diagnostic commands and normal operations return consistent exit codes:
- **Exit Code 0**: Success
- **Exit Code 1**: Failure

This makes JSON-Whisperer suitable for automation, scripting, and CI/CD integration.

### Exit Code Examples

#### Basic Exit Code Handling

```bash
# Check exit code explicitly
dotnet JSON-Whisperer.dll --health-check
if [ $? -eq 0 ]; then
  echo "Health check passed"
else
  echo "Health check failed"
fi

# Use exit code in conditional
if dotnet JSON-Whisperer.dll --validate-config; then
  echo "Configuration is valid"
  dotnet JSON-Whisperer.dll --file data.json
else
  echo "Configuration is invalid, aborting"
  exit 1
fi
```

#### Chaining Commands

```bash
# Stop on first failure (using &&)
dotnet JSON-Whisperer.dll --validate-config && \
dotnet JSON-Whisperer.dll --health-check && \
dotnet JSON-Whisperer.dll --file data.json

# Continue on failure (using ||)
dotnet JSON-Whisperer.dll --health-check || echo "Health check failed, but continuing..."

# Set -e to exit on any failure
set -e
dotnet JSON-Whisperer.dll --validate-config
dotnet JSON-Whisperer.dll --health-check
dotnet JSON-Whisperer.dll --test-ollama
echo "All checks passed"
```

#### Retry Logic with Exit Codes

```bash
#!/bin/bash
# Retry health check up to 5 times

MAX_RETRIES=5
RETRY_COUNT=0

while [ $RETRY_COUNT -lt $MAX_RETRIES ]; do
  if dotnet JSON-Whisperer.dll --health-check; then
    echo "Health check passed"
    exit 0
  else
    RETRY_COUNT=$((RETRY_COUNT + 1))
    echo "Attempt $RETRY_COUNT failed, retrying..."
    sleep 10
  fi
done

echo "Health check failed after $MAX_RETRIES attempts"
exit 1
```

#### Capturing and Logging Results

```bash
#!/bin/bash
# Run diagnostics and log results

LOG_FILE="diagnostics-$(date +%Y%m%d-%H%M%S).log"

echo "=== Diagnostic Report ===" > "$LOG_FILE"
echo "Date: $(date)" >> "$LOG_FILE"
echo "" >> "$LOG_FILE"

# Configuration validation
echo "Configuration Validation:" >> "$LOG_FILE"
if dotnet JSON-Whisperer.dll --validate-config >> "$LOG_FILE" 2>&1; then
  echo "Status: PASSED" >> "$LOG_FILE"
else
  echo "Status: FAILED" >> "$LOG_FILE"
fi
echo "" >> "$LOG_FILE"

# Health check
echo "Health Check:" >> "$LOG_FILE"
if dotnet JSON-Whisperer.dll --health-check >> "$LOG_FILE" 2>&1; then
  echo "Status: PASSED" >> "$LOG_FILE"
else
  echo "Status: FAILED" >> "$LOG_FILE"
fi
echo "" >> "$LOG_FILE"

# Service tests
for service in ollama scylla embedding; do
  echo "Testing $service:" >> "$LOG_FILE"
  if dotnet JSON-Whisperer.dll --test-$service >> "$LOG_FILE" 2>&1; then
    echo "Status: PASSED" >> "$LOG_FILE"
  else
    echo "Status: FAILED" >> "$LOG_FILE"
  fi
  echo "" >> "$LOG_FILE"
done

echo "Diagnostic report saved to $LOG_FILE"
```

## Integration Examples

### CI/CD Pipeline Integration

```yaml
# GitHub Actions - Comprehensive Example
name: JSON-Whisperer CI/CD
on: [push, pull_request]

jobs:
  test:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v3
      
      - name: Setup .NET
        uses: actions/setup-dotnet@v3
        with:
          dotnet-version: '9.0.x'
      
      - name: Start services
        run: docker-compose up -d
      
      - name: Wait for services to be ready
        run: |
          echo "Waiting for services to start..."
          sleep 30
      
      - name: Validate Configuration
        run: |
          dotnet JSON-Whisperer.dll --validate-config
          if [ $? -ne 0 ]; then
            echo "Configuration validation failed"
            exit 1
          fi
      
      - name: Health Check
        run: |
          dotnet JSON-Whisperer.dll --health-check
          if [ $? -ne 0 ]; then
            echo "Health check failed"
            docker-compose logs
            exit 1
          fi
      
      - name: Test Individual Services
        run: |
          dotnet JSON-Whisperer.dll --test-ollama || exit 1
          dotnet JSON-Whisperer.dll --test-scylla || exit 1
          dotnet JSON-Whisperer.dll --test-embedding || exit 1
      
      - name: Validate Knowledge Base
        run: dotnet JSON-Whisperer.dll --validate-knowledge-base
      
      - name: Run Benchmarks
        run: |
          dotnet JSON-Whisperer.dll --benchmark-all > benchmark-results.txt
          cat benchmark-results.txt
      
      - name: Analyze Test JSON Files
        run: |
          mkdir -p analysis-results
          for file in test-data/*.json; do
            echo "Analyzing $file..."
            dotnet JSON-Whisperer.dll --file "$file" > "analysis-results/$(basename $file).txt"
          done
      
      - name: Upload Results
        uses: actions/upload-artifact@v3
        with:
          name: analysis-results
          path: analysis-results/
      
      - name: Cleanup
        if: always()
        run: docker-compose down

# GitLab CI - Comprehensive Example
stages:
  - validate
  - test
  - benchmark
  - deploy

validate-config:
  stage: validate
  script:
    - dotnet JSON-Whisperer.dll --validate-config
  allow_failure: false

health-check:
  stage: test
  script:
    - docker-compose up -d
    - sleep 30
    - dotnet JSON-Whisperer.dll --health-check
  after_script:
    - docker-compose down

test-services:
  stage: test
  script:
    - docker-compose up -d
    - sleep 30
    - dotnet JSON-Whisperer.dll --test-ollama
    - dotnet JSON-Whisperer.dll --test-scylla
    - dotnet JSON-Whisperer.dll --test-embedding
    - dotnet JSON-Whisperer.dll --test-similarity
  after_script:
    - docker-compose down

run-benchmarks:
  stage: benchmark
  script:
    - docker-compose up -d
    - sleep 30
    - dotnet JSON-Whisperer.dll --benchmark-all > benchmark-results.txt
  artifacts:
    paths:
      - benchmark-results.txt
    expire_in: 1 week
  after_script:
    - docker-compose down

deploy-production:
  stage: deploy
  script:
    - dotnet JSON-Whisperer.dll --validate-config
    - dotnet JSON-Whisperer.dll --health-check
    - ./deploy.sh
  only:
    - main
  when: manual

# Jenkins Pipeline - Comprehensive Example
pipeline {
  agent any
  
  stages {
    stage('Setup') {
      steps {
        sh 'docker-compose up -d'
        sh 'sleep 30'
      }
    }
    
    stage('Validate Configuration') {
      steps {
        script {
          def result = sh(
            script: 'dotnet JSON-Whisperer.dll --validate-config',
            returnStatus: true
          )
          if (result != 0) {
            error('Configuration validation failed')
          }
        }
      }
    }
    
    stage('Health Check') {
      steps {
        script {
          def result = sh(
            script: 'dotnet JSON-Whisperer.dll --health-check',
            returnStatus: true
          )
          if (result != 0) {
            error('Health check failed')
          }
        }
      }
    }
    
    stage('Test Services') {
      parallel {
        stage('Test Ollama') {
          steps {
            sh 'dotnet JSON-Whisperer.dll --test-ollama'
          }
        }
        stage('Test ScyllaDB') {
          steps {
            sh 'dotnet JSON-Whisperer.dll --test-scylla'
          }
        }
        stage('Test Embedding') {
          steps {
            sh 'dotnet JSON-Whisperer.dll --test-embedding'
          }
        }
      }
    }
    
    stage('Validate Knowledge Base') {
      steps {
        sh 'dotnet JSON-Whisperer.dll --validate-knowledge-base'
      }
    }
    
    stage('Run Benchmarks') {
      steps {
        sh 'dotnet JSON-Whisperer.dll --benchmark-all > benchmark-results.txt'
        archiveArtifacts artifacts: 'benchmark-results.txt'
      }
    }
    
    stage('Deploy') {
      when {
        branch 'main'
      }
      steps {
        input message: 'Deploy to production?'
        sh './deploy.sh'
      }
    }
  }
  
  post {
    always {
      sh 'docker-compose down'
    }
    failure {
      mail to: 'team@example.com',
           subject: "Pipeline Failed: ${env.JOB_NAME} - ${env.BUILD_NUMBER}",
           body: "Check console output at ${env.BUILD_URL}"
    }
  }
}
```

### Azure DevOps Pipeline

```yaml
# azure-pipelines.yml
trigger:
  - main
  - develop

pool:
  vmImage: 'ubuntu-latest'

stages:
- stage: Validate
  jobs:
  - job: ValidateConfig
    steps:
    - task: UseDotNet@2
      inputs:
        version: '9.0.x'
    
    - script: |
        dotnet JSON-Whisperer.dll --validate-config
      displayName: 'Validate Configuration'
      failOnStderr: true

- stage: Test
  jobs:
  - job: HealthCheck
    steps:
    - script: |
        docker-compose up -d
        sleep 30
        dotnet JSON-Whisperer.dll --health-check
      displayName: 'Run Health Check'
      failOnStderr: true
    
    - script: docker-compose down
      displayName: 'Cleanup'
      condition: always()
  
  - job: ServiceTests
    steps:
    - script: |
        docker-compose up -d
        sleep 30
      displayName: 'Start Services'
    
    - script: dotnet JSON-Whisperer.dll --test-ollama
      displayName: 'Test Ollama'
    
    - script: dotnet JSON-Whisperer.dll --test-scylla
      displayName: 'Test ScyllaDB'
    
    - script: dotnet JSON-Whisperer.dll --test-embedding
      displayName: 'Test Embedding'
    
    - script: docker-compose down
      displayName: 'Cleanup'
      condition: always()

- stage: Benchmark
  jobs:
  - job: RunBenchmarks
    steps:
    - script: |
        docker-compose up -d
        sleep 30
        dotnet JSON-Whisperer.dll --benchmark-all > $(Build.ArtifactStagingDirectory)/benchmark-results.txt
      displayName: 'Run Benchmarks'
    
    - task: PublishBuildArtifacts@1
      inputs:
        pathToPublish: '$(Build.ArtifactStagingDirectory)'
        artifactName: 'benchmarks'
    
    - script: docker-compose down
      displayName: 'Cleanup'
      condition: always()

- stage: Deploy
  condition: and(succeeded(), eq(variables['Build.SourceBranch'], 'refs/heads/main'))
  jobs:
  - deployment: DeployProduction
    environment: 'production'
    strategy:
      runOnce:
        deploy:
          steps:
          - script: |
              dotnet JSON-Whisperer.dll --validate-config
              dotnet JSON-Whisperer.dll --health-check
            displayName: 'Pre-Deployment Validation'
          
          - script: ./deploy.sh
            displayName: 'Deploy Application'
```

### Monitoring Integration

```bash
# Prometheus metrics endpoint (if implemented)
curl http://localhost:8080/metrics

# Health check for monitoring
curl http://localhost:8080/health

# Custom monitoring script
#!/bin/bash
while true; do
  if ! dotnet JSON-Whisperer.dll --health-check > /dev/null 2>&1; then
    echo "$(date): Health check failed" >> /var/log/json-whisperer-monitor.log
    # Send alert
  fi
  sleep 60
done
```

This usage guide provides comprehensive examples of how to use JSON-Whisperer effectively, including the advanced vector similarity features that enhance the quality of JSON analysis and summaries.