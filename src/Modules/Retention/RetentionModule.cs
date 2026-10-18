using EnglishSchool.BuildingBlocks.Health;
using EnglishSchool.BuildingBlocks.Modules;
using EnglishSchool.BuildingBlocks.Outbox;
using EnglishSchool.Modules.Discipline.Contracts.Events;
using EnglishSchool.Modules.Practice.Contracts.Events;
using EnglishSchool.Modules.Retention.Application;
using EnglishSchool.Modules.Retention.Contracts;
using EnglishSchool.Modules.Retention.Endpoints;
using EnglishSchool.Modules.Retention.Infrastructure;
using EnglishSchool.Modules.Retention.Scheduling;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace EnglishSchool.Modules.Retention;

/// <summary>
/// Retention moduli: oʻquvchining soʻz va ibora kartochkalari va ularni FSRS boʻyicha takrorlash.
/// Kartochkalar oʻquvchi kun mashqini bajarganda yoki kun ochilganda (eventlar orqali) avtomatik yaratiladi;
/// unutilgan kartochka <c>CardLapsed</c> eventi bilan tarqaladi.
/// </summary>
public sealed class RetentionModule : IModule
{
    public string Name => "Retention";

    public void AddServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<RetentionOptions>()
            .Bind(configuration.GetSection(RetentionOptions.Section))
            .Validate(o => o.DesiredRetention is > 0 and < 1 && o.MaximumIntervalDays >= 1,
                "Retention: DesiredRetention 0 va 1 oraligʻida, MaximumIntervalDays kamida 1 boʻlishi kerak.");

        services.AddDbContext<RetentionDbContext>(options => options
            .UseNpgsql(
                configuration.GetConnectionString("Postgres"),
                npgsql => npgsql.MigrationsHistoryTable("__ef_migrations_history", RetentionDbContext.Schema))
            .UseSnakeCaseNamingConvention()
            .UseOutbox());
        services.AddOutboxStore<RetentionDbContext>();

        services.TryAddSingleton(TimeProvider.System);
        services.AddSingleton<FsrsScheduler>();
        services.AddScoped<FlashcardEnrollment>();
        services.AddScoped<DueCardQueries>();
        services.AddScoped<ReviewService>();
        services.AddScoped<IRetentionGateAssessment, RetentionGateAssessment>();
        services.AddIntegrationEventConsumer<AttemptEvaluated, EnrollOnPracticeConsumer>();
        services.AddIntegrationEventConsumer<DailyGatePassed, EnrollOnDayUnlockedConsumer>();

        services.AddHostedService<RetentionDatabaseMigrator>();
        services.AddHealthChecks().AddModuleDatabaseCheck<RetentionDbContext>("retention-db");
    }

    public void MapEndpoints(IEndpointRouteBuilder endpoints) => endpoints.MapRetentionEndpoints();
}
