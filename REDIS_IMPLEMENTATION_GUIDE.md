# Redis Cache Implementation Guide

## 📋 Overview

Hướng dẫn triển khai Redis Cache cho các microservices trong hệ thống E-commerce.

---

## 🏗️ Architecture

```
┌─────────────────────────────────────────────────────────────┐
│                    Redis Cluster                             │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐      │
│  │ Redis Master │  │ Redis Master │  │ Redis Master │      │
│  │   (Shard 1)  │  │   (Shard 2)  │  │   (Shard 3)  │      │
│  └──────┬───────┘  └──────┬───────┘  └──────┬───────┘      │
│         │                 │                 │               │
│  ┌──────▼───────┐  ┌──────▼───────┐  ┌──────▼───────┐      │
│  │Redis Replica │  │Redis Replica │  │Redis Replica │      │
│  └──────────────┘  └──────────────┘  └──────────────┘      │
└─────────────────────────────────────────────────────────────┘
```

---

## 🚀 Quick Start

### 1. Start Redis with Docker

```bash
# Single Redis instance (Development)
docker run -d --name redis \
  -p 6379:6379 \
  redis:7-alpine redis-server --appendonly yes

# Redis with password
docker run -d --name redis \
  -p 6379:6379 \
  redis:7-alpine redis-server --requirepass "your_password" --appendonly yes

# Redis Cluster (Production)
# See docker-compose-redis.yml
```

### 2. Add to Service

```bash
# Example: Product Service
cd service/product/Product.Api

# Add project reference
dotnet add reference ../../../shared/Shared.Caching/Shared.Caching.csproj
```

### 3. Update appsettings.json

```json
{
  "ConnectionStrings": {
    "DBConnectParam": "...",
    "Redis": "localhost:6379"
  },
  "Redis": {
    "InstanceName": "product:",
    "ConnectionString": "localhost:6379,password=your_password"
  }
}
```

### 4. Register in Program.cs

```csharp
using Shared.Caching.Extensions;

// Add Redis Caching
builder.Services.AddRedisCaching(builder.Configuration);
```

---

## 📦 Implementation Examples

### **1. Product Service - Cache Product Details**

#### **ProductService.cs**

```csharp
using Shared.Caching.Interfaces;
using Shared.Caching.Constants;

public class ProductService : IProductService
{
    private readonly IProductRepository _repository;
    private readonly ICacheService _cache;

    public ProductService(IProductRepository repository, ICacheService cache)
    {
        _repository = repository;
        _cache = cache;
    }

    // ✅ Cache-Aside Pattern
    public async Task<ProductDto?> GetByIdAsync(Guid id)
    {
        var cacheKey = CacheKeys.Product.ById(id);

        // Try get from cache first
        var product = await _cache.GetOrSetAsync(
            key: cacheKey,
            factory: async () =>
            {
                // If not in cache, get from DB
                var entity = await _repository.GetByIdAsync(id);
                return entity != null ? MapToDto(entity) : null;
            },
            expiration: CacheTTL.Product
        );

        return product;
    }

    // ✅ Invalidate cache when update
    public async Task<ProductDto> UpdateAsync(Guid id, UpdateProductDto dto)
    {
        var product = await _repository.UpdateAsync(id, dto);

        // Invalidate cache
        await _cache.RemoveAsync(CacheKeys.Product.ById(id));

        // Also invalidate list caches
        await _cache.RemoveByPatternAsync(CacheKeys.Product.AllPattern());

        return MapToDto(product);
    }

    // ✅ Cache product list
    public async Task<PagedResult<ProductDto>> GetListAsync(int page, int pageSize)
    {
        var cacheKey = CacheKeys.Product.List(page, pageSize);

        var result = await _cache.GetOrSetAsync(
            key: cacheKey,
            factory: async () => await _repository.GetPagedAsync(page, pageSize),
            expiration: CacheTTL.ProductList
        );

        return result;
    }

    // ✅ Cache featured products
    public async Task<List<ProductDto>> GetFeaturedAsync()
    {
        var cacheKey = CacheKeys.Product.Featured();

        var products = await _cache.GetOrSetAsync(
            key: cacheKey,
            factory: async () => await _repository.GetFeaturedAsync(),
            expiration: CacheTTL.ProductFeatured
        );

        return products;
    }
}
```

---

### **2. Inventory Service - Cache Stock Availability**

#### **InventoryService.cs**

