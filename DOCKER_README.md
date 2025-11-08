# JSON-Whisperer Docker Setup

This directory contains Docker Compose configurations for running JSON-Whisperer with all its dependencies.

## Quick Start

### Prerequisites

- Docker Engine 20.10+
- Docker Compose 2.0+
- At least 8GB RAM available for containers
- 20GB free disk space

### Setup

1. **Run the setup script:**
   ```bash
   # Linux/macOS
   ./scripts/setup.sh
   
   # Windows PowerShell
   .\scripts\setup.ps1
   ```

2. **Start the development environment:**
   ```bash
   docker-compose up -d
   ```

3. **Wait for services to be ready:**
   ```bash
   docker-compose ps
   ```

4. **Test the application:**
   ```bash
   echo '{"test": "data"}' | docker-compose exec -T json-whisperer dotnet JSON-Whisperer.dll --verbose
   ```

## Architecture

The Docker Compose setup includes:

- **ScyllaDB**: Vector database for similarity matching
- **Ollama**: AI model service for text analysis and embeddings
- **JSON-Whisperer**: Main application
- **Ollama-Init**: One-time service to download required models

### Service Dependencies

```
json-whisperer
├── scylla (ScyllaDB)
└── ollama (AI Models)
    └── ollama-init (Model Downloader)
```

## Configuration Files

### Development Environment

- `docker-compose.yml` - Main configuration
- `docker-compose.override.yml` - Development overrides (auto-loaded)
- `Dockerfile` - Application container build

### Production Environment

- `docker-compose.prod.yml` - Production configuration with clustering
- Includes monitoring stack (Prometheus, Grafana, Loki)
- Load balancer (HAProxy)
- Multi-node ScyllaDB cluster

## Usage Examples

### Basic Commands

```bash
# Start all services
docker-compose up -d

# View logs
docker-compose logs -f json-whisperer

# Stop all services
docker-compose down

# Rebuild application
docker-compose build json-whisperer

# Scale application (production)
docker-compose -f docker-compose.yml -f docker-compose.prod.yml up -d --scale json-whisperer=3
```

### Application Usage

```bash
# Interactive mode
docker-compose exec json-whisperer dotnet JSON-Whisperer.dll

# Analyze JSON from file
echo '{"user": {"name": "John", "age": 30}}' > test.json
docker-compose exec -T json-whisperer dotnet JSON-Whisperer.dll --file /dev/stdin < test.json

# Verbose mode with similarity matching
docker-compose exec json-whisperer dotnet JSON-Whisperer.dll --verbose '{"product": {"id": 1, "name": "Widget"}}'

# Health check
docker-compose exec json-whisperer dotnet JSON-Whisperer.dll --health-check

# Reinitialize knowledge base
docker-compose exec json-whisperer dotnet JSON-Whisperer.dll --reinitialize-knowledge-base
```

### Database Operations

```bash
# Connect to ScyllaDB
docker-compose exec scylla cqlsh

# Check cluster status
docker-compose exec scylla nodetool status

# View keyspaces
docker-compose exec scylla cqlsh -e "DESCRIBE KEYSPACES;"

# Check embeddings table
docker-compose exec scylla cqlsh -e "USE json_whisperer; SELECT COUNT(*) FROM embeddings;"
```

### Ollama Operations

```bash
# List installed models
docker-compose exec ollama ollama list

# Pull additional models
docker-compose exec ollama ollama pull llama2

# Test embedding generation
curl -X POST http://localhost:11434/api/embeddings \
  -H "Content-Type: application/json" \
  -d '{"model": "nomic-embed-text", "prompt": "test"}'

# Check Ollama status
curl http://localhost:11434/api/tags
```

## Environment Variables

### Application Configuration

```bash
# Ollama Settings
OLLAMA_BASE_URL=http://ollama:11434
OLLAMA_MODEL_NAME=mistral
OLLAMA_EMBEDDING_MODEL=nomic-embed-text

# ScyllaDB Settings
SCYLLADB_CONTACT_POINTS=scylla
SCYLLADB_KEYSPACE=json_whisperer
SCYLLADB_USERNAME=
SCYLLADB_PASSWORD=

# Vector Settings
VECTOR_SIMILARITY_THRESHOLD=0.7
VECTOR_MAX_SIMILAR_RESULTS=5
VECTOR_ENABLE_SIMILARITY_MATCHING=true

# Application Settings
APPLICATION_VERBOSE_MODE=false
APPLICATION_MAX_JSON_SIZE_BYTES=10485760
```

### Resource Limits

```bash
# Memory limits
SCYLLA_MEMORY=2G
OLLAMA_MEMORY=4G
APP_MEMORY=2G

# CPU limits (Docker Compose format)
# Set in docker-compose.yml under deploy.resources
```

## Production Deployment

### Multi-Node Setup

```bash
# Start production environment with clustering
docker-compose -f docker-compose.yml -f docker-compose.prod.yml up -d

# Check cluster health
docker-compose -f docker-compose.prod.yml exec scylla-node1 nodetool status

# View application replicas
docker-compose -f docker-compose.prod.yml ps json-whisperer
```

### Monitoring

