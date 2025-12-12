# 🚀 Redis Cache - Quick Start Guide

## 📦 Đã tạo sẵn

✅ **Shared.Caching Library** - Thư viện cache dùng chung  
✅ **ICacheService Interface** - Interface với đầy đủ methods  
✅ **RedisCacheService** - Implementation với StackExchange.Redis  
✅ **CacheKeys Constants** - Centralized cache key management  
✅ **CacheTTL Constants** - TTL cho từng loại cache  
✅ **Docker Compose** - Redis + Redis Commander + RedisInsight  

---

## 🎯 Quick Start (3 bước)

### **Bước 1: Start Redis**

```bash
# Start Redis với Docker Compose
docker-compose -f docker-compose-redis.yml up -d

# Verify Redis is running
docker exec -it ecommerce-redis redis-cli -a ecommerce_redis_2024 ping
# Expected: PONG

# Access Redis Commander (Web UI)
# http://localhost:8081
# Username: admin
# Password: admin123
```

### **Bước 2: Add Redis vào Service**

Ví dụ với **Product Service**:

```bash
cd service/product/Product.Api

# Add reference to Shared.Caching
dotnet add reference ../../../shared/Shared.Caching/Shared.Caching.csproj
```

**Update `Product.Api.csproj`:**
```xml
<ItemGroup>
  <ProjectReference Include="..\..\..\shared\Shared.Caching\Shared.Caching.csproj" />
</ItemGroup>
```

**Update `appsettings.json`:**
```json
{
  "ConnectionStrings": {
    "DBConnectParam": "Host=localhost;Database=product_db;Username=postgres;Password=postgres",
    "Redis": "localhost:6379,password=ecommerce_redis_2024"
  },
  "Redis": {
    "InstanceName": "product:",
    "ConnectionString": "localhost:6379,password=ecommerce_redis_2024"
  }
}
```

**Update `Program.cs`:**
```csharp
using Shared.Caching.Extensions;

// Add Redis Caching (thêm sau AddDbContext)
builder.Services.AddRedisCaching(builder.Configuration);

// Existing services...
builder.Services.AddScoped<IProductService, ProductService>();
```

### **Bước 3: Sử dụng trong Service**

**Example: `ProductService.cs`**

```csharp
using Shared.Caching.Interfaces;
using Shared.Caching.Constants;

public class ProductService : IProductService
{
    private readonly IProductRepository _repository;
    private readonly ICacheService _cache; // ← Inject ICacheService

    public ProductService(IProductRepository repository, ICacheService cache)
    {
        _repository = repository;
        _cache = cache;
    }

    // ✅ GET - Cache-Aside Pattern
    public async Task<ProductDto?> GetByIdAsync(Guid id)
    {
        var cacheKey = CacheKeys.Product.ById(id);

        return await _cache.GetOrSetAsync(
            key: cacheKey,
            factory: async () =>
            {
                var product = await _repository.GetByIdAsync(id);
                return product != null ? MapToDto(product) : null;
            },
            expiration: CacheTTL.Product // 1 hour
        );
    }

    // ✅ UPDATE - Invalidate Cache
    public async Task<ProductDto> UpdateAsync(Guid id, UpdateProductDto dto)
    {
        var product = await _repository.UpdateAsync(id, dto);

        // Invalidate cache
        await _cache.RemoveAsync(CacheKeys.Product.ById(id));
        await _cache.RemoveByPatternAsync(CacheKeys.Product.AllPattern());

        return MapToDto(product);
    }

    // ✅ DELETE - Invalidate Cache
    public async Task DeleteAsync(Guid id)
    {
        await _repository.DeleteAsync(id);

        // Invalidate cache
        await _cache.RemoveAsync(CacheKeys.Product.ById(id));
        await _cache.RemoveByPatternAsync(CacheKeys.Product.AllPattern());
    }

    // ✅ LIST - Cache with pagination
    public async Task<PagedResult<ProductDto>> GetListAsync(int page, int pageSize)
    {
        var cacheKey = CacheKeys.Product.List(page, pageSize);

        return await _cache.GetOrSetAsync(
            key: cacheKey,
            factory: async () => await _repository.GetPagedAsync(page, pageSize),
            expiration: CacheTTL.ProductList // 15 minutes
        );
    }
}
```

---

## 🎯 Triển khai theo thứ tự ưu tiên

### **1. Product Service** ⭐⭐⭐ (Cao nhất)
- Cache: Product details, categories, brands, featured products
- Impact: Giảm 70-90% DB queries
- TTL: 1-24 giờ

