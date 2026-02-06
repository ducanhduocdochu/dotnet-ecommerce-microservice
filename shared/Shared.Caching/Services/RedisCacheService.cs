using Microsoft.Extensions.Caching.Distributed;
using Newtonsoft.Json;
using Shared.Caching.Interfaces;
using StackExchange.Redis;

namespace Shared.Caching.Services;

public class RedisCacheService : ICacheService
{
    private readonly IDistributedCache _cache;
    private readonly IConnectionMultiplexer _redis;
    private readonly IDatabase _db;

    public RedisCacheService(IDistributedCache cache, IConnectionMultiplexer redis)
    {
        _cache = cache;
        _redis = redis;
        _db = redis.GetDatabase();
    }

    public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
    {
        var value = await _cache.GetStringAsync(key, cancellationToken);
        
        if (string.IsNullOrEmpty(value))
            return default;

        return JsonConvert.DeserializeObject<T>(value);
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan? expiration = null, CancellationToken cancellationToken = default)
    {
        var serialized = JsonConvert.SerializeObject(value);
        
        var options = new DistributedCacheEntryOptions();
        if (expiration.HasValue)
        {
            options.AbsoluteExpirationRelativeToNow = expiration.Value;
        }

        await _cache.SetStringAsync(key, serialized, options, cancellationToken);
    }

    public async Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        await _cache.RemoveAsync(key, cancellationToken);
    }

    public async Task RemoveByPatternAsync(string pattern, CancellationToken cancellationToken = default)
    {
        var endpoints = _redis.GetEndPoints();
        var server = _redis.GetServer(endpoints.First());

        var keys = server.Keys(pattern: pattern).ToArray();
        
        if (keys.Length > 0)
        {
            await _db.KeyDeleteAsync(keys);
        }
    }

    public async Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default)
    {
        return await _db.KeyExistsAsync(key);
    }

    public async Task<T> GetOrSetAsync<T>(
        string key, 
        Func<Task<T>> factory, 
        TimeSpan? expiration = null,
        CancellationToken cancellationToken = default)
    {
        // Try get from cache
        var cached = await GetAsync<T>(key, cancellationToken);
        if (cached != null)
            return cached;

        // Get from source
        var value = await factory();
        
        // Set to cache
        if (value != null)
        {
            await SetAsync(key, value, expiration, cancellationToken);
        }

        return value;
    }

    public async Task SetManyAsync<T>(Dictionary<string, T> keyValues, TimeSpan? expiration = null, CancellationToken cancellationToken = default)
    {
        var tasks = keyValues.Select(kv => SetAsync(kv.Key, kv.Value, expiration, cancellationToken));
        await Task.WhenAll(tasks);
    }

    public async Task<Dictionary<string, T?>> GetManyAsync<T>(IEnumerable<string> keys, CancellationToken cancellationToken = default)
    {
        var result = new Dictionary<string, T?>();
        
        foreach (var key in keys)
        {
            var value = await GetAsync<T>(key, cancellationToken);
            result[key] = value;
        }

        return result;
    }

    public async Task<long> IncrementAsync(string key, long value = 1, TimeSpan? expiration = null, CancellationToken cancellationToken = default)
    {
        var result = await _db.StringIncrementAsync(key, value);
        
        if (expiration.HasValue)
        {
            await _db.KeyExpireAsync(key, expiration.Value);
        }

        return result;
    }

    public async Task<bool> ExpireAsync(string key, TimeSpan expiration, CancellationToken cancellationToken = default)
    {
        return await _db.KeyExpireAsync(key, expiration);
    }

    public async Task<TimeSpan?> GetTtlAsync(string key, CancellationToken cancellationToken = default)
    {
        return await _db.KeyTimeToLiveAsync(key);
    }
}