```bash
# Access monitoring services
open http://localhost:9090  # Prometheus
open http://localhost:3000  # Grafana (admin/admin123)
open http://localhost:8404  # HAProxy Stats

# View application metrics
curl http://localhost:9090/api/v1/query?query=up
```

### SSL/TLS Configuration

1. **Place SSL certificates in `ssl/` directory:**
   ```
   ssl/
   ├── cert.pem
   └── key.pem
   ```

2. **Update ScyllaDB configuration:**
   ```yaml
   environment:
     - ScyllaDb__EnableSSL=true
     - ScyllaDb__SSLCertificatePath=/etc/ssl/certs/cert.pem
     - ScyllaDb__SSLKeyPath=/etc/ssl/certs/key.pem
   ```

## Troubleshooting

### Common Issues

#### Services Not Starting

```bash
# Check service status
docker-compose ps

# View service logs
docker-compose logs scylla
docker-compose logs ollama
docker-compose logs json-whisperer

# Check resource usage
docker stats
```

#### ScyllaDB Connection Issues

```bash
# Test connectivity
docker-compose exec json-whisperer telnet scylla 9042

# Check ScyllaDB logs
docker-compose logs scylla

# Verify keyspace creation
docker-compose exec scylla cqlsh -e "DESCRIBE KEYSPACE json_whisperer;"
```

#### Ollama Model Issues

```bash
# Check model availability
docker-compose exec ollama ollama list

# Reinstall models
docker-compose exec ollama ollama pull mistral
docker-compose exec ollama ollama pull nomic-embed-text

# Test model loading
curl http://localhost:11434/api/generate -d '{"model": "mistral", "prompt": "test"}'
```

#### Application Errors

```bash
# Check application logs
docker-compose logs -f json-whisperer

# Run health check
docker-compose exec json-whisperer dotnet JSON-Whisperer.dll --health-check

# Test configuration
docker-compose exec json-whisperer dotnet JSON-Whisperer.dll --validate-config
```

### Performance Tuning

#### Memory Optimization

```yaml
# Adjust memory limits in docker-compose.yml
services:
  scylla:
    command: --memory=4G
  ollama:
    deploy:
      resources:
        limits:
          memory: 6G
  json-whisperer:
    environment:
      - Performance__MaxMemoryUsageMB=2048
```

#### CPU Optimization

```yaml
# Adjust CPU limits
services:
  scylla:
    command: --smp=4
    deploy:
      resources:
        limits:
          cpus: '4.0'
```

#### Storage Optimization

```bash
# Use SSD volumes for better performance
docker volume create --driver local \
  --opt type=none \
  --opt o=bind \
  --opt device=/path/to/ssd/storage \
  scylla_data
```

## Backup and Recovery

### Data Backup

```bash
# Backup ScyllaDB data
docker-compose exec scylla nodetool snapshot json_whisperer

# Backup Ollama models
docker run --rm -v ollama_data:/data -v $(pwd):/backup alpine \
  tar czf /backup/ollama-models-backup.tar.gz -C /data .

# Backup application data
docker run --rm -v json_whisperer_cache:/data -v $(pwd):/backup alpine \
  tar czf /backup/app-cache-backup.tar.gz -C /data .
```

### Data Recovery

```bash
# Restore ScyllaDB snapshot
docker-compose exec scylla nodetool refresh json_whisperer embeddings

# Restore Ollama models
docker run --rm -v ollama_data:/data -v $(pwd):/backup alpine \
  tar xzf /backup/ollama-models-backup.tar.gz -C /data

# Restore application cache
docker run --rm -v json_whisperer_cache:/data -v $(pwd):/backup alpine \
  tar xzf /backup/app-cache-backup.tar.gz -C /data
```

## Security Considerations

### Network Security

- Services communicate through isolated Docker network
- Only necessary ports are exposed to host
- Use environment variables for sensitive configuration

### Authentication

```bash
# Set ScyllaDB credentials
export SCYLLADB_USERNAME=your_username
export SCYLLADB_PASSWORD=your_secure_password

# Update Grafana admin password
export GRAFANA_ADMIN_PASSWORD=your_secure_password
```

### Container Security

- Application runs as non-root user
- Read-only filesystem where possible
- Minimal base images used
- Regular security updates recommended

## Development Workflow

### Code Changes

```bash
# Rebuild after code changes
docker-compose build json-whisperer

# Restart with new image
docker-compose up -d json-whisperer

# View updated logs
docker-compose logs -f json-whisperer
```

### Testing

```bash
# Run tests in container
docker-compose exec json-whisperer dotnet test

# Run specific test
docker-compose exec json-whisperer dotnet test --filter "TestName"

# Performance testing
docker-compose exec json-whisperer dotnet JSON-Whisperer.dll --benchmark-all
```

### Debugging

```bash
# Enable debug logging
docker-compose exec json-whisperer \
  env Logging__LogLevel__Default=Debug \
  dotnet JSON-Whisperer.dll --verbose '{"test": "data"}'

# Attach debugger (if configured)
docker-compose -f docker-compose.yml -f docker-compose.debug.yml up -d
```

This Docker setup provides a complete, production-ready environment for JSON-Whisperer with all necessary dependencies and monitoring capabilities.