### **2. Inventory Service** ⭐⭐⭐
- Cache: Stock availability
- Impact: Giảm load khi check tồn kho
- TTL: 2-5 phút (ngắn vì thay đổi nhiều)

### **3. Discount Service** ⭐⭐
- Cache: Discount codes, validation results
- Impact: Tăng tốc validate discount
- TTL: 5-15 phút

### **4. User Service** ⭐⭐
- Cache: User profile, addresses, preferences
- Impact: Giảm queries cho user info
- TTL: 30-60 phút

### **5. Auth Service** ⭐
- Cache: User roles, permissions
- Impact: Tăng tốc authorization
- TTL: 30 phút

---

## 📊 Available Cache Methods

```csharp
// 1. Get from cache
var product = await _cache.GetAsync<ProductDto>(key);

// 2. Set to cache
await _cache.SetAsync(key, product, TimeSpan.FromMinutes(30));

// 3. Get or Set (Cache-Aside)
var product = await _cache.GetOrSetAsync(
    key, 
    async () => await _repository.GetAsync(id),
    TimeSpan.FromHours(1)
);

// 4. Remove single key
await _cache.RemoveAsync(key);

// 5. Remove by pattern
await _cache.RemoveByPatternAsync("product:*");

// 6. Check if exists
bool exists = await _cache.ExistsAsync(key);

// 7. Batch operations
await _cache.SetManyAsync(dictionary, expiration);
var results = await _cache.GetManyAsync<T>(keys);

// 8. Increment (for counters, rate limiting)
var count = await _cache.IncrementAsync(key, 1, TimeSpan.FromMinutes(1));

// 9. Set expiration
await _cache.ExpireAsync(key, TimeSpan.FromHours(2));

// 10. Get TTL
var ttl = await _cache.GetTtlAsync(key);
```

---

## 🔑 Cache Keys (Đã định nghĩa sẵn)

```csharp
using Shared.Caching.Constants;

// Product
CacheKeys.Product.ById(id)                    // "product:id:{guid}"
CacheKeys.Product.BySlug(slug)                // "product:slug:iphone-15"
CacheKeys.Product.List(page, pageSize)        // "product:list:p1:s20"
CacheKeys.Product.Featured()                  // "product:featured"
CacheKeys.Product.AllPattern()                // "product:*"

// Inventory
CacheKeys.Inventory.ByProductId(productId)    // "inventory:product:{guid}"
CacheKeys.Inventory.StockAvailability(id)     // "inventory:stock:{guid}"

// Discount
CacheKeys.Discount.ByCode(code)               // "discount:code:SALE10"
CacheKeys.Discount.Active()                   // "discount:active"

// User
CacheKeys.User.ProfileById(userId)            // "user:profile:{guid}"
CacheKeys.User.AddressesById(userId)          // "user:addresses:{guid}"

// Auth
CacheKeys.Auth.RolesById(userId)              // "auth:roles:{guid}"
CacheKeys.Auth.AllRoles()                     // "auth:roles:all"
```

---

## ⏱️ Cache TTL (Đã định nghĩa sẵn)

```csharp
using Shared.Caching.Constants;

CacheTTL.Product              // 1 hour
CacheTTL.ProductList          // 15 minutes
CacheTTL.Category             // 24 hours
CacheTTL.Inventory            // 5 minutes
CacheTTL.StockAvailability    // 2 minutes
CacheTTL.Discount             // 15 minutes
CacheTTL.UserProfile          // 30 minutes
CacheTTL.UserRoles            // 30 minutes
CacheTTL.FlashSale            // 1 minute
```

---

## 🔄 Cache Invalidation Patterns

### **1. On Update/Delete**
```csharp
public async Task UpdateProductAsync(Guid id, UpdateProductDto dto)
{
    await _repository.UpdateAsync(id, dto);
    
    // Invalidate specific product
    await _cache.RemoveAsync(CacheKeys.Product.ById(id));
    
    // Invalidate all product lists
    await _cache.RemoveByPatternAsync(CacheKeys.Product.AllPattern());
}
```

### **2. On Event (RabbitMQ)**
```csharp
// User.Service publishes user.profile.updated
// Product.Service consumes and invalidates cache

public class UserProfileUpdatedConsumer : IConsumer<UserProfileUpdatedEvent>
{
    private readonly ICacheService _cache;

    public async Task Consume(ConsumeContext<UserProfileUpdatedEvent> context)
    {
        var userId = context.Message.UserId;
        
        // Invalidate seller info in products
        await _cache.RemoveByPatternAsync($"product:seller:{userId}*");
    }
}
```

