#!/bin/bash

# JSON-Whisperer Docker Setup Script

set -e

echo "🚀 Setting up JSON-Whisperer Docker environment..."

# Create necessary directories
echo "📁 Creating directories..."
mkdir -p logs
mkdir -p AppData/examples
mkdir -p scylla-config/development
mkdir -p scylla-config/production
mkdir -p monitoring
mkdir -p haproxy
mkdir -p ssl

# Create sample AppData files if they don't exist
if [ ! -f "AppData/examples/user-profile.json" ]; then
    echo "📝 Creating sample knowledge base files..."
    
    cat > AppData/examples/user-profile.json << 'EOF'
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

    cat > AppData/examples/user-profile.json.description.txt << 'EOF'
User profile data containing personal information, contact details, and user preferences for a social media or e-commerce platform.
EOF

    cat > AppData/examples/product-catalog.json << 'EOF'
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

    cat > AppData/examples/product-catalog.json.description.txt << 'EOF'
E-commerce product catalog with detailed product information including pricing, categories, and technical specifications.
EOF

    cat > AppData/examples/api-response.json << 'EOF'
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

    cat > AppData/examples/api-response.json.description.txt << 'EOF'
Standard API response format with status indicator, data payload, and pagination metadata commonly used in REST APIs.
EOF
fi

# Create environment file
if [ ! -f ".env" ]; then
    echo "🔧 Creating environment file..."
    cat > .env << 'EOF'
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
EOF
fi

# Create basic HAProxy configuration
if [ ! -f "haproxy/haproxy.cfg" ]; then
    echo "⚖️ Creating HAProxy configuration..."
    cat > haproxy/haproxy.cfg << 'EOF'
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
EOF
fi

# Create basic Prometheus configuration
if [ ! -f "monitoring/prometheus.yml" ]; then
    echo "📊 Creating Prometheus configuration..."
    cat > monitoring/prometheus.yml << 'EOF'
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
EOF
fi

echo "✅ Setup complete!"
echo ""
echo "🐳 To start the development environment:"
echo "   docker-compose up -d"
echo ""
echo "🏭 To start the production environment:"
echo "   docker-compose -f docker-compose.yml -f docker-compose.prod.yml up -d"
echo ""
echo "🔍 To view logs:"
echo "   docker-compose logs -f json-whisperer"
echo ""
echo "🩺 To check health:"
echo "   docker-compose ps"
echo "   docker-compose exec json-whisperer dotnet JSON-Whisperer.dll --health-check"
echo ""
echo "📊 Access points:"
echo "   - Application: docker-compose exec json-whisperer dotnet JSON-Whisperer.dll"
echo "   - ScyllaDB: docker-compose exec scylla cqlsh"
echo "   - Ollama: curl http://localhost:11434/api/tags"
echo "   - Prometheus: http://localhost:9090 (production only)"
echo "   - Grafana: http://localhost:3000 (production only)"
echo ""
EOF