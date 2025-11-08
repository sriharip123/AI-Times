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

```bash
# Show help
dotnet JSON-Whisperer.dll --help

# Verbose mode (shows similarity matching results)
dotnet JSON-Whisperer.dll --verbose '{"data": "example"}'

# File input
dotnet JSON-Whisperer.dll --file path/to/file.json

# Disable similarity matching
dotnet JSON-Whisperer.dll --no-similarity '{"data": "example"}'

# Health check
dotnet JSON-Whisperer.dll --health-check

# Configuration validation
dotnet JSON-Whisperer.dll --validate-config

# Test individual services
dotnet JSON-Whisperer.dll --test-ollama
dotnet JSON-Whisperer.dll --test-scylla
dotnet JSON-Whisperer.dll --test-embedding

# Reinitialize knowledge base
dotnet JSON-Whisperer.dll --reinitialize-knowledge-base

# Performance benchmarks
dotnet JSON-Whisperer.dll --benchmark-all
dotnet JSON-Whisperer.dll --benchmark-similarity
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

## Integration Examples

### CI/CD Pipeline Integration

```yaml
# GitHub Actions example
name: JSON Analysis
on: [push]
jobs:
  analyze:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v2
      - name: Setup .NET
        uses: actions/setup-dotnet@v1
        with:
          dotnet-version: '9.0.x'
      - name: Start services
        run: docker-compose up -d
      - name: Wait for services
        run: sleep 30
      - name: Analyze JSON files
        run: |
          for file in test-data/*.json; do
            dotnet JSON-Whisperer.dll --file "$file" > "analysis/$(basename $file).txt"
          done
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