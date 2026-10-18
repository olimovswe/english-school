using EnglishSchool.BuildingBlocks.Health;
using Microsoft.Extensions.Caching.StackExchangeRedis;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace EnglishSchool.BuildingBlocks.Caching;

public static class CachingServiceCollectionExtensions
{
    /// <summary>
    /// HybridCache: L1 jarayon xotirasi. "Redis" connection string berilgan boʻlsa, Valkey/Redis
    /// avtomatik L2 qatlamga aylanadi. API va Worker bir xil kalit prefiksini ishlatib, L2'ni boʻlishadi.
    /// L2 ulanishi tayyorlik tekshiruvida <c>redis</c> nomi bilan koʻrinadi.
    /// </summary>
    public static IServiceCollection AddHybridCaching(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddHybridCache();

        var redis = configuration.GetConnectionString("Redis");
        if (!string.IsNullOrWhiteSpace(redis))
        {
            services.AddSingleton(_ => new RedisConnection(redis));
            services.AddStackExchangeRedisCache(options => options.InstanceName = "es:");
            services.AddOptions<RedisCacheOptions>()
                .Configure<RedisConnection>((options, connection) => options.ConnectionMultiplexerFactory = connection.GetAsync);
            services.AddHealthChecks()
                .AddCheck<RedisHealthCheck>("redis", HealthStatus.Degraded, [HealthCheckTags.Ready], TimeSpan.FromSeconds(3));
        }

        return services;
    }
}
