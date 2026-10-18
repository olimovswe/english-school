using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace EnglishSchool.BuildingBlocks.Caching;

public static class CachingServiceCollectionExtensions
{
    /// <summary>
    /// HybridCache: L1 jarayon xotirasi. "Redis" connection string berilgan boʻlsa, Valkey/Redis
    /// avtomatik L2 qatlamga aylanadi. API va Worker bir xil kalit prefiksini ishlatib, L2'ni boʻlishadi.
    /// </summary>
    public static IServiceCollection AddHybridCaching(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddHybridCache();

        var redis = configuration.GetConnectionString("Redis");
        if (!string.IsNullOrWhiteSpace(redis))
        {
            services.AddStackExchangeRedisCache(options =>
            {
                options.Configuration = redis;
                options.InstanceName = "es:";
            });
        }

        return services;
    }
}
