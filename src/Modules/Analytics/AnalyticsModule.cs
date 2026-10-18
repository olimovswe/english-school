using EnglishSchool.BuildingBlocks.Modules;
using EnglishSchool.Modules.Analytics.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace EnglishSchool.Modules.Analytics;

/// <summary>
/// Analytics moduli. Hozircha faqat chegara belgilangan.
/// Reja: 6–7-haftalar: progress va haftalik hisobot read modellari.
/// </summary>
public sealed class AnalyticsModule : IModule
{
    public string Name => "Analytics";

    public void AddServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<AnalyticsOptions>().Bind(configuration.GetSection(AnalyticsOptions.Section));
        services.AddDbContext<AnalyticsDbContext>(options => options
            .UseNpgsql(
                configuration.GetConnectionString("Postgres"),
                npgsql => npgsql.MigrationsHistoryTable("__ef_migrations_history", AnalyticsDbContext.Schema))
            .UseSnakeCaseNamingConvention());
        services.AddHostedService<AnalyticsDatabaseMigrator>();
        services.AddHealthChecks().AddDbContextCheck<AnalyticsDbContext>("analytics-db");
    }

    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
    }
}
