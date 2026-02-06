# E-Commerce Microservices Architecture

> **Comprehensive System Architecture Documentation**  
> Version: 1.0  
> Last Updated: December 2025

---

## Table of Contents

1. [System Overview](#system-overview)
2. [Architecture Patterns](#architecture-patterns)
3. [Service Catalog](#service-catalog)
4. [Communication Patterns](#communication-patterns)
5. [Critical Business Flows](#critical-business-flows)
6. [Message Queue Architecture](#message-queue-architecture)
7. [Caching Strategy](#caching-strategy)
8. [API Architecture](#api-architecture)
9. [Security Architecture](#security-architecture)
10. [Data Architecture](#data-architecture)
11. [Deployment Architecture](#deployment-architecture)

---

## System Overview

### High-Level Architecture

```
┌─────────────────────────────────────────────────────────────────────────┐
│                           CLIENT APPLICATIONS                            │
│                     (Web, Mobile, Admin Dashboard)                       │
└────────────────────────────────┬────────────────────────────────────────┘
                                 │
                                 │ HTTPS
                                 ▼
┌─────────────────────────────────────────────────────────────────────────┐
│                          API GATEWAY (YARP)                              │
│          - Reverse Proxy    - Rate Limiting    - JWT Validation         │
│          - Load Balancing   - Request Routing  - CORS                   │
└────┬────────┬────────┬────────┬────────┬────────┬────────┬──────────┬───┘
     │        │        │        │        │        │        │          │
     │        │        │        │        │        │        │          │
┌────▼───┐ ┌─▼────┐ ┌─▼─────┐ ┌▼─────┐ ┌▼──────┐ ┌▼──────┐ ┌▼────────┐ ┌▼─────────┐
│  Auth  │ │ User │ │Product│ │Order │ │Payment│ │Invent.│ │Discount│ │Notifica. │
│Service │ │Service│ │Service│ │Service│ │Service│ │Service│ │Service │ │ Service  │
└────┬───┘ └─┬────┘ └─┬─────┘ └┬─────┘ └┬──────┘ └┬──────┘ └┬───────┘ └┬─────────┘
     │       │        │         │        │         │         │          │
     │       │        │         │        │         │         │          │
     │       │        │         │        └────┬────┘         │          │
     │       │        │         │             │              │          │
     │       │        │         └─────gRPC────┴──────gRPC────┘          │
     │       │        │                       │                         │
┌────┴───────┴────────┴───────────────────────┴─────────────────────────┴─────┐
│                          INFRASTRUCTURE LAYER                                │
│  ┌──────────┐    ┌───────────┐    ┌─────────────┐    ┌────────────────┐   │
│  │PostgreSQL│    │   Redis   │    │  RabbitMQ   │    │  Shared Libs   │   │
│  │(Per DB)  │    │  (Cache)  │    │ (Messaging) │    │  (Protos/Msg)  │   │
│  └──────────┘    └───────────┘    └─────────────┘    └────────────────┘   │
└──────────────────────────────────────────────────────────────────────────────┘
```

### Key Characteristics

- **Architecture Style**: Microservices
- **Communication**: REST (sync), gRPC (sync), RabbitMQ (async)
- **Data**: Database per service (PostgreSQL)
- **Caching**: Distributed (Redis)
- **API Gateway**: YARP Reverse Proxy
- **Auth**: JWT Bearer Token
- **Deployment**: Docker Containers + Docker Compose

---

## Architecture Patterns

### 1. Microservices Pattern
Each service owns its:
- Database
- Business logic
- API endpoints
- Data models

### 2. API Gateway Pattern
Single entry point for all client requests:
- Route requests to appropriate services
- Handle cross-cutting concerns (auth, logging, rate limiting)
- Protocol translation

### 3. Database per Service
Each microservice has its own database:
- Strong data isolation
- Independent scaling
- Technology flexibility
- Prevents tight coupling

### 4. Event-Driven Architecture
Services communicate via events:
- Loose coupling
- Asynchronous processing
- Event sourcing for audit trails
- Eventual consistency

### 5. CQRS (Command Query Responsibility Segregation)
Separate read and write operations:
- Optimized queries with caching
- Complex write validation
- Different data models for reads/writes

### 6. Saga Pattern
Distributed transactions across services:
- Orchestration-based (Order Service as orchestrator)
- Compensating transactions
- Event-driven coordination

---

## Service Catalog

### Core Services

| Service | Port | Database | Role | Dependencies |
|---------|------|----------|------|--------------|
| **Gateway** | 5010 | - | API Gateway, Reverse Proxy | All services |
| **Auth** | 5001 | auth_db | Authentication, Authorization | PostgreSQL |
| **User** | 5002 | user_db | User profiles, addresses, roles | PostgreSQL, Redis, RabbitMQ |
| **Product** | 5004 | product_db | Product catalog, categories, brands | PostgreSQL, Redis, RabbitMQ |
| **Order** | 5003 | order_db | Orders, cart, order orchestration | PostgreSQL, RabbitMQ, Inventory (gRPC), Discount (gRPC) |
| **Payment** | 5005 | payment_db | Payment processing, transactions | PostgreSQL, RabbitMQ |
| **Inventory** | 5006/5106 | inventory_db | Stock management, reservations | PostgreSQL, Redis, RabbitMQ |
| **Discount** | 5007/5107 | discount_db | Coupons, promotions, campaigns | PostgreSQL, RabbitMQ |
| **Notification** | 5008 | notification_db | Email, SMS, push notifications | RabbitMQ |

### Service Responsibilities

#### Auth Service
```
Responsibilities:
✓ User registration & login
✓ JWT token generation & validation
✓ Password hashing & verification
✓ Email verification
✓ Role & permission management
✓ Token refresh

Technologies:
- ASP.NET Core Minimal API
- PostgreSQL (user credentials, roles, permissions)
- BCrypt (password hashing)
- JWT Bearer authentication
```

#### User Service
```
Responsibilities:
✓ User profile management
✓ Address management
✓ User preferences
✓ Avatar upload
✓ User search & filtering
✓ Publish user update events

Technologies:
- ASP.NET Core Minimal API
- PostgreSQL (user profiles, addresses)
- Redis (user profile cache)
- RabbitMQ (user events)
```

#### Product Service
```
Responsibilities:
✓ Product catalog management
✓ Category hierarchy
✓ Brand management
✓ Product variants & attributes
✓ Product search & filtering
✓ Image management

Technologies:
- ASP.NET Core Minimal API
- PostgreSQL (products, categories, brands)
- Redis (product cache, category cache)
```

#### Order Service (Orchestrator)
```
Responsibilities:
✓ Shopping cart management
✓ Order creation & tracking
✓ Order status workflow
✓ Checkout orchestration
✓ Saga coordination
✓ Inventory check via gRPC
✓ Discount validation via gRPC

Technologies:
- ASP.NET Core Minimal API
- PostgreSQL (orders, cart items)
- RabbitMQ (order events, saga coordination)
- gRPC Client (Inventory, Discount)
```

#### Payment Service
```
Responsibilities:
✓ Payment processing
✓ Payment gateway integration (VNPay, Momo, ZaloPay)
✓ Transaction recording
✓ Payment callback handling
✓ Refund processing
✓ Publish payment events

Technologies:
- ASP.NET Core Minimal API
- PostgreSQL (payments, transactions)
- RabbitMQ (payment events)
- External Payment Gateways
```

#### Inventory Service
```
Responsibilities:
✓ Stock level tracking
✓ Stock reservation (temporary hold)
✓ Stock commit (confirm reservation)
✓ Stock release (cancel reservation)
✓ Low stock alerts
✓ gRPC server for sync operations

Technologies:
- ASP.NET Core Minimal API + gRPC
- PostgreSQL (inventory, reservations)
- Redis (stock cache)
- RabbitMQ (inventory events)
```

#### Discount Service
```
Responsibilities:
✓ Coupon management
✓ Discount validation
✓ Usage tracking
✓ Campaign management
✓ Auto-apply rules
✓ gRPC server for validation

Technologies:
- ASP.NET Core Minimal API + gRPC
- PostgreSQL (discounts, usage history)
- RabbitMQ (discount events)
```

#### Notification Service
```
Responsibilities:
✓ Email notifications
✓ SMS notifications
✓ Push notifications
✓ Template management
✓ Notification queue processing
✓ Delivery status tracking

Technologies:
- ASP.NET Core Minimal API
- RabbitMQ (notification events)
- SMTP (email)
- SMS Gateway
```

---

## Communication Patterns

### 1. REST API (Synchronous)

```
Client/Gateway → Service (HTTP/HTTPS)

Use Cases:
- CRUD operations
- Queries with immediate response
- User-facing operations
- Admin operations

Example Flow:
┌────────┐    GET /api/products    ┌─────────┐
│ Client │ ────────────────────────> │ Product │
│        │ <────────────────────── │ Service │
└────────┘    200 OK + JSON        └─────────┘
```

**Advantages:**
- Simple, widely supported
- Stateless
- Cacheable
- Human-readable

**Disadvantages:**
- Over-fetching/Under-fetching
- Multiple round trips
- Network latency

### 2. gRPC (Synchronous)

```
Service → Service (HTTP/2 + Protocol Buffers)

Use Cases:
- Service-to-service communication
- High-performance requirements
- Strongly-typed contracts
- Real-time validation

Example Flow:
┌───────┐  CheckStockRequest  ┌───────────┐
│ Order │ ───────────────────> │ Inventory │
│Service│ <─────────────────── │  Service  │
└───────┘  CheckStockResponse  └───────────┘
```

**Advantages:**
- High performance (binary protocol)
- Strong typing
- Bi-directional streaming
- Code generation

**Disadvantages:**
- Complex debugging
- Limited browser support
- Requires HTTP/2

**Current gRPC Implementations:**

```protobuf
// Inventory Service
service InventoryService {
  rpc CheckStock(CheckStockRequest) returns (CheckStockResponse);
  rpc ReserveStock(ReserveStockRequest) returns (ReserveStockResponse);
  rpc ReleaseStock(ReleaseStockRequest) returns (ReleaseStockResponse);
  rpc CommitStock(CommitStockRequest) returns (CommitStockResponse);
  rpc GetInventory(GetInventoryRequest) returns (GetInventoryResponse);
}

// Discount Service
service DiscountService {
  rpc ValidateDiscount(ValidateDiscountRequest) returns (ValidateDiscountResponse);
  rpc ApplyDiscount(ApplyDiscountRequest) returns (ApplyDiscountResponse);
}
```

### 3. Message Queue (Asynchronous)

```
Publisher → RabbitMQ → Consumer(s)

Use Cases:
- Event broadcasting
- Background processing
- Decoupled communication
- Eventually consistent operations

Example Flow:
┌─────────┐  OrderCreatedEvent  ┌──────────┐
│ Order   │ ──────────────────> │ RabbitMQ │
│ Service │                     │          │
└─────────┘                     └────┬─────┘
                                     │
                   ┌─────────────────┼─────────────────┐
                   │                 │                 │
                   ▼                 ▼                 ▼
              ┌─────────┐      ┌─────────┐      ┌──────────┐
              │Inventory│      │ Payment │      │Notifica. │
              │ Service │      │ Service │      │ Service  │
              └─────────┘      └─────────┘      └──────────┘
```

**Advantages:**
- Loose coupling
- Async processing
- Load leveling
- Fault tolerance

**Disadvantages:**
- Eventual consistency
- Complex debugging
- Message ordering challenges
- Duplicate message handling

---

## Critical Business Flows

### Flow 1: User Registration & Authentication

```
┌──────┐                                   ┌──────┐                 ┌──────┐
│Client│                                   │ Auth │                 │ User │
└──┬───┘                                   └──┬───┘                 └──┬───┘
   │                                          │                        │
   │ 1. POST /api/auth/register               │                        │
   ├─────────────────────────────────────────>│                        │
   │    {email, password, fullName}           │                        │
   │                                          │                        │
   │                                          │ 2. Hash password       │
   │                                          │    Generate verification│
   │                                          │    token               │
   │                                          │                        │
   │                                          │ 3. Save to auth_db     │
   │                                          │                        │
   │ 4. 201 Created                           │                        │
   │<─────────────────────────────────────────┤                        │
   │    {userId, message}                     │                        │
   │                                          │                        │
   │ 5. POST /api/auth/verify-email           │                        │
   ├─────────────────────────────────────────>│                        │
   │    {email, token}                        │                        │
   │                                          │                        │
   │                                          │ 6. Verify token        │
   │                                          │    Update status       │
   │                                          │                        │
   │                                          │ 7. Publish UserVerifiedEvent
   │                                          │───────────────────────>│
   │                                          │    (RabbitMQ)          │
   │                                          │                        │
   │                                          │                        │ 8. Create profile
   │                                          │                        │    in user_db
   │ 9. 200 OK                                │                        │
   │<─────────────────────────────────────────┤                        │
   │                                          │                        │
   │ 10. POST /api/auth/login                 │                        │
   ├─────────────────────────────────────────>│                        │
   │     {email, password}                    │                        │
   │                                          │                        │
   │                                          │ 11. Verify password    │
   │                                          │     Generate JWT       │
   │                                          │     (access + refresh) │
   │                                          │                        │
   │ 12. 200 OK                               │                        │
   │<─────────────────────────────────────────┤                        │
   │     {accessToken, refreshToken}          │                        │
   │                                          │                        │
```

**Key Points:**
- Password hashed with BCrypt
- Email verification required before login
- JWT token with 60 minutes expiry
- Refresh token for extended sessions
- User profile created asynchronously

---

### Flow 2: Order Checkout (Saga Pattern)

```
┌──────┐  ┌───────┐  ┌─────────┐  ┌─────────┐  ┌─────────┐  ┌─────────┐
│Client│  │Gateway│  │  Order  │  │Inventory│  │Discount │  │ Payment │
└──┬───┘  └───┬───┘  └────┬────┘  └────┬────┘  └────┬────┘  └────┬────┘
   │          │           │              │            │            │
   │ 1. POST /api/orders/checkout         │            │            │
   ├─────────>├──────────>│              │            │            │
   │          │           │              │            │            │
   │          │           │ 2. Validate Discount (gRPC)            │
   │          │           │──────────────────────────>│            │
   │          │           │<──────────────────────────┤            │
   │          │           │   {valid, discountAmount} │            │
   │          │           │              │            │            │
   │          │           │ 3. Check & Reserve Stock (gRPC)        │
   │          │           │──────────────>│            │            │
   │          │           │<──────────────┤            │            │
   │          │           │   {reserved}  │            │            │
   │          │           │              │            │            │
   │          │           │ 4. Create Order (PENDING)│            │
   │          │           │    Save to order_db      │            │
   │          │           │              │            │            │
   │          │           │ 5. Publish OrderCreatedEvent           │
   │          │           │══════════════════════════════════════> │
   │          │           │              (RabbitMQ)                │
   │          │           │              │            │            │
   │          │           │              │            │            │ 6. Create payment
   │          │           │              │            │            │    Generate URL
   │          │           │              │            │            │
   │ 7. 200 OK (paymentUrl)              │            │            │
   │<─────────┤<──────────┤              │            │            │
   │          │           │              │            │            │
   │ 8. User completes payment            │            │            │
   │────────────────────────────────────────────────────────────> │
   │          │           │              │            │            │
   │          │           │              │            │            │ 9. Publish
   │          │           │<══════════════════════════════════════ │  PaymentSuccessEvent
   │          │           │              │            │            │
   │          │           │ 10. Update Order (CONFIRMED)           │
   │          │           │              │            │            │
   │          │           │ 11. Publish OrderConfirmedEvent        │
   │          │           │══════════════>│            │            │
   │          │           │═════════════════════════> │            │
   │          │           │              │            │            │
   │          │           │              │ 12. Commit │ 13. Record │
   │          │           │              │    Stock   │     Usage  │
   │          │           │              │            │            │
```

**Saga Steps:**
1. **Validate Discount** (gRPC to Discount Service)
2. **Reserve Stock** (gRPC to Inventory Service)
3. **Create Order** (PENDING status)
4. **Initiate Payment** (Event to Payment Service)
5. **Confirm or Cancel** based on payment result

**Compensating Actions:**
- Payment Failed → Release Stock → Cancel Order
- Inventory Insufficient → Cancel Order → Notify User

---

### Flow 3: Payment Success/Failure Handling

#### Success Flow:
```
┌─────────┐          ┌───────┐          ┌───────────┐          ┌──────────┐
│ Payment │          │RabbitMQ          │   Order   │          │Inventory │
│ Service │          │       │          │  Service  │          │ Service  │
└────┬────┘          └───┬───┘          └─────┬─────┘          └────┬─────┘
     │                   │                    │                     │
     │ 1. PaymentSuccessEvent                │                     │
     ├──────────────────>│                    │                     │
     │                   │                    │                     │
     │                   │ 2. Consume Event   │                     │
     │                   │───────────────────>│                     │
     │                   │                    │                     │
     │                   │                    │ 3. Update Order     │
     │                   │                    │    (CONFIRMED)      │
     │                   │                    │                     │
     │                   │                    │ 4. OrderConfirmedEvent
     │                   │<───────────────────┤                     │
     │                   │                    │                     │
     │                   │ 5. Route to Inventory                    │
     │                   │────────────────────────────────────────> │
     │                   │                    │                     │
     │                   │                    │                     │ 6. Commit
     │                   │                    │                     │    Reserved
     │                   │                    │                     │    Stock
```

#### Failure Flow:
```
┌─────────┐          ┌───────┐          ┌───────────┐          ┌──────────┐
│ Payment │          │RabbitMQ          │   Order   │          │Inventory │
│ Service │          │       │          │  Service  │          │ Service  │
└────┬────┘          └───┬───┘          └─────┬─────┘          └────┬─────┘
     │                   │                    │                     │
     │ 1. PaymentFailedEvent                 │                     │
     ├──────────────────>│                    │                     │
     │                   │                    │                     │
     │                   │ 2. Consume Event   │                     │
     │                   │───────────────────>│                     │
     │                   │                    │                     │
     │                   │                    │ 3. Update Order     │
     │                   │                    │    (FAILED)         │
     │                   │                    │                     │
     │                   │                    │ 4. OrderCancelledEvent
     │                   │<───────────────────┤                     │
     │                   │                    │                     │
     │                   │ 5. Route to Inventory                    │
     │                   │────────────────────────────────────────> │
     │                   │                    │                     │
     │                   │                    │                     │ 6. Release
     │                   │                    │                     │    Reserved
     │                   │                    │                     │    Stock
```

---

## Message Queue Architecture

### RabbitMQ Exchange & Queue Topology

```
                          ┌─────────────────────────────────────┐
                          │         RabbitMQ Broker             │
                          │                                     │
┌─────────────────────────┼─────────────────────────────────────┼─────────────┐
│                         │         EXCHANGES                   │             │
│  ┌──────────────────────┴──────────────────────────┐         │             │
│  │                                                  │         │             │
│  │  ┌──────────────────┐  ┌──────────────────┐   │         │             │
│  │  │  order.exchange  │  │ payment.exchange │   │         │             │
│  │  │    (topic)       │  │    (topic)       │   │         │             │
│  │  └────────┬─────────┘  └────────┬─────────┘   │         │             │
│  │           │                     │              │         │             │
│  │  ┌────────┴───────┐   ┌─────────┴──────────┐  │         │             │
│  │  │inventory.exchange│  │notification.exchange│ │         │             │
│  │  │    (topic)       │  │    (topic)        │  │         │             │
│  │  └────────┬─────────┘  └─────────┬──────────┘ │         │             │
│  │           │                      │             │         │             │
│  └───────────┼──────────────────────┼─────────────┘         │             │
│              │                      │                       │             │
│  ┌───────────┼──────────────────────┼───────────────────────┼───────────┐ │
│  │           │        QUEUES        │                       │           │ │
│  │           │                      │                       │           │ │
│  │  ┌────────▼─────────┐   ┌───────▼────────┐   ┌─────────▼────────┐  │ │
│  │  │ order.created    │   │ order.confirmed│   │ order.cancelled  │  │ │
│  │  │    queue         │   │    queue       │   │    queue         │  │ │
│  │  └────────┬─────────┘   └───────┬────────┘   └─────────┬────────┘  │ │
│  │           │                     │                      │           │ │
│  │  ┌────────▼──────────┐  ┌───────▼────────┐   ┌────────▼─────────┐ │ │
│  │  │ payment.success   │  │ payment.failed │   │inventory.low     │ │ │
│  │  │    queue          │  │    queue       │   │   stock.queue    │ │ │
│  │  └───────────────────┘  └────────────────┘   └──────────────────┘ │ │
│  │                                                                    │ │
│  └────────────────────────────────────────────────────────────────────┘ │
└──────────────────────────────────────────────────────────────────────────┘
```

### Event Catalog

#### Order Events
```csharp
// order.exchange (topic)

OrderCreatedEvent
├─ Routing Key: order.created
├─ Publishers: Order Service
├─ Consumers: Payment Service
└─ Payload: { OrderId, UserId, TotalAmount, Items[] }

OrderConfirmedEvent
├─ Routing Key: order.confirmed
├─ Publishers: Order Service
├─ Consumers: Inventory Service, Discount Service, Notification Service
└─ Payload: { OrderId, Items[], DiscountCode }

OrderCancelledEvent
├─ Routing Key: order.cancelled
├─ Publishers: Order Service
├─ Consumers: Inventory Service, Discount Service, Notification Service
└─ Payload: { OrderId, Reason, Items[] }
```

#### Payment Events
```csharp
// payment.exchange (topic)

PaymentSuccessEvent
├─ Routing Key: payment.success
├─ Publishers: Payment Service
├─ Consumers: Order Service, Notification Service
└─ Payload: { PaymentId, OrderId, Amount, TransactionId }

PaymentFailedEvent
├─ Routing Key: payment.failed
├─ Publishers: Payment Service
├─ Consumers: Order Service, Notification Service
└─ Payload: { PaymentId, OrderId, Reason, ErrorCode }
```

#### Inventory Events
```csharp
// inventory.exchange (topic)

StockReservedEvent
├─ Routing Key: inventory.reserved
├─ Publishers: Inventory Service
├─ Consumers: Order Service
└─ Payload: { ReservationId, ProductId, Quantity }

StockReleasedEvent
├─ Routing Key: inventory.released
├─ Publishers: Inventory Service
├─ Consumers: Notification Service
└─ Payload: { ProductId, Quantity, Reason }

LowStockAlertEvent
├─ Routing Key: inventory.low-stock
├─ Publishers: Inventory Service
├─ Consumers: Notification Service
└─ Payload: { ProductId, CurrentStock, Threshold }
```

#### User Events
```csharp
// user.exchange (topic)

UserProfileUpdatedEvent
├─ Routing Key: user.profile.updated
├─ Publishers: User Service
├─ Consumers: Product Service (cache invalidation)
└─ Payload: { UserId, UpdatedFields[] }
```

### Message Queue Patterns

#### 1. Publish-Subscribe Pattern
```
One publisher → Multiple consumers

Example: OrderConfirmedEvent
Publisher: Order Service
Subscribers:
  - Inventory Service (commit stock)
  - Discount Service (record usage)
  - Notification Service (send email)
```

#### 2. Work Queue Pattern
```
Multiple workers process tasks from shared queue

Example: Email sending queue
Publisher: Notification Service
Workers: Multiple notification worker instances
```

#### 3. Request-Reply Pattern
```
Synchronous-like communication via async messages

Alternative to gRPC for non-critical operations
Uses correlation ID to match responses
```

### Event Consumer Implementation

```csharp
// Base Consumer Class (Shared.Messaging)
public abstract class EventConsumer<TEvent> : BackgroundService
{
    protected readonly IConnection _connection;
    protected readonly ILogger _logger;
    protected readonly string _exchange;
    protected readonly string _queue;
    protected readonly string _routingKey;

    protected abstract Task HandleAsync(TEvent message, CancellationToken cancellationToken);
}

// Example: OrderConfirmedConsumer in Inventory Service
public class OrderConfirmedConsumer : EventConsumer<OrderConfirmedEvent>
{
    private readonly IInventoryService _inventoryService;

    public OrderConfirmedConsumer(
        IConnection connection,
        ILogger<OrderConfirmedConsumer> logger,
        IInventoryService inventoryService)
        : base(connection, logger, "order.exchange", "inventory.order.confirmed.queue", "order.confirmed")
    {
        _inventoryService = inventoryService;
    }

    protected override async Task HandleAsync(OrderConfirmedEvent message, CancellationToken cancellationToken)
    {
        // Commit reserved stock
        foreach (var item in message.Items)
        {
            await _inventoryService.CommitStockAsync(item.ProductId, item.Quantity);
        }
    }
}
```

### Dead Letter Queue (DLQ) Strategy

```
┌─────────────┐
│   Message   │
└──────┬──────┘
       │
       ▼
┌─────────────┐
│  Consumer   │──── Retry (3 times)
└──────┬──────┘
       │
       │ Failed after retries
       ▼
┌─────────────┐
│ Dead Letter │──── Manual investigation
│    Queue    │     & reprocessing
└─────────────┘
```

Configuration:
- Max retries: 3
- Retry delay: Exponential backoff (1s, 2s, 4s)
- DLQ retention: 7 days
- Alert on DLQ messages

---

## Caching Strategy

### Redis Architecture

```
┌──────────────────────────────────────────────────────────────┐
│                    Redis (Single Instance)                    │
│                       Port: 6379                              │
├──────────────────────────────────────────────────────────────┤
│                                                              │
│  ┌────────────────────┐  ┌────────────────────┐            │
│  │   User Service     │  │  Product Service   │            │
│  │   Cache Keys       │  │   Cache Keys       │            │
│  ├────────────────────┤  ├────────────────────┤            │
│  │ user:profile:{id}  │  │ product:{id}       │            │
│  │ user:address:{id}  │  │ category:{id}      │            │
│  │ user:list:{page}   │  │ brand:{id}         │            │
│  │ TTL: 30 min        │  │ product:list:{q}   │            │
│  └────────────────────┘  │ category:tree      │            │
│                          │ TTL: 15-60 min     │            │
│                          └────────────────────┘            │
│                                                              │
│  ┌────────────────────────────────────────────┐            │
│  │         Inventory Service Cache Keys        │            │
│  ├────────────────────────────────────────────┤            │
│  │ inventory:stock:{productId}                │            │
│  │ inventory:reservation:{id}                 │            │
│  │ TTL: 5 min (hot data)                      │            │
│  └────────────────────────────────────────────┘            │
│                                                              │
└──────────────────────────────────────────────────────────────┘
```

### Cache Patterns

#### 1. Cache-Aside Pattern (Lazy Loading)
```csharp
public async Task<ProductResponse> GetProductAsync(Guid id)
{
    // 1. Try cache first
    var cached = await _cache.GetAsync<ProductResponse>($"product:{id}");
    if (cached != null)
        return cached;

    // 2. Cache miss - fetch from database
    var product = await _repository.GetByIdAsync(id);
    
    // 3. Store in cache
    await _cache.SetAsync($"product:{id}", product, TimeSpan.FromMinutes(30));
    
    return product;
}
```

#### 2. Write-Through Pattern
```csharp
public async Task<ProductResponse> UpdateProductAsync(Guid id, UpdateProductRequest request)
{
    // 1. Update database
    var product = await _repository.UpdateAsync(id, request);
    
    // 2. Update cache immediately
    await _cache.SetAsync($"product:{id}", product, TimeSpan.FromMinutes(30));
    
    return product;
}
```

#### 3. Cache Invalidation Pattern
```csharp
public async Task DeleteProductAsync(Guid id)
{
    // 1. Delete from database
    await _repository.DeleteAsync(id);
    
    // 2. Remove from cache
    await _cache.RemoveAsync($"product:{id}");
    
    // 3. Invalidate related caches
    await _cache.RemoveByPrefixAsync("product:list");
    await _cache.RemoveByPrefixAsync($"category:{product.CategoryId}");
}
```

### Cache Key Conventions

```
Format: {service}:{entity}:{identifier}:{scope}

Examples:
- user:profile:123e4567-e89b-12d3-a456-426614174000
- product:list:page:1:size:20:search:iphone
- inventory:stock:product:abc-123
- category:tree:all
```

### TTL Strategy

| Data Type | TTL | Reasoning |
|-----------|-----|-----------|
| User Profile | 30 min | Moderate change frequency |
| Product Details | 60 min | Low change frequency |
| Product List | 15 min | Frequent updates |
| Category Tree | 120 min | Rare changes |
| Inventory Stock | 5 min | Critical, frequent changes |
| Cart | Session | User-specific, volatile |

### Cache Eviction Strategies

1. **Time-based (TTL)**: Automatic expiration
2. **Event-based**: Invalidate on relevant events
3. **Prefix-based**: Bulk invalidation for related keys
4. **Manual**: Admin API for cache management

### Distributed Cache Service Interface

```csharp
public interface ICacheService
{
    Task<T?> GetAsync<T>(string key);
    Task SetAsync<T>(string key, T value, TimeSpan? expiry = null);
    Task RemoveAsync(string key);
    Task RemoveByPrefixAsync(string prefix);
    Task<bool> ExistsAsync(string key);
    Task RefreshAsync(string key);
}
```

---

## API Architecture

### REST API Design

#### Endpoint Conventions
```
HTTP Method + Resource Path + Query Parameters

GET    /api/products              - List all products
GET    /api/products/{id}         - Get specific product
POST   /api/products              - Create product
PUT    /api/products/{id}         - Update product (full)
PATCH  /api/products/{id}         - Update product (partial)
DELETE /api/products/{id}         - Delete product

GET    /api/products/{id}/reviews - Get product reviews (nested)
POST   /api/products/{id}/reviews - Add review
```

#### Query Parameters
```
Filtering:  ?status=active&category=electronics
Sorting:    ?sortBy=price&sortOrder=desc
Pagination: ?page=1&pageSize=20
Search:     ?search=iphone
Fields:     ?fields=id,name,price
```

#### Request/Response Format
```json
// Request Body (POST/PUT)
{
  "name": "iPhone 15 Pro",
  "price": 29990000,
  "categoryId": "uuid"
}

// Success Response (200/201)
{
  "id": "uuid",
  "name": "iPhone 15 Pro",
  "price": 29990000,
  "createdAt": "2024-01-01T00:00:00Z"
}

// Error Response (400/404/500)
{
  "error": "ValidationError",
  "message": "Price must be greater than 0",
  "details": [
    {
      "field": "price",
      "message": "Invalid value"
    }
  ]
}

// Paginated Response
{
  "items": [...],
  "page": 1,
  "pageSize": 20,
  "totalCount": 150,
  "totalPages": 8
}
```

#### HTTP Status Codes
```
Success:
- 200 OK              - Successful GET/PUT/PATCH
- 201 Created         - Successful POST
- 204 No Content      - Successful DELETE

Client Errors:
- 400 Bad Request     - Invalid input
- 401 Unauthorized    - Missing/invalid token
- 403 Forbidden       - Insufficient permissions
- 404 Not Found       - Resource not found
- 409 Conflict        - Duplicate/conflict

Server Errors:
- 500 Internal Error  - Server error
- 503 Service Unavailable - Dependency failure
```

### API Gateway Routing

```yaml
# YARP Configuration (appsettings.json)
ReverseProxy:
  Routes:
    # Auth Routes
    auth-route:
      ClusterId: "auth"
      Match:
        Path: "/api/auth/{**catch-all}"
      
    # User Routes
    user-route:
      ClusterId: "user"
      Match:
        Path: "/api/users/{**catch-all}"
      AuthorizationPolicy: "authenticated"
      
    # Product Routes
    product-route:
      ClusterId: "product"
      Match:
        Path: "/api/products/{**catch-all}"
      
  Clusters:
    auth:
      Destinations:
        destination1:
          Address: "http://auth-service:8080"
    
    user:
      Destinations:
        destination1:
          Address: "http://user-service:8080"
```

### Cross-Cutting Concerns

#### 1. Authentication Middleware
```csharp
// Validate JWT token
// Extract user claims (userId, roles)
// Set HttpContext.User
```

#### 2. Authorization Policies
```csharp
// Role-based: [Authorize(Roles = "Admin")]
// Policy-based: [Authorize(Policy = "SellerOnly")]
// Resource-based: Check ownership
```

#### 3. Logging & Correlation
```csharp
// Request logging
// Correlation ID tracking
// Performance metrics
```

#### 4. Rate Limiting
```csharp
// Per user: 100 requests/minute
// Per IP: 1000 requests/hour
// Configurable per endpoint
```

---

## Security Architecture

### Authentication Flow

```
┌──────┐                ┌──────────┐                ┌──────────┐
│Client│                │ Gateway  │                │   Auth   │
└──┬───┘                └────┬─────┘                └────┬─────┘
   │                         │                           │
   │ 1. POST /api/auth/login │                           │
   ├────────────────────────>├──────────────────────────>│
   │  {email, password}      │                           │
   │                         │                           │
   │                         │                           │ 2. Validate
   │                         │                           │    credentials
   │                         │                           │
   │                         │                           │ 3. Generate
   │                         │                           │    JWT token
   │                         │                           │
   │                         │ 4. Return tokens          │
   │<────────────────────────┤<──────────────────────────┤
   │  {accessToken, refreshToken}                       │
   │                         │                           │
   │ 5. GET /api/products (with Authorization header)   │
   ├────────────────────────>│                           │
   │  Header: Bearer {token} │                           │
   │                         │                           │
   │                         │ 6. Validate JWT           │
   │                         │    Extract claims         │
   │                         │                           │
   │                         │ 7. Forward to Product Service
   │                         ├───────────────────────────>
   │                         │  (with user context)      
   │                         │                           
```

### JWT Token Structure

```json
{
  "header": {
    "alg": "HS256",
    "typ": "JWT"
  },
  "payload": {
    "sub": "user-id-uuid",
    "email": "user@example.com",
    "name": "John Doe",
    "roles": ["User", "Seller"],
    "permissions": ["product:create", "product:update"],
    "iat": 1609459200,
    "exp": 1609462800,
    "iss": "ecommerce-auth",
    "aud": "ecommerce-services"
  },
  "signature": "..."
}
```

### Authorization Matrix

| Role | Permissions |
|------|-------------|
| **Guest** | Browse products, view details |
| **User** | + Add to cart, checkout, view orders, manage profile |
| **Seller** | + Create products, view seller orders, update inventory |
| **Admin** | + Manage users, roles, all orders, system config |

### Security Best Practices

1. **HTTPS Only** in production
2. **JWT Secret** stored in environment variables
3. **Password Hashing** with BCrypt (cost factor 12)
4. **CORS** configured per environment
5. **SQL Injection** prevented by EF Core parameterization
6. **XSS Protection** via JSON encoding
7. **Rate Limiting** to prevent abuse
8. **Input Validation** on all endpoints

---

## Data Architecture

### Database Design

```
Per-Service Database Pattern:

┌─────────────┐  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐
│   auth_db   │  │   user_db   │  │ product_db  │  │  order_db   │
├─────────────┤  ├─────────────┤  ├─────────────┤  ├─────────────┤
│ Users       │  │ UserProfiles│  │ Products    │  │ Orders      │
│ Roles       │  │ Addresses   │  │ Categories  │  │ OrderItems  │
│ Permissions │  │ Preferences │  │ Brands      │  │ CartItems   │
│ UserRoles   │  │ Avatars     │  │ Variants    │  │ Addresses   │
└─────────────┘  └─────────────┘  │ Attributes  │  └─────────────┘
                                  │ Images      │
                                  └─────────────┘

┌─────────────┐  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐
│ payment_db  │  │inventory_db │  │ discount_db │  │notification │
├─────────────┤  ├─────────────┤  ├─────────────┤  │     _db     │
│ Payments    │  │ Inventory   │  │ Discounts   │  ├─────────────┤
│ Transactions│  │ Reservations│  │ Campaigns   │  │Notifications│
│ Methods     │  │ StockLogs   │  │ Categories  │  │ Templates   │
│ Refunds     │  │ Warehouses  │  │ UsageHistory│  │ Deliveries  │
└─────────────┘  └─────────────┘  └─────────────┘  └─────────────┘
```

### Data Consistency Patterns

#### 1. Strong Consistency (within service)
```
Single database transaction
ACID guarantees
Example: Order creation with items
```

#### 2. Eventual Consistency (across services)
```
Event-driven updates
Saga pattern for distributed transactions
Example: Order → Payment → Inventory flow
```

#### 3. Data Duplication
```
Denormalize for performance
Store essential data in order items:
- Product name, price (at order time)
- Seller name
- Variant details

Benefit: Historical accuracy, query performance
Trade-off: Storage space, sync complexity
```

### Database Schema Examples

#### Order Service Schema
```sql
CREATE TABLE orders (
    id UUID PRIMARY KEY,
    user_id UUID NOT NULL,
    order_number VARCHAR(50) UNIQUE NOT NULL,
    status VARCHAR(20) NOT NULL,
    payment_status VARCHAR(20) NOT NULL,
    subtotal DECIMAL(18,2) NOT NULL,
    shipping_fee DECIMAL(18,2) NOT NULL,
    discount_amount DECIMAL(18,2) NOT NULL,
    total_amount DECIMAL(18,2) NOT NULL,
    created_at TIMESTAMP WITH TIME ZONE NOT NULL,
    updated_at TIMESTAMP WITH TIME ZONE NOT NULL
);

CREATE TABLE order_items (
    id UUID PRIMARY KEY,
    order_id UUID NOT NULL REFERENCES orders(id),
    product_id UUID NOT NULL,
    product_name VARCHAR(500) NOT NULL,
    product_image VARCHAR(2000),
    variant_id UUID,
    variant_name VARCHAR(500),
    unit_price DECIMAL(18,2) NOT NULL,
    quantity INT NOT NULL,
    total_price DECIMAL(18,2) NOT NULL,
    status VARCHAR(20) NOT NULL
);

CREATE INDEX idx_orders_user_id ON orders(user_id);
CREATE INDEX idx_orders_status ON orders(status);
CREATE INDEX idx_order_items_order_id ON order_items(order_id);
```

---

## Deployment Architecture

### Docker Compose Topology

```
┌─────────────────────────────────────────────────────────────────┐
│                    Docker Network (Bridge)                       │
│                                                                  │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐         │
│  │  PostgreSQL  │  │    Redis     │  │  RabbitMQ    │         │
│  │   :5432      │  │    :6379     │  │  :5672       │         │
│  │              │  │              │  │  :15672 (UI) │         │
│  └──────────────┘  └──────────────┘  └──────────────┘         │
│         │                  │                  │                 │
│         └──────────────────┴──────────────────┘                 │
│                            │                                    │
│         ┌──────────────────┴──────────────────┐                │
│         │                                     │                │
│  ┌──────▼──────┐  ┌──────────┐  ┌──────────┐ │               │
│  │    Auth     │  │   User   │  │ Product  │ │               │
│  │   :5001     │  │  :5002   │  │  :5004   │ │               │
│  └─────────────┘  └──────────┘  └──────────┘ │               │
│                                               │                │
│  ┌─────────────┐  ┌──────────┐  ┌──────────┐ │               │
│  │   Order     │  │ Payment  │  │Inventory │ │               │
│  │   :5003     │  │  :5005   │  │:5006/5106│ │               │
│  └─────────────┘  └──────────┘  └──────────┘ │               │
│                                               │                │
│  ┌─────────────┐  ┌─────────────────────────┐│               │
│  │  Discount   │  │    Notification         ││               │
│  │:5007/5107   │  │       :5008             ││               │
│  └─────────────┘  └─────────────────────────┘│               │
│                            │                                   │
│                   ┌────────▼─────────┐                        │
│                   │    Gateway       │                        │
│                   │     :5010        │                        │
│                   └──────────────────┘                        │
│                            │                                   │
└────────────────────────────┼───────────────────────────────────┘
                             │
                             │ HTTPS
                             ▼
                      ┌─────────────┐
                      │   Clients   │
                      └─────────────┘
```

### Container Resource Limits

```yaml
# Recommended resource allocation
services:
  postgres:
    mem_limit: 512m
    cpus: 0.5
    
  redis:
    mem_limit: 256m
    cpus: 0.25
    
  rabbitmq:
    mem_limit: 512m
    cpus: 0.5
    
  *-service:
    mem_limit: 512m
    cpus: 0.5
```

### Health Checks

```yaml
healthcheck:
  test: ["CMD", "curl", "-f", "http://localhost:8080/health"]
  interval: 30s
  timeout: 10s
  retries: 3
  start_period: 40s
```

### Scaling Strategy

```bash
# Horizontal scaling with Docker Swarm or Kubernetes
docker service scale order-service=3
docker service scale product-service=3

# Stateless services can scale easily:
- Auth, User, Product, Order, Payment ✓

# Stateful services require special handling:
- PostgreSQL: Read replicas, partitioning
- Redis: Clustering, Sentinel
- RabbitMQ: Clustering, federation
```

---

## Monitoring & Observability

### Key Metrics to Track

```
Application Metrics:
- Request rate (req/sec)
- Response time (p50, p95, p99)
- Error rate (%)
- Active connections

Business Metrics:
- Orders per hour
- Payment success rate
- Cart abandonment rate
- Product view to purchase conversion

Infrastructure Metrics:
- CPU usage
- Memory usage
- Disk I/O
- Network bandwidth

Service Health:
- Database connection pool
- Cache hit/miss ratio
- Message queue depth
- gRPC call success rate
```

### Logging Strategy

```
Log Levels:
- TRACE: Detailed diagnostic
- DEBUG: Development info
- INFO: General information
- WARNING: Potential issues
- ERROR: Errors that need attention
- FATAL: Critical failures

Structured Logging (JSON):
{
  "timestamp": "2024-01-01T12:00:00Z",
  "level": "INFO",
  "service": "order-service",
  "traceId": "abc123",
  "userId": "user-uuid",
  "message": "Order created successfully",
  "orderId": "order-uuid",
  "amount": 1000000
}
```

### Distributed Tracing

```
Request Flow Tracing:

TraceId: abc-123-def
│
├─ Span 1: Gateway (5ms)
│  └─ Span 2: Auth validation (2ms)
│
├─ Span 3: Order Service (150ms)
│  ├─ Span 4: Discount gRPC call (20ms)
│  ├─ Span 5: Inventory gRPC call (30ms)
│  └─ Span 6: Database write (50ms)
│
└─ Span 7: RabbitMQ publish (5ms)
```

---

## Performance Optimization

### Response Time Targets

| Operation Type | Target | Acceptable |
|----------------|--------|------------|
| Product List | < 100ms | < 200ms |
| Product Detail | < 50ms | < 100ms |
| Checkout | < 500ms | < 1000ms |
| Search | < 200ms | < 400ms |
| Cart Operations | < 100ms | < 200ms |

### Optimization Techniques

1. **Caching**: Redis for hot data
2. **Database Indexing**: Critical query paths
3. **Connection Pooling**: Reuse DB connections
4. **Async Processing**: Background jobs via RabbitMQ
5. **gRPC**: Fast inter-service communication
6. **Pagination**: Limit query results
7. **CDN**: Static assets (future)
8. **Database Partitioning**: Large tables (future)

---

## Disaster Recovery

### Backup Strategy

```
PostgreSQL:
- Daily full backup
- Continuous WAL archiving
- Point-in-time recovery capability
- Retention: 30 days

Redis:
- AOF (Append-Only File) enabled
- RDB snapshots every hour
- Retention: 7 days

RabbitMQ:
- Message persistence enabled
- Queue definitions backup
- Retention: 7 days
```

### Failure Scenarios & Mitigation

| Scenario | Impact | Mitigation |
|----------|--------|------------|
| Service Crash | Requests fail | Auto-restart, health checks |
| Database Down | Data operations fail | Connection retry, circuit breaker |
| Cache Down | Performance degradation | Graceful degradation to DB |
| Message Broker Down | Async operations queue | Persist messages, retry logic |
| Network Partition | Service isolation | Timeout configs, fallbacks |

---

## Future Enhancements

### Phase 2 Improvements

1. **Service Mesh** (Istio/Linkerd)
   - Advanced traffic management
   - Mutual TLS
   - Observability

2. **CQRS with Event Sourcing**
   - Separate read/write models
   - Event store
   - Replay capability

3. **GraphQL Gateway**
   - Flexible queries
   - Reduced over-fetching
   - Frontend optimization

4. **Kubernetes**
   - Production orchestration
   - Auto-scaling
   - Rolling updates

5. **Elasticsearch**
   - Advanced search
   - Full-text search
   - Faceted filtering

---

## Summary

This microservices architecture provides:

✅ **Scalability**: Independent service scaling  
✅ **Resilience**: Fault isolation, graceful degradation  
✅ **Flexibility**: Technology diversity per service  
✅ **Performance**: Caching, async processing, gRPC  
✅ **Maintainability**: Clear boundaries, single responsibility  
✅ **Observability**: Logging, metrics, tracing  
✅ **Security**: JWT auth, role-based access, encryption  

The system is production-ready with room for growth and optimization as business needs evolve.

