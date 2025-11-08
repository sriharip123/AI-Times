# JSON-Whisperer Docker Setup Script for Windows

Write-Host "🚀 Setting up JSON-Whisperer Docker environment..." -ForegroundColor Green

# Create necessary directories
Write-Host "📁 Creating directories..." -ForegroundColor Yellow
$directories = @(
    "logs",
    "AppData/examples",
    "scylla-config/development",
    "scylla-config/production",
    "monitoring",
    "haproxy",
    "ssl"
)

foreach ($dir in $directories) {
    if (!(Test-Path $dir)) {
        New-Item -ItemType Directory -Path $dir -Force | Out-Null
        Write-Host "  Created: $dir" -ForegroundColor Gray
    }
}

# Create sample AppData files if they don't exist
if (!(Test-Path "AppData/examples/user-profile.json")) {
    Write-Host "📝 Creating sample knowledge base files..." -ForegroundColor Yellow
    
    @'
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
'@ | Out-File -FilePath "AppData/examples/user-profile.json" -Encoding UTF8

    @'
User profile data containing personal information, contact details, and user preferences for a social media or e-commerce platform.
'@ | Out-File -FilePath "AppData/examples/user-profile.json.description.txt" -Encoding UTF8

    @'
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
'@ | Out-File -FilePath "AppData/examples/product-catalog.json" -Encoding UTF8

    @'
E-commerce product catalog with detailed product information including pricing, categories, and technical specifications.
'@ | Out-File -FilePath "AppData/examples/product-catalog.json.description.txt" -Encoding UTF8

    @'
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
'@ | Out-File -FilePath "AppData/examples/api-response.json" -Encoding UTF8

    @'
Standard API response format with status indicator, data payload, and pagination metadata commonly used in REST APIs.
'@ | Out-File -FilePath "AppData/examples/api-response.json.description.txt" -Encoding UTF8
}

# Create environment file
if (!(Test-Path ".env")) {
    Write-Host "🔧 Creating environment file..." -ForegroundColor Yellow
    @'
# JSON-Whisperer Environment Configuration

# Application Environment
ASPNETCORE_ENVIRONMENT=Development

# Grafana Admin Password
GRAFANA_ADMIN_PASSWORD=admin123

# ScyllaDB Configuration
SCYLLADB_USERNAME=
SCYLLADB_PASSWORD=

# Ollama Configuration
OLLAMA_GPU=0

# Resource Limits
SCYLLA_MEMORY=2G
OLLAMA_MEMORY=4G
APP_MEMORY=2G
'@ | Out-File -FilePath ".env" -Encoding UTF8
}

# Create basic HAProxy configuration
if (!(Test-Path "haproxy/haproxy.cfg")) {
    Write-Host "⚖️ Creating HAProxy configuration..." -ForegroundColor Yellow
    @'
global
    daemon
    log stdout local0

defaults
    mode http
    timeout connect 5000ms
    timeout client 50000ms
    timeout server 50000ms
    log global
    option httplog

frontend json_whisperer_frontend
    bind *:80
    default_backend json_whisperer_backend

backend json_whisperer_backend
    balance roundrobin
    server app1 json-whisperer:8080 check
    server app2 json-whisperer:8080 check backup

listen stats
    bind *:8404
    stats enable
    stats uri /stats
    stats refresh 30s
'@ | Out-File -FilePath "haproxy/haproxy.cfg" -Encoding UTF8
}

# Create basic Prometheus configuration
if (!(Test-Path "monitoring/prometheus.yml")) {
    Write-Host "📊 Creating Prometheus configuration..." -ForegroundColor Yellow
    @'
global:
  scrape_interval: 15s
  evaluation_interval: 15s

scrape_configs:
  - job_name: 'json-whisperer'
    static_configs:
      - targets: ['json-whisperer:8080']
    scrape_interval: 30s
    metrics_path: /metrics

  - job_name: 'scylla'
    static_configs:
      - targets: ['scylla:9180']
    scrape_interval: 30s

  - job_name: 'ollama'
    static_configs:
      - targets: ['ollama:11434']
    scrape_interval: 30s
'@ | Out-File -FilePath "monitoring/prometheus.yml" -Encoding UTF8
}

Write-Host "✅ Setup complete!" -ForegroundColor Green
Write-Host ""
Write-Host "🐳 To start the development environment:" -ForegroundColor Cyan
Write-Host "   docker-compose up -d" -ForegroundColor White
Write-Host ""
Write-Host "🏭 To start the production environment:" -ForegroundColor Cyan
Write-Host "   docker-compose -f docker-compose.yml -f docker-compose.prod.yml up -d" -ForegroundColor White
Write-Host ""
Write-Host "🔍 To view logs:" -ForegroundColor Cyan
Write-Host "   docker-compose logs -f json-whisperer" -ForegroundColor White
Write-Host ""
Write-Host "🩺 To check health:" -ForegroundColor Cyan
Write-Host "   docker-compose ps" -ForegroundColor White
Write-Host "   docker-compose exec json-whisperer dotnet JSON-Whisperer.dll --health-check" -ForegroundColor White
Write-Host ""
Write-Host "📊 Access points:" -ForegroundColor Cyan
Write-Host "   - Application: docker-compose exec json-whisperer dotnet JSON-Whisperer.dll" -ForegroundColor White
Write-Host "   - ScyllaDB: docker-compose exec scylla cqlsh" -ForegroundColor White
Write-Host "   - Ollama: curl http://localhost:11434/api/tags" -ForegroundColor White
Write-Host "   - Prometheus: http://localhost:9090 (production only)" -ForegroundColor White
Write-Host "   - Grafana: http://localhost:3000 (production only)" -ForegroundColor White
Write-Host ""