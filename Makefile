.PHONY: help build up down restart logs clean migrate health test

# Default target
help:
	@echo "Available commands:"
	@echo "  make build       - Build all Docker images"
	@echo "  make up          - Start all services"
	@echo "  make down        - Stop all services"
	@echo "  make restart     - Restart all services"
	@echo "  make logs        - View logs (all services)"
	@echo "  make clean       - Remove all containers, volumes, and images"
	@echo "  make migrate     - Run database migrations for all services"
	@echo "  make health      - Check health of all services"
	@echo "  make infra       - Start only infrastructure services"
	@echo "  make test        - Run tests"

# Build all images
build:
	docker-compose build

# Build without cache
build-clean:
	docker-compose build --no-cache

# Start all services
up:
	docker-compose up -d

# Start with logs
up-logs:
	docker-compose up

# Stop all services
down:
	docker-compose down

# Stop and remove volumes
down-clean:
	docker-compose down -v

# Restart all services
restart:
	docker-compose restart

# View logs
logs:
	docker-compose logs -f

# View logs for specific service
logs-auth:
	docker-compose logs -f auth-service

logs-user:
	docker-compose logs -f user-service

logs-product:
	docker-compose logs -f product-service

logs-order:
	docker-compose logs -f order-service

logs-payment:
	docker-compose logs -f payment-service

logs-inventory:
	docker-compose logs -f inventory-service

logs-discount:
	docker-compose logs -f discount-service

logs-gateway:
	docker-compose logs -f gateway

# Start only infrastructure
infra:
	docker-compose up -d postgres redis rabbitmq

# Clean everything
clean:
	docker-compose down -v
	docker system prune -a -f --volumes

# Run migrations
migrate:
	@echo "Running migrations for all services..."
	docker exec -it ecommerce-auth dotnet ef database update || true
	docker exec -it ecommerce-user dotnet ef database update || true
	docker exec -it ecommerce-product dotnet ef database update || true
	docker exec -it ecommerce-order dotnet ef database update || true
	docker exec -it ecommerce-payment dotnet ef database update || true
	docker exec -it ecommerce-inventory dotnet ef database update || true
	docker exec -it ecommerce-discount dotnet ef database update || true
	@echo "Migrations completed!"

# Check service health
health:
	@echo "Checking PostgreSQL..."
	@docker exec -it ecommerce-postgres pg_isready -U postgres || echo "PostgreSQL is not ready"
	@echo ""
	@echo "Checking Redis..."
	@docker exec -it ecommerce-redis redis-cli ping || echo "Redis is not ready"
	@echo ""
	@echo "Checking RabbitMQ..."
	@docker exec -it ecommerce-rabbitmq rabbitmq-diagnostics ping || echo "RabbitMQ is not ready"
	@echo ""
	@echo "Services status:"
	@docker-compose ps

# View service status
ps:
	docker-compose ps

# View resource usage
stats:
	docker stats

# Rebuild specific service
rebuild-auth:
	docker-compose build auth-service
	docker-compose up -d auth-service

rebuild-user:
	docker-compose build user-service
	docker-compose up -d user-service

rebuild-product:
	docker-compose build product-service
	docker-compose up -d product-service

rebuild-order:
	docker-compose build order-service
	docker-compose up -d order-service

rebuild-payment:
	docker-compose build payment-service
	docker-compose up -d payment-service

rebuild-inventory:
	docker-compose build inventory-service
	docker-compose up -d inventory-service

rebuild-discount:
	docker-compose build discount-service
	docker-compose up -d discount-service

rebuild-gateway:
	docker-compose build gateway
	docker-compose up -d gateway

# Development helpers
dev-infra:
	docker-compose up -d postgres redis rabbitmq
	@echo "Infrastructure started. You can now run services locally."

# Shell access
shell-auth:
	docker exec -it ecommerce-auth /bin/sh

shell-user:
	docker exec -it ecommerce-user /bin/sh

shell-product:
	docker exec -it ecommerce-product /bin/sh

shell-postgres:
	docker exec -it ecommerce-postgres psql -U postgres

shell-redis:
	docker exec -it ecommerce-redis redis-cli

shell-rabbitmq:
	docker exec -it ecommerce-rabbitmq /bin/sh

