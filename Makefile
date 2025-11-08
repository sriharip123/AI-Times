# JSON-Whisperer Docker Makefile

.PHONY: help setup build up down logs clean test health prod-up prod-down backup restore

# Default target
help:
	@echo "JSON-Whisperer Docker Commands:"
	@echo ""
	@echo "Setup and Build:"
	@echo "  setup     - Run initial setup script"
	@echo "  build     - Build application container"
	@echo ""
	@echo "Development:"
	@echo "  up        - Start development environment"
	@echo "  down      - Stop development environment"
	@echo "  restart   - Restart development environment"
	@echo "  logs      - View application logs"
	@echo "  shell     - Open shell in application container"
	@echo ""
	@echo "Production:"
	@echo "  prod-up   - Start production environment"
	@echo "  prod-down - Stop production environment"
	@echo "  prod-logs - View production logs"
	@echo ""
	@echo "Maintenance:"
	@echo "  health    - Check service health"
	@echo "  test      - Run application tests"
	@echo "  clean     - Clean up containers and volumes"
	@echo "  backup    - Backup data volumes"
	@echo "  restore   - Restore data from backup"
	@echo ""
	@echo "Utilities:"
	@echo "  models    - Download/update AI models"
	@echo "  db-shell  - Open ScyllaDB shell"
	@echo "  monitor   - Open monitoring dashboard"

# Setup and Build
setup:
	@echo "🚀 Running setup script..."
	@if [ -f "scripts/setup.sh" ]; then \
		chmod +x scripts/setup.sh && ./scripts/setup.sh; \
	else \
		powershell -ExecutionPolicy Bypass -File scripts/setup.ps1; \
	fi

build:
	@echo "🔨 Building application container..."
	docker-compose build json-whisperer

# Development Environment
up: setup
	@echo "🐳 Starting development environment..."
	docker-compose up -d
	@echo "✅ Services started. Use 'make logs' to view output."

down:
	@echo "🛑 Stopping development environment..."
	docker-compose down

restart: down up

logs:
	@echo "📋 Viewing application logs (Ctrl+C to exit)..."
	docker-compose logs -f json-whisperer

shell:
	@echo "🐚 Opening shell in application container..."
	docker-compose exec json-whisperer /bin/bash

# Production Environment
prod-up: setup
	@echo "🏭 Starting production environment..."
	docker-compose -f docker-compose.yml -f docker-compose.prod.yml up -d
	@echo "✅ Production services started."

prod-down:
	@echo "🛑 Stopping production environment..."
	docker-compose -f docker-compose.yml -f docker-compose.prod.yml down

prod-logs:
	@echo "📋 Viewing production logs..."
	docker-compose -f docker-compose.yml -f docker-compose.prod.yml logs -f json-whisperer

# Maintenance
health:
	@echo "🩺 Checking service health..."
	@docker-compose ps
	@echo ""
	@echo "Application health check:"
	@docker-compose exec json-whisperer dotnet JSON-Whisperer.dll --health-check || true

test:
	@echo "🧪 Running application tests..."
	docker-compose exec json-whisperer dotnet test

clean:
	@echo "🧹 Cleaning up containers and volumes..."
	docker-compose down -v --remove-orphans
	docker system prune -f
	@echo "✅ Cleanup complete."

backup:
	@echo "💾 Creating backup..."
	@mkdir -p backups
	@echo "Backing up ScyllaDB..."
	@docker-compose exec scylla nodetool snapshot json_whisperer || true
	@echo "Backing up Ollama models..."
	@docker run --rm -v json-whisperer_ollama_data:/data -v $(PWD)/backups:/backup alpine \
		tar czf /backup/ollama-models-$(shell date +%Y%m%d_%H%M%S).tar.gz -C /data . || true
	@echo "Backing up application cache..."
	@docker run --rm -v json-whisperer_json_whisperer_cache:/data -v $(PWD)/backups:/backup alpine \
		tar czf /backup/app-cache-$(shell date +%Y%m%d_%H%M%S).tar.gz -C /data . || true
	@echo "✅ Backup complete. Files saved in backups/ directory."

restore:
	@echo "📥 Restoring from backup..."
	@echo "Available backups:"
	@ls -la backups/ || echo "No backups found."
	@echo "To restore, run: make restore-file BACKUP_FILE=filename"

restore-file:
	@if [ -z "$(BACKUP_FILE)" ]; then \
		echo "❌ Please specify BACKUP_FILE=filename"; \
		exit 1; \
	fi
	@echo "📥 Restoring from $(BACKUP_FILE)..."
	@if echo "$(BACKUP_FILE)" | grep -q "ollama"; then \
		docker run --rm -v json-whisperer_ollama_data:/data -v $(PWD)/backups:/backup alpine \
			tar xzf /backup/$(BACKUP_FILE) -C /data; \
	elif echo "$(BACKUP_FILE)" | grep -q "app-cache"; then \
		docker run --rm -v json-whisperer_json_whisperer_cache:/data -v $(PWD)/backups:/backup alpine \
			tar xzf /backup/$(BACKUP_FILE) -C /data; \
	else \
		echo "❌ Unknown backup file type"; \
		exit 1; \
	fi
	@echo "✅ Restore complete."

# Utilities
models:
	@echo "📦 Downloading/updating AI models..."
	docker-compose exec ollama ollama pull mistral
	docker-compose exec ollama ollama pull nomic-embed-text
	@echo "✅ Models updated."

db-shell:
	@echo "🗄️ Opening ScyllaDB shell..."
	docker-compose exec scylla cqlsh

monitor:
	@echo "📊 Opening monitoring dashboard..."
	@echo "Prometheus: http://localhost:9090"
	@echo "Grafana: http://localhost:3000 (admin/admin123)"
	@echo "HAProxy Stats: http://localhost:8404/stats"
	@if command -v open >/dev/null 2>&1; then \
		open http://localhost:3000; \
	elif command -v xdg-open >/dev/null 2>&1; then \
		xdg-open http://localhost:3000; \
	else \
		echo "Please open http://localhost:3000 in your browser"; \
	fi

# Quick test commands
test-basic:
	@echo "🧪 Running basic functionality test..."
	@echo '{"test": "data"}' | docker-compose exec -T json-whisperer dotnet JSON-Whisperer.dll

test-verbose:
	@echo "🧪 Running verbose test with similarity matching..."
	@echo '{"user": {"name": "John", "age": 30}}' | docker-compose exec -T json-whisperer dotnet JSON-Whisperer.dll --verbose

test-similarity:
	@echo "🧪 Testing similarity matching..."
	docker-compose exec json-whisperer dotnet JSON-Whisperer.dll --test-similarity-search

# Development helpers
dev-rebuild: down build up

dev-reset: clean setup up

dev-logs-all:
	docker-compose logs -f

# Production helpers
prod-scale:
	@echo "📈 Scaling production application..."
	docker-compose -f docker-compose.yml -f docker-compose.prod.yml up -d --scale json-whisperer=3

prod-health:
	@echo "🩺 Checking production health..."
	docker-compose -f docker-compose.yml -f docker-compose.prod.yml ps
	@echo ""
	@echo "Cluster status:"
	@docker-compose -f docker-compose.yml -f docker-compose.prod.yml exec scylla-node1 nodetool status || true