### **3. Write-Through**
```csharp
public async Task UpdateAsync(Guid id, UpdateDto dto)
{
    // Update DB
    var entity = await _repository.UpdateAsync(id, dto);
    
    // Update cache immediately
    await _cache.SetAsync(CacheKeys.Product.ById(id), entity, CacheTTL.Product);
    
    return entity;
}
```

---

## 🧪 Testing Cache

### **Test 1: Verify Redis Connection**
```bash
# Connect to Redis CLI
docker exec -it ecommerce-redis redis-cli -a ecommerce_redis_2024

# Test commands
> PING
PONG

> SET test:key "Hello Redis"
OK

> GET test:key
"Hello Redis"

> DEL test:key
(integer) 1
```

### **Test 2: Monitor Cache Activity**
```bash
# Monitor real-time commands
docker exec -it ecommerce-redis redis-cli -a ecommerce_redis_2024 MONITOR

# In another terminal, call your API
curl http://localhost:5002/api/products/{id}

# You should see Redis commands in MONITOR output
```

### **Test 3: Check Cache Hit**
```csharp
// First call - Cache MISS (slow)
var start = DateTime.UtcNow;
var product1 = await _productService.GetByIdAsync(id);
var duration1 = (DateTime.UtcNow - start).TotalMilliseconds;
Console.WriteLine($"First call: {duration1}ms"); // ~200ms

// Second call - Cache HIT (fast)
start = DateTime.UtcNow;
var product2 = await _productService.GetByIdAsync(id);
var duration2 = (DateTime.UtcNow - start).TotalMilliseconds;
Console.WriteLine($"Second call: {duration2}ms"); // ~5ms
```

---

## 🐛 Debugging

### **View all keys**
```bash
docker exec -it ecommerce-redis redis-cli -a ecommerce_redis_2024 KEYS "*"
```

### **View product keys**
```bash
docker exec -it ecommerce-redis redis-cli -a ecommerce_redis_2024 KEYS "product:*"
```

### **Get value**
```bash
docker exec -it ecommerce-redis redis-cli -a ecommerce_redis_2024 GET "product:id:xxx"
```

### **Delete by pattern**
```bash
docker exec -it ecommerce-redis redis-cli -a ecommerce_redis_2024 --eval "return redis.call('del', unpack(redis.call('keys', ARGV[1])))" 0 "product:*"
```

### **Check TTL**
```bash
docker exec -it ecommerce-redis redis-cli -a ecommerce_redis_2024 TTL "product:id:xxx"
```

### **Redis Commander (Web UI)**
```
URL: http://localhost:8081
Username: admin
Password: admin123
```

---

## 📈 Expected Performance

### **Before Redis:**
- Product detail: ~200ms (DB query)
- Product list: ~500ms (DB + joins)
- Stock check: ~100ms

### **After Redis (Cache Hit):**
- Product detail: ~5ms ⚡ (40x faster)
- Product list: ~10ms ⚡ (50x faster)
- Stock check: ~2ms ⚡ (50x faster)

### **Cache Hit Rate:**
- Target: 80-95%
- DB load reduction: 70-90%

---

## ⚠️ Best Practices

1. ✅ **Always set TTL** - Tránh memory leak
2. ✅ **Use CacheKeys constants** - Consistent naming
3. ✅ **Invalidate on write** - Đảm bảo data fresh
4. ✅ **Handle failures gracefully** - Fallback to DB nếu Redis down
5. ✅ **Monitor hit rate** - Optimize TTL dựa trên metrics
6. ✅ **Use appropriate TTL** - Balance giữa freshness vs performance
7. ✅ **Batch operations** - Dùng SetMany/GetMany khi có thể
8. ✅ **Test invalidation** - Đảm bảo cache được clear đúng lúc

---

## 🚀 Next Steps

1. ✅ Start Redis: `docker-compose -f docker-compose-redis.yml up -d`
2. ✅ Implement caching in Product Service (highest impact)
3. ✅ Test cache hit/miss scenarios
4. ✅ Monitor Redis Commander to see cache activity
5. ✅ Add caching to Inventory Service
6. ✅ Implement in other services
7. ✅ Monitor performance improvements
8. ✅ Optimize TTL values based on usage patterns

---

## 📚 Resources

- **Full Guide**: See `REDIS_IMPLEMENTATION_GUIDE.md`
- **Redis CLI**: https://redis.io/docs/manual/cli/
- **StackExchange.Redis**: https://stackexchange.github.io/StackExchange.Redis/
- **Redis Best Practices**: https://redis.io/docs/manual/patterns/

---

**Happy Caching! 🚀**


