# Docker Deployment Guide

## Prerequisites

- Docker Desktop installed (Windows/Mac) or Docker Engine + Docker Compose (Linux)
- At least 8GB RAM available for Docker
- At least 20GB free disk space

## Quick Start

### 1. Build and Run All Services

```bash
# Build all images
docker-compose build

# Start all services
docker-compose up -d

# View logs
docker-compose logs -f

# View logs for specific service
docker-compose logs -f auth-service
```

### 2. Stop All Services

```bash
# Stop all services
docker-compose down

# Stop and remove volumes (WARNING: This will delete all data)
docker-compose down -v
```

## Service Ports

| Service      | Port  | Description                    |
|-------------|-------|--------------------------------|
| Gateway     | 5010  | API Gateway (Entry point)      |
| Auth        | 5001  | Authentication Service         |
| User        | 5002  | User Management Service        |
| Order       | 5003  | Order Management Service       |
| Product     | 5004  | Product Catalog Service        |
| Payment     | 5005  | Payment Processing Service     |
| Inventory   | 5006  | Inventory Management (HTTP)    |
| Inventory   | 5106  | Inventory Management (gRPC)    |
| Discount    | 5007  | Discount & Promotion (HTTP)    |
| Discount    | 5107  | Discount & Promotion (gRPC)    |
| Notification| 5008  | Notification Service           |
| PostgreSQL  | 5432  | Database                       |
| Redis       | 6379  | Cache                          |
| RabbitMQ    | 5672  | Message Broker (AMQP)          |
| RabbitMQ UI | 15672 | Management Console             |

## Infrastructure Services

### PostgreSQL
- **Username**: `postgres`
- **Password**: `postgres`
- **Databases**: Automatically created via `scripts/init-db.sql`
  - `auth_db`
  - `user_db`
  - `product_db`
  - `order_db`
  - `payment_db`
  - `inventory_db`
  - `discount_db`
  - `notification_db`

### Redis
- **Port**: `6379`
- **Persistence**: Enabled (AOF)

### RabbitMQ
- **Username**: `admin`
- **Password**: `admin123`
- **Management UI**: http://localhost:15672

## Database Migrations

After starting services for the first time, you need to run EF Core migrations:

```bash
# Auth Service
docker exec -it ecommerce-auth dotnet ef database update

# User Service
docker exec -it ecommerce-user dotnet ef database update

# Product Service
docker exec -it ecommerce-product dotnet ef database update

# Order Service
docker exec -it ecommerce-order dotnet ef database update

# Payment Service
docker exec -it ecommerce-payment dotnet ef database update

# Inventory Service
docker exec -it ecommerce-inventory dotnet ef database update

# Discount Service
docker exec -it ecommerce-discount dotnet ef database update
```

**Alternative**: Run migrations from host machine:

```bash
# Example for Auth Service
cd service/auth/Auth.Api
dotnet ef database update

# Repeat for other services
```

## Health Checks

### Check Service Health

```bash
# Gateway
curl http://localhost:5010/health

# Auth Service
curl http://localhost:5001/health

# User Service
curl http://localhost:5002/health

# And so on...
```

### Check Infrastructure Health

```bash
# PostgreSQL
docker exec -it ecommerce-postgres pg_isready -U postgres

# Redis
docker exec -it ecommerce-redis redis-cli ping

# RabbitMQ
docker exec -it ecommerce-rabbitmq rabbitmq-diagnostics ping
```

## Useful Commands

### View Container Status
```bash
docker-compose ps
```

### View Container Resource Usage
```bash
docker stats
```

### Rebuild Specific Service
```bash
docker-compose build auth-service
docker-compose up -d auth-service
```

### Execute Commands in Container
```bash
# Open shell in container
docker exec -it ecommerce-auth /bin/sh

# Run specific command
docker exec -it ecommerce-auth dotnet --version
```

### View Logs with Timestamps
```bash
docker-compose logs -f --timestamps auth-service
```

### Clean Up Everything
```bash
# Stop and remove containers, networks, and volumes
docker-compose down -v

# Remove all unused images, containers, networks
docker system prune -a --volumes
```

## Development Workflow

### 1. Start Infrastructure Only
```bash
docker-compose up -d postgres redis rabbitmq
```

Then run services locally for faster development:
```bash
cd service/auth/Auth.Api
dotnet run
```

### 2. Hot Reload (Development)
For hot reload during development, modify the Dockerfile to use `dotnet watch`:

```dockerfile
# In runtime stage
ENTRYPOINT ["dotnet", "watch", "run", "--no-launch-profile"]
```

### 3. Environment Variables
You can override environment variables using `.env` file:

```bash
# Create .env file in project root
POSTGRES_PASSWORD=your_secure_password
JWT_SECRET=your_jwt_secret_key
RABBITMQ_PASSWORD=your_rabbitmq_password
```

## Troubleshooting

### Services Can't Connect to Database
```bash
# Check if PostgreSQL is ready
docker-compose logs postgres

# Restart database
docker-compose restart postgres

# Check database exists
docker exec -it ecommerce-postgres psql -U postgres -l
```

### Port Already in Use
```bash
# Find process using port (Windows)
netstat -ano | findstr :5010

# Find process using port (Linux/Mac)
lsof -i :5010

# Kill process or change port in docker-compose.yml
```

### Out of Memory
```bash
# Increase Docker memory limit in Docker Desktop settings
# Recommended: At least 8GB RAM

# Or reduce number of running services
docker-compose up -d postgres redis rabbitmq gateway auth-service
```

### Rebuild from Scratch
```bash
# Remove everything
docker-compose down -v
docker system prune -a --volumes

# Rebuild
docker-compose build --no-cache
docker-compose up -d
```

## Production Considerations

1. **Security**:
   - Change default passwords
   - Use secrets management (Docker Secrets, HashiCorp Vault)
   - Enable HTTPS/TLS
   - Use environment-specific configurations

2. **Performance**:
   - Use production-optimized images
   - Enable connection pooling
   - Configure resource limits
   - Use Redis clustering for cache
   - Use PostgreSQL replication

3. **Monitoring**:
   - Add health check endpoints
   - Integrate with monitoring tools (Prometheus, Grafana)
   - Set up logging aggregation (ELK Stack, Seq)
   - Use distributed tracing (Jaeger, Zipkin)

4. **Scaling**:
   - Use Docker Swarm or Kubernetes
   - Implement load balancing
   - Configure auto-scaling
   - Use separate database instances per service

## API Gateway Access

All services are accessible through the Gateway at `http://localhost:5010`:

```bash
# Example API calls
# Register
curl -X POST http://localhost:5010/api/auth/register \
  -H "Content-Type: application/json" \
  -d '{"email":"user@example.com","password":"Password123!"}'

# Login
curl -X POST http://localhost:5010/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"user@example.com","password":"Password123!"}'

# Get products (with JWT token)
curl -X GET http://localhost:5010/api/products \
  -H "Authorization: Bearer YOUR_JWT_TOKEN"
```

## Support

For issues or questions:
1. Check logs: `docker-compose logs -f [service-name]`
2. Verify service health checks
3. Review environment variables
4. Check network connectivity between services
