# 🎯 Redis Cache Patterns & Best Practices

## 📚 Cache Patterns

### 1. **Cache-Aside (Lazy Loading)** ⭐ Most Common

**Khi nào dùng:** Read-heavy operations, data không thay đổi thường xuyên

**Flow:**
```
1. Check cache
2. If HIT → return from cache
3. If MISS → get from DB → save to cache → return
```

**Implementation:**
```csharp
public async Task<ProductDto?> GetByIdAsync(Guid id)
{
    var cacheKey = CacheKeys.Product.ById(id);

    // Try get from cache
    var cached = await _cache.GetAsync<ProductDto>(cacheKey);
    if (cached != null)
        return cached;

    // Cache miss - get from DB
    var product = await _repository.GetByIdAsync(id);
    if (product == null)
        return null;

    // Save to cache
    await _cache.SetAsync(cacheKey, product, CacheTTL.Product);

    return product;
}

// Or use helper method
public async Task<ProductDto?> GetByIdAsync(Guid id)
{
    return await _cache.GetOrSetAsync(
        key: CacheKeys.Product.ById(id),
        factory: async () => await _repository.GetByIdAsync(id),
        expiration: CacheTTL.Product
    );
}
```

**Pros:**
- ✅ Simple to implement
- ✅ Only cache what's needed
- ✅ Cache failures don't break the app

**Cons:**
- ❌ Cache miss penalty (slower first request)
- ❌ Possible cache stampede

---

### 2. **Write-Through Cache**

**Khi nào dùng:** Data consistency is critical, write operations are acceptable to be slower

**Flow:**
```
1. Update DB
2. Update cache immediately
3. Return
```

**Implementation:**
```csharp
public async Task<ProductDto> UpdateAsync(Guid id, UpdateProductDto dto)
{
    // Update DB
    var product = await _repository.UpdateAsync(id, dto);

    // Update cache immediately (Write-Through)
    var cacheKey = CacheKeys.Product.ById(id);
    await _cache.SetAsync(cacheKey, product, CacheTTL.Product);

    return product;
}
```

**Pros:**
- ✅ Cache always up-to-date
- ✅ No cache miss after write
- ✅ Consistent data

**Cons:**
- ❌ Write latency (2 operations)
- ❌ Wasted writes if data not read

---

### 3. **Write-Behind (Write-Back) Cache**

**Khi nào dùng:** Write-heavy operations, eventual consistency is acceptable

**Flow:**
```
1. Update cache immediately
2. Queue DB write (async)
3. Return (fast)
4. Background worker writes to DB
```

**Implementation:**
```csharp
public async Task<ProductDto> UpdateAsync(Guid id, UpdateProductDto dto)
{
    // Update cache immediately
    var cacheKey = CacheKeys.Product.ById(id);
    await _cache.SetAsync(cacheKey, dto, CacheTTL.Product);

    // Queue DB write (background)
    await _queue.EnqueueAsync(new UpdateProductJob
    {
        ProductId = id,
        Data = dto
    });

    return dto;
}

// Background worker
public class UpdateProductWorker : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var job = await _queue.DequeueAsync();
            await _repository.UpdateAsync(job.ProductId, job.Data);
        }
    }
}
```

**Pros:**
- ✅ Very fast writes
- ✅ Reduced DB load
- ✅ Good for high write throughput

**Cons:**
- ❌ Complex implementation
- ❌ Risk of data loss if cache fails
- ❌ Eventual consistency

**⚠️ Not recommended for critical data (orders, payments)**

---

### 4. **Refresh-Ahead**

**Khi nào dùng:** Predictable access patterns, prevent cache miss for popular items

**Flow:**
```
1. Get from cache
2. If TTL < threshold → refresh in background
3. Return current cached value
```