```csharp
public class InventoryService : IInventoryService
{
    private readonly IInventoryRepository _repository;
    private readonly ICacheService _cache;

    public async Task<StockDto?> GetStockAsync(Guid productId, Guid? variantId = null)
    {
        var cacheKey = CacheKeys.Inventory.ByProductId(productId, variantId);

        var stock = await _cache.GetOrSetAsync(
            key: cacheKey,
            factory: async () => await _repository.GetStockAsync(productId, variantId),
            expiration: CacheTTL.Inventory // 5 minutes
        );

        return stock;
    }

    // ✅ Invalidate when stock changes
    public async Task ReserveStockAsync(Guid productId, int quantity)
    {
        await _repository.ReserveStockAsync(productId, quantity);

        // Invalidate cache
        await _cache.RemoveByPatternAsync(
            CacheKeys.Inventory.ByProductPattern(productId)
        );
    }

    // ✅ Batch get stock (for product list)
    public async Task<Dictionary<Guid, int>> GetStockBatchAsync(List<Guid> productIds)
    {
        var cacheKeys = productIds.Select(id => CacheKeys.Inventory.StockAvailability(id));

        // Try get from cache
        var cached = await _cache.GetManyAsync<int>(cacheKeys);

        // Find missing keys
        var missing = productIds.Where(id =>
            !cached.ContainsKey(CacheKeys.Inventory.StockAvailability(id)) ||
            cached[CacheKeys.Inventory.StockAvailability(id)] == 0
        ).ToList();

        if (missing.Any())
        {
            // Get missing from DB
            var fromDb = await _repository.GetStockBatchAsync(missing);

            // Cache them
            var toCache = fromDb.ToDictionary(
                kv => CacheKeys.Inventory.StockAvailability(kv.Key),
                kv => kv.Value
            );
            await _cache.SetManyAsync(toCache, CacheTTL.StockAvailability);

            // Merge results
            foreach (var kv in fromDb)
            {
                cached[CacheKeys.Inventory.StockAvailability(kv.Key)] = kv.Value;
            }
        }

        return cached.ToDictionary(
            kv => productIds.First(id => CacheKeys.Inventory.StockAvailability(id) == kv.Key),
            kv => kv.Value ?? 0
        );
    }
}
```

---

### **3. Discount Service - Cache Discount Validation**

#### **DiscountService.cs**

```csharp
public class DiscountService : IDiscountService
{
    private readonly IDiscountRepository _repository;
    private readonly ICacheService _cache;

    // ✅ Cache discount by code
    public async Task<DiscountDto?> GetByCodeAsync(string code)
    {
        var cacheKey = CacheKeys.Discount.ByCode(code);

        var discount = await _cache.GetOrSetAsync(
            key: cacheKey,
            factory: async () => await _repository.GetByCodeAsync(code),
            expiration: CacheTTL.Discount
        );

        return discount;
    }

    // ✅ Cache validation result (short TTL)
    public async Task<ValidationResult> ValidateAsync(string code, Guid userId, decimal orderAmount)
    {
        var cacheKey = CacheKeys.Discount.Validation(code, userId);

        // Check cache first (5 minutes)
        var cached = await _cache.GetAsync<ValidationResult>(cacheKey);
        if (cached != null)
            return cached;

        // Validate from DB
        var result = await _repository.ValidateAsync(code, userId, orderAmount);

        // Cache only if valid (avoid caching errors)
        if (result.IsValid)
        {
            await _cache.SetAsync(cacheKey, result, CacheTTL.DiscountValidation);
        }

        return result;
    }

    // ✅ Cache active discounts
    public async Task<List<DiscountDto>> GetActiveDiscountsAsync()
    {
        var cacheKey = CacheKeys.Discount.Active();

        var discounts = await _cache.GetOrSetAsync(
            key: cacheKey,
            factory: async () => await _repository.GetActiveAsync(),
            expiration: CacheTTL.ActiveDiscounts
        );

        return discounts;
    }

    // ✅ Invalidate when discount used
    public async Task RecordUsageAsync(Guid discountId, Guid userId, Guid orderId)
    {
        await _repository.RecordUsageAsync(discountId, userId, orderId);

        // Invalidate validation cache for this user
        await _cache.RemoveByPatternAsync($"{CacheKeys.Discount.Prefix}validate:*:user:{userId}");

        // Invalidate discount cache
        await _cache.RemoveAsync(CacheKeys.Discount.ById(discountId));
    }
}
```

---

### **4. User Service - Cache User Profile**

#### **UserService.cs**

