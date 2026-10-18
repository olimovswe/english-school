using EnglishSchool.BuildingBlocks.Modules;
using EnglishSchool.BuildingBlocks.Outbox;
using EnglishSchool.Modules.Practice.Application;
using EnglishSchool.Modules.Practice.Endpoints;
using EnglishSchool.Modules.Practice.Grading;
using EnglishSchool.Modules.Practice.Infrastructure;
using EnglishSchool.Modules.Practice.Seed;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace EnglishSchool.Modules.Practice;

/// <summary>
/// Practice moduli: kun mashqlari (gap-fill, multiple choice, matching), javoblarni 4 tierli baholash va
/// takrorlanuvchi xatolar taksonomiyasi. Natijalar <c>AttemptEvaluated</c> / <c>ErrorDetected</c> eventlari bilan tarqaladi.
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

        services.TryAddSingleton(TimeProvider.System);
        services.AddScoped<IGradingTier, RuleBasedTier>();
        services.AddScoped<IGradingTier, NormalizationTier>();
        services.AddScoped<IGradingTier, AiGatewayTier>();
        services.AddScoped<GradingPipeline>();
        services.AddScoped<ExerciseQueries>();
        services.AddScoped<AttemptService>();

        services.AddHostedService<PracticeSeeder>();
        services.AddHealthChecks().AddDbContextCheck<PracticeDbContext>("practice-db");
    }

    public void MapEndpoints(IEndpointRouteBuilder endpoints) => endpoints.MapPracticeEndpoints();
}
