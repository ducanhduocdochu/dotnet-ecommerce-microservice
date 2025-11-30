# Docker Build & Run Guide

## 📋 Overview

All services use **Monorepo + Copy in Docker** approach:
- Services that use RabbitMQ → copy `shared/Shared.Messaging`
- Build context = project root
- Multi-stage build (small runtime images)

---

## 🚀 Build Commands

Run from **project root** (`D:\Microservice Ecommerce Dotnet8`):

```bash
# Gateway (no shared dependency)
docker build -f service/gateway/Gateway.Api/Dockerfile -t ecommerce-gateway .

# Auth (no shared dependency)
docker build -f service/auth/Auth.Api/Dockerfile -t ecommerce-auth .

# User (with shared)
docker build -f service/user/User.Api/Dockerfile -t ecommerce-user .

# Product (with shared)
docker build -f service/product/Product.Api/Dockerfile -t ecommerce-product .

# Order (with shared)
docker build -f service/order/Order.Api/Dockerfile -t ecommerce-order .

# Inventory (with shared)
docker build -f service/inventory/Inventory.Api/Dockerfile -t ecommerce-inventory .

# Discount (with shared)
docker build -f service/discount/Discount.Api/Dockerfile -t ecommerce-discount .

# Payment (with shared)
docker build -f service/payment/Payment.Api/Dockerfile -t ecommerce-payment .
```

---

## 🏃 Run Commands

### Prerequisites

```bash
# Start PostgreSQL
docker run -d --name ecommerce-postgres \
  -e POSTGRES_PASSWORD=postgres \
  -p 5432:5432 \
  postgres:16-alpine

# Create databases
docker exec -it ecommerce-postgres psql -U postgres -c "CREATE DATABASE auth_db;"
docker exec -it ecommerce-postgres psql -U postgres -c "CREATE DATABASE user_db;"
docker exec -it ecommerce-postgres psql -U postgres -c "CREATE DATABASE product_db;"
docker exec -it ecommerce-postgres psql -U postgres -c "CREATE DATABASE order_db;"
docker exec -it ecommerce-postgres psql -U postgres -c "CREATE DATABASE inventory_db;"
docker exec -it ecommerce-postgres psql -U postgres -c "CREATE DATABASE discount_db;"
docker exec -it ecommerce-postgres psql -U postgres -c "CREATE DATABASE payment_db;"

# Start RabbitMQ
docker run -d --name ecommerce-rabbitmq \
  -p 5672:5672 -p 15672:15672 \
  rabbitmq:3-management-alpine
```

### Run Services

```bash
# Gateway
docker run -d --name gateway -p 5010:8080 ecommerce-gateway

# Auth
docker run -d --name auth -p 5000:8080 \
  -e ConnectionStrings__DBConnectParam="Host=host.docker.internal;Database=auth_db;Username=postgres;Password=postgres" \
  ecommerce-auth

# User
docker run -d --name user -p 5001:8080 \
  -e ConnectionStrings__DBConnectParam="Host=host.docker.internal;Database=user_db;Username=postgres;Password=postgres" \
  -e RabbitMQ__HostName=host.docker.internal \
  ecommerce-user

# Product
docker run -d --name product -p 5002:8080 \
  -e ConnectionStrings__DBConnectParam="Host=host.docker.internal;Database=product_db;Username=postgres;Password=postgres" \
  -e RabbitMQ__HostName=host.docker.internal \
  ecommerce-product

# Order
docker run -d --name order -p 5003:8080 \
  -e ConnectionStrings__DBConnectParam="Host=host.docker.internal;Database=order_db;Username=postgres;Password=postgres" \
  -e RabbitMQ__HostName=host.docker.internal \
  -e ServiceUrls__Discount=http://host.docker.internal:5006 \
  -e ServiceUrls__Inventory=http://host.docker.internal:5005 \
  -e ServiceUrls__Payment=http://host.docker.internal:5007 \
  ecommerce-order

# Inventory
docker run -d --name inventory -p 5005:8080 \
  -e ConnectionStrings__DBConnectParam="Host=host.docker.internal;Database=inventory_db;Username=postgres;Password=postgres" \
  -e RabbitMQ__HostName=host.docker.internal \
  ecommerce-inventory

# Discount
docker run -d --name discount -p 5006:8080 \
  -e ConnectionStrings__DefaultConnection="Host=host.docker.internal;Database=discount_db;Username=postgres;Password=postgres" \
  -e RabbitMQ__HostName=host.docker.internal \
  ecommerce-discount

# Payment
docker run -d --name payment -p 5007:8080 \
  -e ConnectionStrings__DefaultConnection="Host=host.docker.internal;Database=payment_db;Username=postgres;Password=postgres" \
  -e RabbitMQ__HostName=host.docker.internal \
  ecommerce-payment
```

---

## 🔗 Service URLs

| Service | Port | URL |
|---------|------|-----|
| Gateway | 5010 | http://localhost:5010 |
| Auth | 5000 | http://localhost:5000 |
| User | 5001 | http://localhost:5001 |
| Product | 5002 | http://localhost:5002 |
| Order | 5003 | http://localhost:5003 |
| Inventory | 5005 | http://localhost:5005 |
| Discount | 5006 | http://localhost:5006 |
| Payment | 5007 | http://localhost:5007 |
| RabbitMQ UI | 15672 | http://localhost:15672 (guest/guest) |

---

## 🧹 Cleanup

```bash
# Stop all services
docker stop gateway auth user product order inventory discount payment
docker stop ecommerce-postgres ecommerce-rabbitmq

# Remove containers
docker rm gateway auth user product order inventory discount payment
docker rm ecommerce-postgres ecommerce-rabbitmq

# Remove images (optional)
docker rmi ecommerce-gateway ecommerce-auth ecommerce-user ecommerce-product \
  ecommerce-order ecommerce-inventory ecommerce-discount ecommerce-payment
```

---

## 📝 Notes

- **`host.docker.internal`**: Allows containers to reach host machine (PostgreSQL, RabbitMQ)
- For production, use `docker-compose` or Kubernetes with proper networking
- Images are ~200MB each (multi-stage build with aspnet:8.0 runtime)

---

## ✅ Verify Build

After building, check image sizes:

```bash
docker images | grep ecommerce
```

Expected output:
```
ecommerce-gateway     latest   abc123   2 minutes ago   220MB
ecommerce-auth        latest   def456   3 minutes ago   230MB
ecommerce-user        latest   ghi789   4 minutes ago   235MB
...
```

