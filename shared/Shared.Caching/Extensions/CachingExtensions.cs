using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Shared.Caching.Interfaces;
using Shared.Caching.Services;
using StackExchange.Redis;

namespace Shared.Caching.Extensions;

public static class CachingExtensions
{
    public static IServiceCollection AddRedisCaching(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var redisConnection = configuration.GetConnectionString("Redis") 
            ?? configuration["Redis:ConnectionString"]
            ?? "localhost:6379";

        // Add StackExchange.Redis
        services.AddSingleton<IConnectionMultiplexer>(sp =>
        {
            var configOptions = ConfigurationOptions.Parse(redisConnection);
            configOptions.AbortOnConnectFail = false;
            configOptions.ConnectTimeout = 5000;
            configOptions.SyncTimeout = 5000;
            return ConnectionMultiplexer.Connect(configOptions);
        });

        // Add IDistributedCache
        services.AddStackExchangeRedisCache(options =>
        {
            options.Configuration = redisConnection;
            options.InstanceName = configuration["Redis:InstanceName"] ?? "ecommerce:";
        });

        // Add custom cache service
        services.AddScoped<ICacheService, RedisCacheService>();

        return services;
    }
}