**Implementation:**
```csharp
public async Task<ProductDto?> GetByIdAsync(Guid id)
{
    var cacheKey = CacheKeys.Product.ById(id);
    
    var product = await _cache.GetAsync<ProductDto>(cacheKey);
    
    if (product != null)
    {
        // Check TTL
        var ttl = await _cache.GetTtlAsync(cacheKey);
        
        // If TTL < 10 minutes, refresh in background
        if (ttl.HasValue && ttl.Value.TotalMinutes < 10)
        {
            _ = Task.Run(async () =>
            {
                var fresh = await _repository.GetByIdAsync(id);
                await _cache.SetAsync(cacheKey, fresh, CacheTTL.Product);
            });
        }
        
        return product;
    }

    // Cache miss - normal flow
    product = await _repository.GetByIdAsync(id);
    if (product != null)
    {
        await _cache.SetAsync(cacheKey, product, CacheTTL.Product);
    }

    return product;
}
```

**Pros:**
- ✅ No cache miss for popular items
- ✅ Always fresh data
- ✅ Better user experience

**Cons:**
- ❌ Complex logic
- ❌ More background processing
- ❌ May refresh unused data

---

### 5. **Cache Stampede Prevention**

**Problem:** Nhiều requests cùng lúc cache miss → tất cả query DB

**Solution: Lock/Semaphore**

```csharp
private static readonly SemaphoreSlim _semaphore = new(1, 1);

public async Task<ProductDto?> GetByIdAsync(Guid id)
{
    var cacheKey = CacheKeys.Product.ById(id);
    
    // Try get from cache
    var cached = await _cache.GetAsync<ProductDto>(cacheKey);
    if (cached != null)
        return cached;

    // Lock to prevent stampede
    await _semaphore.WaitAsync();
    try
    {
        // Double-check cache (another thread might have filled it)
        cached = await _cache.GetAsync<ProductDto>(cacheKey);
        if (cached != null)
            return cached;

        // Get from DB
        var product = await _repository.GetByIdAsync(id);
        if (product != null)
        {
            await _cache.SetAsync(cacheKey, product, CacheTTL.Product);
        }

        return product;
    }
    finally
    {
        _semaphore.Release();
    }
}
```

**Better: Use Redis Lock**

```csharp
public async Task<ProductDto?> GetByIdAsync(Guid id)
{
    var cacheKey = CacheKeys.Product.ById(id);
    var lockKey = $"{cacheKey}:lock";
    
    var cached = await _cache.GetAsync<ProductDto>(cacheKey);
    if (cached != null)
        return cached;

    // Try acquire lock
    var lockAcquired = await _db.StringSetAsync(
        lockKey, 
        "locked", 
        TimeSpan.FromSeconds(10), 
        When.NotExists
    );

    if (lockAcquired)
    {
        try
        {
            // This thread gets the data
            var product = await _repository.GetByIdAsync(id);
            if (product != null)
            {
                await _cache.SetAsync(cacheKey, product, CacheTTL.Product);
            }
            return product;
        }
        finally
        {
            await _db.KeyDeleteAsync(lockKey);
        }
    }
    else
    {
        // Wait and retry
        await Task.Delay(100);
        return await GetByIdAsync(id); // Retry
    }
}
```

---

## 🎯 Cache Invalidation Strategies

### 1. **Time-Based (TTL)** - Simplest

```csharp
// Auto expire after 1 hour
await _cache.SetAsync(key, value, TimeSpan.FromHours(1));
```

**Pros:** Simple, automatic
**Cons:** Data may be stale before expiration

---

### 2. **Event-Based Invalidation** - Most Accurate

```csharp
// On update → invalidate
public async Task UpdateProductAsync(Guid id, UpdateProductDto dto)
{
    await _repository.UpdateAsync(id, dto);
    
    // Invalidate specific product
    await _cache.RemoveAsync(CacheKeys.Product.ById(id));
    
    // Invalidate related caches
    await _cache.RemoveByPatternAsync(CacheKeys.Product.AllPattern());
}
```

**Pros:** Always fresh data
**Cons:** Must remember to invalidate everywhere

---

### 3. **Pattern-Based Invalidation**

