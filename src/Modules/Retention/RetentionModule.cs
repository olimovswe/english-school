using EnglishSchool.BuildingBlocks.Modules;
using EnglishSchool.Modules.Retention.Infrastructure;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace EnglishSchool.Modules.Retention;

/// <summary>
/// Retention moduli: oʻquvchining soʻz va ibora kartochkalari va ularni FSRS boʻyicha takrorlash.
/// </summary>
public sealed class RetentionModule : IModule
{
    public string Name => "Retention";

    public void AddServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<RetentionOptions>().Bind(configuration.GetSection(RetentionOptions.Section));

        services.AddDbContext<RetentionDbContext>(options => options
            .UseNpgsql(
                configuration.GetConnectionString("Postgres"),
                npgsql => npgsql.MigrationsHistoryTable("__ef_migrations_history", RetentionDbContext.Schema))
            .UseSnakeCaseNamingConvention());

        services.AddHostedService<RetentionDatabaseMigrator>();
        services.AddHealthChecks().AddDbContextCheck<RetentionDbContext>("retention-db");
    }

    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
    }
}
