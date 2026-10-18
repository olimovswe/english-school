using EnglishSchool.BuildingBlocks.Modules;
using EnglishSchool.BuildingBlocks.Outbox;
using EnglishSchool.Modules.Practice.Infrastructure;
using EnglishSchool.Modules.Practice.Seed;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace EnglishSchool.Modules.Practice;

/// <summary>
/// Practice moduli: kun mashqlari (gap-fill, multiple choice, matching), urinishlar va takrorlanuvchi
/// xatolar taksonomiyasi. Natijalar <c>AttemptEvaluated</c> / <c>ErrorDetected</c> eventlari bilan tarqaladi.
/// </summary>
public sealed class PracticeModule : IModule
{
    public string Name => "Practice";

    public void AddServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<PracticeOptions>().Bind(configuration.GetSection(PracticeOptions.Section));

        services.AddDbContext<PracticeDbContext>(options => options
            .UseNpgsql(
                configuration.GetConnectionString("Postgres"),
                npgsql => npgsql.MigrationsHistoryTable("__ef_migrations_history", PracticeDbContext.Schema))
            .UseSnakeCaseNamingConvention()
            .UseOutbox());
        services.AddOutboxStore<PracticeDbContext>();

        services.AddHostedService<PracticeSeeder>();
        services.AddHealthChecks().AddDbContextCheck<PracticeDbContext>("practice-db");
    }

    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
    }
}