```csharp
public class UserService : IUserService
{
    private readonly IUserRepository _repository;
    private readonly ICacheService _cache;
    private readonly IMessagePublisher _publisher;

    public async Task<UserProfileDto?> GetProfileAsync(Guid userId)
    {
        var cacheKey = CacheKeys.User.ProfileById(userId);

        var profile = await _cache.GetOrSetAsync(
            key: cacheKey,
            factory: async () => await _repository.GetProfileAsync(userId),
            expiration: CacheTTL.UserProfile
        );

        return profile;
    }

    // ✅ Write-Through Cache Pattern
    public async Task<UserProfileDto> UpdateProfileAsync(Guid userId, UpdateProfileDto dto)
    {
        // Update DB
        var profile = await _repository.UpdateProfileAsync(userId, dto);

        // Update cache immediately (Write-Through)
        var cacheKey = CacheKeys.User.ProfileById(userId);
        await _cache.SetAsync(cacheKey, profile, CacheTTL.UserProfile);

        // Publish event for other services
        await _publisher.PublishAsync(
            EventConstants.UserExchange,
            EventConstants.UserProfileUpdated,
            new UserProfileUpdatedEvent
            {
                UserId = userId,
                FullName = profile.FullName,
                Email = profile.Email,
                Phone = profile.Phone,
                AvatarUrl = profile.AvatarUrl,
                UpdatedAt = DateTime.UtcNow
            }
        );

        return profile;
    }

    // ✅ Cache user addresses
    public async Task<List<UserAddressDto>> GetAddressesAsync(Guid userId)
    {
        var cacheKey = CacheKeys.User.AddressesById(userId);

        var addresses = await _cache.GetOrSetAsync(
            key: cacheKey,
            factory: async () => await _repository.GetAddressesAsync(userId),
            expiration: CacheTTL.UserAddresses
        );

        return addresses;
    }
}
```

---

### **5. Auth Service - Cache User Roles & Permissions**

#### **AuthService.cs**

```csharp
public class AuthService : IAuthService
{
    private readonly IAuthRepository _repository;
    private readonly ICacheService _cache;

    public async Task<List<RoleDto>> GetUserRolesAsync(Guid userId)
    {
        var cacheKey = CacheKeys.Auth.RolesById(userId);

        var roles = await _cache.GetOrSetAsync(
            key: cacheKey,
            factory: async () => await _repository.GetUserRolesAsync(userId),
            expiration: CacheTTL.UserRoles
        );

        return roles;
    }

    public async Task AssignRoleAsync(Guid userId, Guid roleId)
    {
        await _repository.AssignRoleAsync(userId, roleId);

        // Invalidate cache
        await _cache.RemoveAsync(CacheKeys.Auth.RolesById(userId));
        await _cache.RemoveAsync(CacheKeys.Auth.PermissionsById(userId));
    }

    // ✅ Cache all roles (rarely changes)
    public async Task<List<RoleDto>> GetAllRolesAsync()
    {
        var cacheKey = CacheKeys.Auth.AllRoles();

        var roles = await _cache.GetOrSetAsync(
            key: cacheKey,
            factory: async () => await _repository.GetAllRolesAsync(),
            expiration: CacheTTL.AllRoles // 12 hours
        );

        return roles;
    }
}
```

---

## 🔄 Cache Invalidation Strategies

### **1. Time-Based Expiration (TTL)**

```csharp
// Auto expire after TTL
await _cache.SetAsync(key, value, TimeSpan.FromMinutes(30));
```

### **2. Event-Based Invalidation**

```csharp
// When product updated → invalidate cache
public async Task UpdateProductAsync(Guid id, UpdateProductDto dto)
{
    await _repository.UpdateAsync(id, dto);

    // Invalidate specific product
    await _cache.RemoveAsync(CacheKeys.Product.ById(id));

    // Invalidate related caches
    await _cache.RemoveByPatternAsync(CacheKeys.Product.AllPattern());
}
```

### **3. Write-Through Cache**

```csharp
// Update DB and cache simultaneously
public async Task UpdateAsync(Guid id, UpdateDto dto)
{
    var entity = await _repository.UpdateAsync(id, dto);
    await _cache.SetAsync(CacheKeys.Product.ById(id), entity, CacheTTL.Product);
    return entity;
}
```

### **4. Cache-Aside (Lazy Loading)**

```csharp
// Most common pattern
var data = await _cache.GetOrSetAsync(
    key: cacheKey,
    factory: async () => await _repository.GetAsync(id),
    expiration: CacheTTL.Product
);
```

---

## 📊 Cache Patterns Summary

| Pattern           | Use Case           | Pros             | Cons                       |
| ----------------- | ------------------ | ---------------- | -------------------------- |
| **Cache-Aside**   | Read-heavy         | Simple, flexible | Cache miss penalty         |
| **Write-Through** | Write consistency  | Always fresh     | Write latency              |
| **Write-Behind**  | Write-heavy        | Fast writes      | Complex, risk of data loss |
| **Refresh-Ahead** | Predictable access | No cache miss    | Complex logic              |

---