```csharp
// Invalidate all product caches
await _cache.RemoveByPatternAsync("product:*");

// Invalidate all caches for a specific product
await _cache.RemoveByPatternAsync($"product:*:{productId}*");

// Invalidate all list caches
await _cache.RemoveByPatternAsync("product:list:*");
```

---

### 4. **Tag-Based Invalidation** (Advanced)

```csharp
// Set cache with tags
await _cache.SetAsync(key, value, expiration);
await _cache.SetAsync($"tag:product:{productId}", key, expiration);
await _cache.SetAsync($"tag:category:{categoryId}", key, expiration);

// Invalidate by tag
public async Task InvalidateByTagAsync(string tag)
{
    var keys = await _cache.GetAsync<List<string>>($"tag:{tag}");
    if (keys != null)
    {
        foreach (var key in keys)
        {
            await _cache.RemoveAsync(key);
        }
    }
}
```

---

## 🔥 Advanced Patterns

### 1. **Two-Level Cache (L1 + L2)**

```csharp
// L1: In-Memory Cache (fast, small)
// L2: Redis Cache (slower, large)

public class TwoLevelCacheService
{
    private readonly IMemoryCache _l1Cache;
    private readonly ICacheService _l2Cache;

    public async Task<T?> GetAsync<T>(string key)
    {
        // Try L1 first
        if (_l1Cache.TryGetValue(key, out T? value))
            return value;

        // Try L2
        value = await _l2Cache.GetAsync<T>(key);
        if (value != null)
        {
            // Promote to L1
            _l1Cache.Set(key, value, TimeSpan.FromMinutes(5));
        }

        return value;
    }
}
```

---

### 2. **Probabilistic Early Expiration (PEE)**

Prevent cache stampede by randomly refreshing before expiration

```csharp
public async Task<T?> GetWithPEEAsync<T>(string key, Func<Task<T>> factory, TimeSpan ttl)
{
    var cached = await _cache.GetAsync<T>(key);
    if (cached != null)
    {
        var currentTtl = await _cache.GetTtlAsync(key);
        if (currentTtl.HasValue)
        {
            // Calculate probability of early refresh
            var delta = ttl.TotalSeconds * 0.1; // 10% of TTL
            var probability = delta / currentTtl.Value.TotalSeconds;
            
            if (Random.Shared.NextDouble() < probability)
            {
                // Refresh in background
                _ = Task.Run(async () =>
                {
                    var fresh = await factory();
                    await _cache.SetAsync(key, fresh, ttl);
                });
            }
        }
        return cached;
    }

    // Cache miss
    var value = await factory();
    await _cache.SetAsync(key, value, ttl);
    return value;
}
```

---

### 3. **Batch Cache Operations**

```csharp
// Get multiple products at once
public async Task<Dictionary<Guid, ProductDto>> GetManyAsync(List<Guid> ids)
{
    var cacheKeys = ids.Select(id => CacheKeys.Product.ById(id)).ToList();
    
    // Batch get from cache
    var cached = await _cache.GetManyAsync<ProductDto>(cacheKeys);
    
    // Find missing
    var missing = ids.Where(id => 
        !cached.ContainsKey(CacheKeys.Product.ById(id)) ||
        cached[CacheKeys.Product.ById(id)] == null
    ).ToList();

    if (missing.Any())
    {
        // Batch get from DB
        var fromDb = await _repository.GetManyAsync(missing);
        
        // Batch set to cache
        var toCache = fromDb.ToDictionary(
            p => CacheKeys.Product.ById(p.Id),
            p => p
        );
        await _cache.SetManyAsync(toCache, CacheTTL.Product);
        
        // Merge results
        foreach (var kv in toCache)
        {
            cached[kv.Key] = kv.Value;
        }
    }

    return cached.ToDictionary(
        kv => ids.First(id => CacheKeys.Product.ById(id) == kv.Key),
        kv => kv.Value!
    );
}
```

---

## ⚠️ Best Practices

