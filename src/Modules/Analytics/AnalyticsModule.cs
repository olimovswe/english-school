using EnglishSchool.BuildingBlocks.Health;
using EnglishSchool.BuildingBlocks.Modules;
using EnglishSchool.BuildingBlocks.Outbox;
using EnglishSchool.Modules.Analytics.Application;
using EnglishSchool.Modules.Analytics.Contracts;
using EnglishSchool.Modules.Analytics.Endpoints;
using EnglishSchool.Modules.Analytics.Infrastructure;
using EnglishSchool.Modules.Discipline.Contracts.Events;
using EnglishSchool.Modules.Practice.Contracts.Events;
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
        services.AddHealthChecks().AddModuleDatabaseCheck<AnalyticsDbContext>("analytics-db");
        services.AddScoped<AnalyticsEventUpdater>();
        services.AddScoped<IProgressQueries, ProgressQueries>();
        services.AddIntegrationEventConsumer<AttemptEvaluated, AttemptEvaluatedConsumer>();
        services.AddIntegrationEventConsumer<ErrorDetected, ErrorDetectedConsumer>();
        services.AddIntegrationEventConsumer<DailyGatePassed, DailyGatePassedConsumer>();
        services.AddIntegrationEventConsumer<SessionEnded, SessionEndedConsumer>();
    }

    public void MapEndpoints(IEndpointRouteBuilder endpoints) => endpoints.MapAnalyticsEndpoints();
}
