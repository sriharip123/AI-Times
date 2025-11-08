# JSON-Whisperer Deployment Guide

This guide provides detailed instructions for deploying JSON-Whisperer in various environments.

## Table of Contents

- [Prerequisites](#prerequisites)
- [Local Development Setup](#local-development-setup)
- [Production Deployment](#production-deployment)
- [Docker Deployment](#docker-deployment)
- [Cloud Deployment](#cloud-deployment)
- [Configuration Management](#configuration-management)
- [Monitoring and Logging](#monitoring-and-logging)
- [Security Considerations](#security-considerations)
- [Troubleshooting](#troubleshooting)

## Prerequisites

### System Requirements

- **.NET 9 Runtime** or SDK
- **Ollama Service** (local or remote)
- **Mistral Model** (or compatible model)
- **ScyllaDB Database** (for vector similarity matching)
- **nomic-embed-text Model** (for embedding generation)
- **Minimum 4GB RAM** (8GB recommended for large JSON files and vector operations)
- **Network access** to Ollama service (port 11434 by default)
- **Network access** to ScyllaDB cluster (port 9042 by default)

### Supported Platforms

- Windows 10/11 (x64, ARM64)
- Linux (x64, ARM64)
- macOS (x64, ARM64)

## Local Development Setup

### 1. Install Prerequisites

#### .NET 9 SDK
```bash
# Windows (using winget)
winget install Microsoft.DotNet.SDK.9

# macOS (using Homebrew)
brew install dotnet

# Linux (Ubuntu/Debian)
wget https://packages.microsoft.com/config/ubuntu/22.04/packages-microsoft-prod.deb -O packages-microsoft-prod.deb
sudo dpkg -i packages-microsoft-prod.deb
sudo apt-get update && sudo apt-get install -y dotnet-sdk-9.0
```

#### Ollama Installation
```bash
# Windows
# Download from https://ollama.ai and run installer

# macOS
brew install ollama

# Linux
curl -fsSL https://ollama.ai/install.sh | sh
```

#### ScyllaDB Installation

##### Local Development (Docker)
```bash
# Run ScyllaDB in Docker for development
docker run --name scylla-dev -p 9042:9042 -d scylladb/scylla:latest

# Wait for ScyllaDB to start (check logs)
docker logs -f scylla-dev

# Verify connection
docker exec -it scylla-dev cqlsh
```

##### Production Installation (Linux)
```bash
# Ubuntu/Debian
sudo apt-get update
sudo apt-get install -y software-properties-common
sudo apt-key adv --keyserver hkp://keyserver.ubuntu.com:80 --recv-keys 5e08fbd8b5d6ec9c
sudo add-apt-repository 'deb http://downloads.scylladb.com/deb/ubuntu $(lsb_release -sc) main'
sudo apt-get update
sudo apt-get install -y scylla

# CentOS/RHEL
sudo yum install -y epel-release
sudo curl -o /etc/yum.repos.d/scylla.repo -L http://downloads.scylladb.com/rpm/centos/scylla-4.6.repo
sudo yum install -y scylla

# Configure and start ScyllaDB
sudo scylla_setup
sudo systemctl enable scylla-server
sudo systemctl start scylla-server
```

##### ScyllaDB Cloud Setup
```bash
# For ScyllaDB Cloud, obtain connection details from your cloud console
# Update appsettings.json with provided connection string and credentials
```

### 2. Setup Ollama Service

```bash
# Start Ollama service
ollama serve

# In another terminal, install required models
ollama pull mistral
ollama pull nomic-embed-text

# Verify installation
ollama list
```

### 3. Setup ScyllaDB Database

```bash
# Connect to ScyllaDB
docker exec -it scylla-dev cqlsh

# Create keyspace (will be done automatically by application)
# But you can create it manually if needed:
CREATE KEYSPACE IF NOT EXISTS json_whisperer 
WITH REPLICATION = {
  'class': 'SimpleStrategy',
  'replication_factor': 1
};

# Exit cqlsh
exit
```

### 4. Initialize Knowledge Base

```bash
# Create AppData directory structure
mkdir -p JSON-Whisperer/AppData/examples

# Add sample JSON files with descriptions
# Each JSON file should have a corresponding .description.txt file
# Example: sample.json and sample.json.description.txt
```

### 5. Build and Run Application

```bash
# Clone repository
git clone <repository-url>
cd JSON-Whisperer

# Build application
dotnet build

# Run with sample JSON (basic mode)
dotnet run --project JSON-Whisperer -- '{"name": "test", "value": 123}'

# Run with verbose mode to see similarity matching
dotnet run --project JSON-Whisperer -- --verbose '{"user": {"name": "John", "age": 30}}'

# Test with file input
echo '{"products": [{"id": 1, "name": "Widget"}]}' > sample.json
dotnet run --project JSON-Whisperer -- --file sample.json
```

## Production Deployment

### 1. Self-Contained Deployment

Create a self-contained deployment that doesn't require .NET runtime on target machine:

```bash
# Windows x64
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true

# Linux x64
dotnet publish -c Release -r linux-x64 --self-contained true -p:PublishSingleFile=true

# macOS x64
dotnet publish -c Release -r osx-x64 --self-contained true -p:PublishSingleFile=true
```

### 2. Framework-Dependent Deployment

Smaller deployment size, requires .NET runtime on target:

```bash
dotnet publish -c Release --no-self-contained
```

### 3. Production Configuration

Create production `appsettings.Production.json`:

```json
{
  "Ollama": {
    "BaseUrl": "http://ollama-service:11434",
    "ModelName": "mistral",
    "TimeoutSeconds": 60,
    "RetryAttempts": 5,
    "RetryDelaySeconds": 3
  },
  "Application": {
    "VerboseMode": false,
    "MaxJsonSizeBytes": 52428800,
    "EnablePerformanceMetrics": true,
    "OutputFormat": "standard"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "JSON_Whisperer": "Information",
      "Microsoft": "Warning"
    },
    "File": {
      "Enabled": true,
      "Path": "/var/log/json-whisperer/app.log",
      "MaxFileSizeBytes": 10485760,
      "MaxFiles": 10
    }
  },
  "Performance": {
    "EnableTiming": true,
    "EnableMemoryTracking": true,
    "WarnOnSlowOperationsMs": 10000
  }
}
```

### 4. Environment Variables for Production

```bash
# Production environment variables
export ASPNETCORE_ENVIRONMENT=Production
export OLLAMA_BASE_URL=http://ollama-prod:11434
export APP_MAX_JSON_SIZE_BYTES=52428800
export PERF_ENABLE_TIMING=true
export PERF_ENABLE_MEMORY_TRACKING=true
```

## Docker Deployment

### 1. Multi-Stage Dockerfile

```dockerfile
# Build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["JSON-Whisperer/JSON-Whisperer.csproj", "JSON-Whisperer/"]
RUN dotnet restore "JSON-Whisperer/JSON-Whisperer.csproj"
COPY . .
WORKDIR "/src/JSON-Whisperer"
RUN dotnet build "JSON-Whisperer.csproj" -c Release -o /app/build

# Publish stage
FROM build AS publish
RUN dotnet publish "JSON-Whisperer.csproj" -c Release -o /app/publish

# Runtime stage
FROM mcr.microsoft.com/dotnet/runtime:9.0 AS final
WORKDIR /app
COPY --from=publish /app/publish .

# Create non-root user
RUN groupadd -r appuser && useradd -r -g appuser appuser
RUN chown -R appuser:appuser /app
USER appuser

ENTRYPOINT ["dotnet", "JSON-Whisperer.dll"]
```

### 2. Docker Compose Setup

```yaml
version: '3.8'

services:
  scylla:
    image: scylladb/scylla:latest
    container_name: scylla-node1
    ports:
      - "9042:9042"
      - "9160:9160"
      - "7000:7000"
      - "7001:7001"
    volumes:
      - scylla_data:/var/lib/scylla
    environment:
      - SCYLLA_CLUSTER_NAME=json-whisperer-cluster
      - SCYLLA_DC=datacenter1
      - SCYLLA_RACK=rack1
    command: --seeds=scylla-node1 --smp 2 --memory 2G --overprovisioned 1
    healthcheck:
      test: ["CMD", "cqlsh", "-e", "describe keyspaces"]
      interval: 30s
      timeout: 10s
      retries: 5
      start_period: 60s

  ollama:
    image: ollama/ollama:latest
    ports:
      - "11434:11434"
    volumes:
      - ollama_data:/root/.ollama
    environment:
      - OLLAMA_HOST=0.0.0.0
    healthcheck:
      test: ["CMD", "curl", "-f", "http://localhost:11434/api/tags"]
      interval: 30s
      timeout: 10s
      retries: 3

  json-whisperer:
    build: .
    depends_on:
      scylla:
        condition: service_healthy
      ollama:
        condition: service_healthy
    environment:
      - OLLAMA_BASE_URL=http://ollama:11434
      - SCYLLADB_CONTACT_POINTS=scylla
      - SCYLLADB_PORT=9042
      - SCYLLADB_KEYSPACE=json_whisperer
      - VECTOR_ENABLE_SIMILARITY_MATCHING=true
      - VECTOR_SIMILARITY_THRESHOLD=0.7
      - APP_VERBOSE_MODE=false
      - PERF_ENABLE_TIMING=true
    volumes:
      - ./logs:/app/logs
      - ./AppData:/app/AppData
    stdin_open: true
    tty: true
    networks:
      - json-whisperer-network

networks:
  json-whisperer-network:
    driver: bridge

volumes:
  ollama_data:
  scylla_data:
```

### 3. Build and Run with Docker

```bash
# Build image
docker build -t json-whisperer:latest .

# Run with Docker Compose
docker-compose up -d

# Wait for services to be healthy
docker-compose ps

# Install required models in Ollama container
docker-compose exec ollama ollama pull mistral
docker-compose exec ollama ollama pull nomic-embed-text

# Verify ScyllaDB is ready
docker-compose exec scylla cqlsh -e "DESCRIBE KEYSPACES;"

# Test the application with basic JSON
echo '{"test": "data"}' | docker-compose exec -T json-whisperer dotnet JSON-Whisperer.dll

# Test with verbose mode to see similarity matching
echo '{"user": {"name": "John", "age": 30}}' | docker-compose exec -T json-whisperer dotnet JSON-Whisperer.dll --verbose

# Initialize knowledge base (if not done automatically)
docker-compose exec json-whisperer dotnet JSON-Whisperer.dll --reinitialize-knowledge-base
```

## Cloud Deployment

### 1. Azure Container Instances

```bash
# Create resource group
az group create --name json-whisperer-rg --location eastus

# Create container instance
az container create \
  --resource-group json-whisperer-rg \
  --name json-whisperer \
  --image json-whisperer:latest \
  --cpu 2 \
  --memory 4 \
  --environment-variables \
    OLLAMA_BASE_URL=http://ollama-service:11434 \
    APP_VERBOSE_MODE=false
```

### 2. AWS ECS Deployment

Create `task-definition.json`:

```json
{
  "family": "json-whisperer",
  "networkMode": "awsvpc",
  "requiresCompatibilities": ["FARGATE"],
  "cpu": "1024",
  "memory": "2048",
  "executionRoleArn": "arn:aws:iam::account:role/ecsTaskExecutionRole",
  "containerDefinitions": [
    {
      "name": "json-whisperer",
      "image": "your-account.dkr.ecr.region.amazonaws.com/json-whisperer:latest",
      "essential": true,
      "environment": [
        {"name": "OLLAMA_BASE_URL", "value": "http://ollama-service:11434"},
        {"name": "APP_VERBOSE_MODE", "value": "false"}
      ],
      "logConfiguration": {
        "logDriver": "awslogs",
        "options": {
          "awslogs-group": "/ecs/json-whisperer",
          "awslogs-region": "us-east-1",
          "awslogs-stream-prefix": "ecs"
        }
      }
    }
  ]
}
```

### 3. Kubernetes Deployment

Create `k8s-deployment.yaml`:

```yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: json-whisperer
spec:
  replicas: 2
  selector:
    matchLabels:
      app: json-whisperer
  template:
    metadata:
      labels:
        app: json-whisperer
    spec:
      containers:
      - name: json-whisperer
        image: json-whisperer:latest
        env:
        - name: OLLAMA_BASE_URL
          value: "http://ollama-service:11434"
        - name: APP_VERBOSE_MODE
          value: "false"
        - name: PERF_ENABLE_TIMING
          value: "true"
        resources:
          requests:
            memory: "1Gi"
            cpu: "500m"
          limits:
            memory: "2Gi"
            cpu: "1000m"
        livenessProbe:
          exec:
            command:
            - /bin/sh
            - -c
            - "dotnet JSON-Whisperer.dll --health-check"
          initialDelaySeconds: 30
          periodSeconds: 30
---
apiVersion: v1
kind: Service
metadata:
  name: json-whisperer-service
spec:
  selector:
    app: json-whisperer
  ports:
  - port: 80
    targetPort: 8080
```

## Vector Similarity Configuration

### 1. ScyllaDB Configuration

#### Connection Settings
```json
{
  "ScyllaDb": {
    "ContactPoints": "127.0.0.1",
    "Port": 9042,
    "Keyspace": "json_whisperer",
    "Username": "",
    "Password": "",
    "DataCenter": "datacenter1",
    "ConnectionTimeoutSeconds": 10,
    "QueryTimeoutSeconds": 30,
    "CreateKeyspaceIfNotExists": true,
    "ReplicationFactor": 1,
    "ConsistencyLevel": "LocalQuorum",
    "MaxConnectionsPerHost": 8,
    "MaxRequestsPerConnection": 32768,
    "EnableCompression": true,
    "CompressionType": "LZ4"
  }
}
```

#### Production ScyllaDB Cluster Configuration
```json
{
  "ScyllaDb": {
    "ContactPoints": "scylla-node1,scylla-node2,scylla-node3",
    "Port": 9042,
    "Keyspace": "json_whisperer_prod",
    "Username": "${SCYLLADB_USERNAME}",
    "Password": "${SCYLLADB_PASSWORD}",
    "DataCenter": "datacenter1",
    "ReplicationFactor": 3,
    "ConsistencyLevel": "Quorum",
    "MaxConnectionsPerHost": 16,
    "EnableSSL": true,
    "SSLCertificatePath": "/etc/ssl/certs/scylla-cert.pem",
    "SSLKeyPath": "/etc/ssl/private/scylla-key.pem"
  }
}
```

### 2. Vector Similarity Settings

#### Basic Configuration
```json
{
  "Vector": {
    "SimilarityThreshold": 0.7,
    "MaxSimilarResults": 5,
    "EnableSimilarityMatching": true,
    "AppDataPath": "AppData",
    "InitializeKnowledgeBase": true,
    "EmbeddingDimensions": 768,
    "BatchSize": 100,
    "CacheEmbeddings": true,
    "EmbeddingCacheExpirationHours": 24
  }
}
```

#### Advanced Vector Configuration
```json
{
  "Vector": {
    "SimilarityThreshold": 0.75,
    "MaxSimilarResults": 10,
    "EnableSimilarityMatching": true,
    "AppDataPath": "/app/data",
    "InitializeKnowledgeBase": true,
    "EmbeddingDimensions": 768,
    "BatchSize": 200,
    "CacheEmbeddings": true,
    "EmbeddingCacheExpirationHours": 48,
    "MinSimilarityScore": 0.6,
    "MaxEmbeddingRetries": 5,
    "EmbeddingRetryDelayMs": 2000,
    "EnableVectorCompression": true,
    "VectorNormalization": "L2"
  }
}
```

### 3. Knowledge Base Setup

#### Directory Structure
```
AppData/
├── examples/
│   ├── user-profile.json
│   ├── user-profile.json.description.txt
│   ├── product-catalog.json
│   ├── product-catalog.json.description.txt
│   ├── order-data.json
│   ├── order-data.json.description.txt
│   └── api-response.json
│   └── api-response.json.description.txt
```

#### Example JSON Files

**user-profile.json**
```json
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
```

**user-profile.json.description.txt**
```
User profile data containing personal information, contact details, and user preferences for a social media or e-commerce platform.
```

**product-catalog.json**
```json
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
```

**product-catalog.json.description.txt**
```
E-commerce product catalog with detailed product information including pricing, categories, and technical specifications.
```

### 4. Environment Variables for Vector Services

```bash
# ScyllaDB Configuration
export SCYLLADB_CONTACT_POINTS="scylla-node1,scylla-node2,scylla-node3"
export SCYLLADB_PORT=9042
export SCYLLADB_KEYSPACE="json_whisperer_prod"
export SCYLLADB_USERNAME="your_username"
export SCYLLADB_PASSWORD="your_password"
export SCYLLADB_DATACENTER="datacenter1"

# Vector Similarity Configuration
export VECTOR_SIMILARITY_THRESHOLD=0.75
export VECTOR_MAX_SIMILAR_RESULTS=10
export VECTOR_ENABLE_SIMILARITY_MATCHING=true
export VECTOR_APP_DATA_PATH="/app/data"
export VECTOR_EMBEDDING_DIMENSIONS=768
export VECTOR_BATCH_SIZE=200

# Ollama Embedding Configuration
export OLLAMA_EMBEDDING_MODEL="nomic-embed-text"
export OLLAMA_MAX_CONCURRENT_REQUESTS=10
```

### 5. Performance Tuning for Vector Operations

#### ScyllaDB Optimization
```json
{
  "ScyllaDb": {
    "PoolingOptions": {
      "CoreConnectionsPerHost": 4,
      "MaxConnectionsPerHost": 16,
      "MaxRequestsPerConnection": 32768,
      "NewConnectionThreshold": 1600
    },
    "EnableCompression": true,
    "CompressionType": "LZ4",
    "LoadBalancingPolicy": "DCAwareRoundRobin",
    "RetryPolicy": "DefaultRetryPolicy"
  }
}
```

#### Vector Processing Optimization
```json
{
  "Vector": {
    "BatchSize": 500,
    "CacheEmbeddings": true,
    "EmbeddingCacheExpirationHours": 72,
    "EnableVectorCompression": true,
    "VectorNormalization": "L2",
    "MaxEmbeddingRetries": 3,
    "EmbeddingRetryDelayMs": 1000
  }
}
```

## Configuration Management

### 1. Environment-Specific Configurations

```bash
# Development
export ASPNETCORE_ENVIRONMENT=Development
export OLLAMA_BASE_URL=http://localhost:11434

# Staging
export ASPNETCORE_ENVIRONMENT=Staging
export OLLAMA_BASE_URL=http://ollama-staging:11434

# Production
export ASPNETCORE_ENVIRONMENT=Production
export OLLAMA_BASE_URL=http://ollama-prod:11434
```

### 2. Configuration Validation

Add health check endpoint to validate configuration:

```bash
# Check configuration validity
dotnet JSON-Whisperer.dll --validate-config

# Check Ollama connectivity
dotnet JSON-Whisperer.dll --check-ollama

# Full health check
dotnet JSON-Whisperer.dll --health-check
```

### 3. Secrets Management

#### Azure Key Vault
```bash
# Store secrets in Azure Key Vault
az keyvault secret set --vault-name "json-whisperer-kv" --name "OllamaApiKey" --value "your-api-key"
```

#### AWS Secrets Manager
```bash
# Store secrets in AWS Secrets Manager
aws secretsmanager create-secret --name "json-whisperer/ollama" --secret-string '{"apiKey":"your-api-key"}'
```

## Monitoring and Logging

### 1. Application Insights (Azure)

Add to `appsettings.json`:

```json
{
  "ApplicationInsights": {
    "ConnectionString": "InstrumentationKey=your-key"
  },
  "Logging": {
    "ApplicationInsights": {
      "LogLevel": {
        "Default": "Information"
      }
    }
  }
}
```

### 2. CloudWatch (AWS)

Configure CloudWatch logging:

```json
{
  "Logging": {
    "AWSProvider": {
      "LogGroup": "/aws/ecs/json-whisperer",
      "Region": "us-east-1"
    }
  }
}
```

### 3. Prometheus Metrics

Add metrics endpoint for Prometheus scraping:

```csharp
// Add to Program.cs
services.AddSingleton<IMetricsLogger, PrometheusMetricsLogger>();
```

### 4. Health Checks

Configure health check endpoints:

```csharp
// Add to Program.cs
services.AddHealthChecks()
    .AddCheck<OllamaHealthCheck>("ollama")
    .AddCheck<ConfigurationHealthCheck>("configuration");
```

## Security Considerations

### 1. Network Security

- Use HTTPS for Ollama communication in production
- Implement network segmentation
- Configure firewall rules to restrict access
- Use VPN or private networks for cloud deployments

### 2. Authentication and Authorization

```bash
# Example: API key authentication for Ollama
export OLLAMA_API_KEY=your-secure-api-key
```

### 3. Input Validation

- Validate JSON size limits
- Sanitize file paths
- Implement rate limiting
- Log security events

### 4. Container Security

```dockerfile
# Use non-root user
RUN groupadd -r appuser && useradd -r -g appuser appuser
USER appuser

# Use minimal base image
FROM mcr.microsoft.com/dotnet/runtime:9.0-alpine

# Scan for vulnerabilities
RUN apk add --no-cache ca-certificates
```

## Troubleshooting

### 1. Vector Similarity Issues

#### ScyllaDB Connection Problems
```bash
# Test ScyllaDB connectivity
docker exec -it scylla-container cqlsh -e "DESCRIBE KEYSPACES;"

# Check ScyllaDB cluster status
docker exec -it scylla-container nodetool status

# Test from application
dotnet JSON-Whisperer.dll --test-scylla-connection

# Check ScyllaDB logs
docker logs scylla-container

# Common connection errors and solutions:
# Error: "No hosts available" - Check contact points and network connectivity
# Error: "Authentication failed" - Verify username/password
# Error: "Keyspace not found" - Enable CreateKeyspaceIfNotExists or create manually
```

#### Embedding Generation Issues
```bash
# Test Ollama embedding service
curl -X POST http://localhost:11434/api/embeddings \
  -H "Content-Type: application/json" \
  -d '{"model": "nomic-embed-text", "prompt": "test"}'

# Check if embedding model is installed
ollama list | grep nomic-embed-text

# Install embedding model if missing
ollama pull nomic-embed-text

# Test embedding generation from application
dotnet JSON-Whisperer.dll --test-embedding-generation

# Common embedding errors:
# Error: "Model not found" - Install nomic-embed-text model
# Error: "Timeout" - Increase Ollama timeout settings
# Error: "Out of memory" - Reduce batch size or increase system memory
```

#### Similarity Matching Problems
```bash
# Test similarity search functionality
dotnet JSON-Whisperer.dll --test-similarity-search

# Check knowledge base initialization
dotnet JSON-Whisperer.dll --validate-knowledge-base

# Verify AppData directory structure
ls -la AppData/examples/

# Common similarity issues:
# No similar results found - Check similarity threshold (lower it for testing)
# Slow similarity search - Optimize ScyllaDB configuration or reduce vector dimensions
# Memory issues during similarity - Reduce MaxSimilarResults or enable vector compression
```

#### Knowledge Base Issues
```bash
# Validate knowledge base files
find AppData/examples -name "*.json" -exec echo "Checking {}" \; -exec jq . {} \;

# Check for missing description files
for json_file in AppData/examples/*.json; do
  desc_file="${json_file}.description.txt"
  if [ ! -f "$desc_file" ]; then
    echo "Missing description file: $desc_file"
  fi
done

# Reinitialize knowledge base
dotnet JSON-Whisperer.dll --reinitialize-knowledge-base

# Common knowledge base errors:
# Invalid JSON files - Validate JSON syntax
# Missing description files - Create .description.txt files for each JSON
# Permission issues - Check file permissions in AppData directory
```

### 2. Performance Issues

#### Vector Operation Performance
```bash
# Monitor vector operation performance
dotnet JSON-Whisperer.dll --benchmark-vector-operations

# Check ScyllaDB performance metrics
docker exec -it scylla-container nodetool cfstats json_whisperer

# Monitor memory usage during vector operations
docker stats json-whisperer-container

# Performance optimization tips:
# - Increase ScyllaDB connection pool size
# - Enable vector compression for large datasets
# - Adjust batch size based on available memory
# - Use SSD storage for ScyllaDB data directory
```

#### Embedding Generation Performance
```bash
# Benchmark embedding generation
time curl -X POST http://localhost:11434/api/embeddings \
  -H "Content-Type: application/json" \
  -d '{"model": "nomic-embed-text", "prompt": "large text content here"}'

# Monitor Ollama resource usage
docker stats ollama-container

# Optimization strategies:
# - Use GPU acceleration if available
# - Increase Ollama concurrent request limits
# - Cache embeddings to avoid regeneration
# - Process embeddings in batches
```

### 3. Common Deployment Issues

#### Service Discovery Issues
```bash
# Test Ollama connectivity
curl -v http://ollama-service:11434/api/tags

# Check DNS resolution
nslookup ollama-service

# Test from application container
docker exec -it json-whisperer-container curl http://ollama:11434/api/tags
```

#### Memory Issues
```bash
# Monitor memory usage
docker stats json-whisperer-container

# Check application logs
docker logs json-whisperer-container

# Increase memory limits
docker run -m 4g json-whisperer:latest
```

#### Performance Issues
```bash
# Enable performance monitoring
export PERF_ENABLE_TIMING=true
export PERF_ENABLE_MEMORY_TRACKING=true

# Monitor Ollama performance
curl http://ollama:11434/api/ps
```

### 2. Diagnostic Commands

```bash
# Application diagnostics
dotnet JSON-Whisperer.dll --diagnostics

# Configuration validation
dotnet JSON-Whisperer.dll --validate-config

# Network connectivity test
dotnet JSON-Whisperer.dll --test-connection

# Performance benchmark
dotnet JSON-Whisperer.dll --benchmark
```

### 3. Log Analysis

```bash
# View application logs
tail -f /var/log/json-whisperer/app.log

# Search for errors
grep "ERROR" /var/log/json-whisperer/app.log

# Monitor performance metrics
grep "Performance Summary" /var/log/json-whisperer/app.log
```

## Backup and Recovery

### 1. Configuration Backup

```bash
# Backup configuration files
tar -czf config-backup-$(date +%Y%m%d).tar.gz appsettings*.json

# Backup environment variables
env | grep -E "(OLLAMA|APP|PERF)_" > env-backup-$(date +%Y%m%d).txt
```

### 2. Model Backup

```bash
# Backup Ollama models
docker exec ollama-container tar -czf /tmp/models-backup.tar.gz /root/.ollama/models

# Copy backup from container
docker cp ollama-container:/tmp/models-backup.tar.gz ./models-backup-$(date +%Y%m%d).tar.gz
```

### 3. Disaster Recovery

```bash
# Restore configuration
tar -xzf config-backup-20241102.tar.gz

# Restore environment variables
source env-backup-20241102.txt

# Restore Ollama models
docker cp models-backup-20241102.tar.gz ollama-container:/tmp/
docker exec ollama-container tar -xzf /tmp/models-backup-20241102.tar.gz -C /
```

## Performance Tuning

### 1. Application Optimization

```json
{
  "Application": {
    "MaxJsonSizeBytes": 104857600,
    "EnablePerformanceMetrics": true
  },
  "Performance": {
    "EnableTiming": true,
    "EnableMemoryTracking": true,
    "WarnOnSlowOperationsMs": 5000
  }
}
```

### 2. Ollama Optimization

```bash
# Increase Ollama memory
export OLLAMA_MAX_LOADED_MODELS=2
export OLLAMA_NUM_PARALLEL=4

# Use GPU acceleration (if available)
export OLLAMA_GPU=1
```

### 3. Container Resource Limits

```yaml
# Docker Compose resource limits
services:
  json-whisperer:
    deploy:
      resources:
        limits:
          cpus: '2.0'
          memory: 4G
        reservations:
          cpus: '1.0'
          memory: 2G
```

This deployment guide provides comprehensive instructions for deploying JSON-Whisperer in various environments, from local development to production cloud deployments.