### ✅ DO

1. **Always set TTL** - Prevent memory leaks
2. **Use consistent key naming** - Use CacheKeys constants
3. **Invalidate on write** - Keep cache fresh
4. **Handle failures gracefully** - Fallback to DB
5. **Monitor cache hit rate** - Optimize based on metrics
6. **Use appropriate TTL** - Balance freshness vs performance
7. **Batch operations** - Reduce round trips
8. **Compress large objects** - Save memory
9. **Test invalidation** - Ensure consistency
10. **Log cache operations** - For debugging

### ❌ DON'T

1. **Don't cache everything** - Be selective
2. **Don't use same TTL for all** - Customize per use case
3. **Don't forget to invalidate** - Stale data is worse than no cache
4. **Don't cache sensitive data** - Without encryption
5. **Don't ignore cache failures** - Have fallback logic
6. **Don't cache large objects** - Without compression
7. **Don't use cache for locks** - Use proper distributed locks
8. **Don't cache volatile data** - Use short TTL
9. **Don't over-invalidate** - Balance freshness vs performance
10. **Don't skip monitoring** - Track metrics

---

## 📊 Cache Decision Tree

```
Should I cache this data?
│
├─ Is it read frequently? NO → Don't cache
│  YES ↓
│
├─ Does it change often? YES → Use short TTL (1-5min)
│  NO ↓
│
├─ Is it expensive to compute? NO → Maybe don't cache
│  YES ↓
│
├─ Is consistency critical? YES → Use Write-Through or short TTL
│  NO ↓
│
└─ Cache it! Use Cache-Aside with appropriate TTL
```

---

## 🎯 TTL Guidelines

| Data Type | Change Frequency | Recommended TTL |
|-----------|-----------------|-----------------|
| Static config | Rarely | 24 hours |
| Categories, Brands | Rarely | 12-24 hours |
| Product details | Sometimes | 1 hour |
| Product list | Often | 15 minutes |
| Stock availability | Very often | 2-5 minutes |
| Flash sale | Real-time | 30-60 seconds |
| User profile | Sometimes | 30 minutes |
| User session | Active | 15-30 minutes |
| Discount validation | Often | 5 minutes |
| Cart items | Active | 30 minutes |

---

## 🔍 Monitoring & Debugging

### Key Metrics:

```csharp
public class CacheMetrics
{
    public long TotalRequests { get; set; }
    public long CacheHits { get; set; }
    public long CacheMisses { get; set; }
    public double HitRate => TotalRequests > 0 
        ? (double)CacheHits / TotalRequests * 100 
        : 0;
}

// Track metrics
public async Task<T?> GetWithMetricsAsync<T>(string key)
{
    _metrics.TotalRequests++;
    
    var value = await _cache.GetAsync<T>(key);
    
    if (value != null)
        _metrics.CacheHits++;
    else
        _metrics.CacheMisses++;
    
    return value;
}
```

### Debug Logging:

```csharp
public async Task<T?> GetAsync<T>(string key)
{
    _logger.LogDebug("Cache GET: {Key}", key);
    
    var value = await _cache.GetAsync<T>(key);
    
    if (value != null)
        _logger.LogDebug("Cache HIT: {Key}", key);
    else
        _logger.LogDebug("Cache MISS: {Key}", key);
    
    return value;
}
```

---

## 📚 Summary

### Most Used Patterns (80% of cases):
1. **Cache-Aside** - For read-heavy data
2. **Write-Through** - For consistency-critical data
3. **TTL-based expiration** - Simple and effective

### When to Use Each:
- **Product catalog** → Cache-Aside + 1h TTL
- **Stock levels** → Cache-Aside + 5min TTL
- **User profile** → Write-Through + 30min TTL
- **Flash sales** → Cache-Aside + 1min TTL
- **Static config** → Cache-Aside + 24h TTL

### Key Takeaway:
> Start simple with Cache-Aside + TTL, optimize based on metrics! 🚀