## 🎯 Cache Strategy by Service

| Service       | Cache What                          | TTL       | Invalidation              |
| ------------- | ----------------------------------- | --------- | ------------------------- |
| **Product**   | Product details, categories, brands | 1-24h     | On update/delete          |
| **Inventory** | Stock availability                  | 2-5min    | On reserve/commit/release |
| **Discount**  | Discount codes, validation          | 5-15min   | On usage/update           |
| **User**      | Profile, addresses, preferences     | 30-60min  | On update                 |
| **Auth**      | Roles, permissions                  | 30min-12h | On role change            |
| **Order**     | Cart items                          | 30min     | On add/remove             |
| **Payment**   | Gateway configs                     | 24h       | On config change          |

---

## 🔥 Advanced: Rate Limiting with Redis

```csharp
public class RateLimitMiddleware
{
    private readonly ICacheService _cache;

    public async Task<bool> CheckRateLimitAsync(string clientId, string endpoint)
    {
        var key = CacheKeys.RateLimit.ByIp(clientId, endpoint);

        // Increment counter
        var count = await _cache.IncrementAsync(key, 1, TimeSpan.FromMinutes(1));

        // Check limit (100 requests per minute)
        return count <= 100;
    }
}
```

---

## 🔧 Redis CLI Commands (Debugging)

```bash
# Connect to Redis
docker exec -it redis redis-cli

# View all keys
KEYS *

# View product keys
KEYS product:*

# Get value
GET product:id:xxx-xxx-xxx

# Delete key
DEL product:id:xxx-xxx-xxx

# Delete by pattern
EVAL "return redis.call('del', unpack(redis.call('keys', ARGV[1])))" 0 product:*

# Check TTL
TTL product:id:xxx-xxx-xxx

# Monitor real-time
MONITOR

# Get info
INFO
INFO memory
INFO stats
```

---

## 🐳 Docker Compose for Redis

```yaml
version: "3.8"

services:
  redis:
    image: redis:7-alpine
    container_name: ecommerce-redis
    ports:
      - "6379:6379"
    command: redis-server --appendonly yes --requirepass "your_password"
    volumes:
      - redis-data:/data
    restart: unless-stopped
    healthcheck:
      test: ["CMD", "redis-cli", "ping"]
      interval: 10s
      timeout: 3s
      retries: 3

  redis-commander:
    image: rediscommander/redis-commander:latest
    container_name: redis-commander
    environment:
      - REDIS_HOSTS=local:redis:6379:0:your_password
    ports:
      - "8081:8081"
    depends_on:
      - redis

volumes:
  redis-data:
```

---

## 📈 Monitoring & Metrics

### Key Metrics to Track:

- **Hit Rate**: Cache hits / (hits + misses)
- **Eviction Rate**: Keys evicted due to memory
- **Memory Usage**: Current memory usage
- **Connection Count**: Active connections
- **Latency**: Average response time

### Tools:

- **Redis Commander**: Web UI for Redis
- **RedisInsight**: Official GUI tool
- **Prometheus + Grafana**: Metrics & dashboards

---

## ⚠️ Best Practices

1. ✅ **Always set TTL** - Avoid memory leaks
2. ✅ **Use consistent key naming** - Use CacheKeys constants
3. ✅ **Invalidate on write** - Keep cache fresh
4. ✅ **Handle cache failures gracefully** - Fallback to DB
5. ✅ **Monitor cache hit rate** - Optimize TTL
6. ✅ **Use appropriate TTL** - Balance freshness vs performance
7. ✅ **Batch operations** - Use SetMany/GetMany
8. ✅ **Compress large objects** - Save memory
9. ✅ **Use Redis Cluster** - For production scale
10. ✅ **Test cache invalidation** - Ensure consistency

---

## 🚀 Performance Impact

### Before Redis:

- Product detail: **~200ms** (DB query)
- Product list: **~500ms** (DB query + joins)
- Stock check: **~100ms** (DB query)

### After Redis:

- Product detail: **~5ms** (cache hit)
- Product list: **~10ms** (cache hit)
- Stock check: **~2ms** (cache hit)

### Expected Improvements:

- **40-100x faster** for cached data
- **80-95% cache hit rate** for read-heavy operations
- **Reduced DB load** by 70-90%
- **Better scalability** - Handle more concurrent users

---

## 📚 Next Steps

1. ✅ Implement Redis in Product Service (highest impact)
2. ✅ Add Redis to Inventory Service
3. ✅ Implement caching in Discount Service
4. ✅ Add monitoring & metrics
5. ✅ Test cache invalidation scenarios
6. ✅ Optimize TTL values based on metrics
7. ✅ Implement Redis Cluster for production

