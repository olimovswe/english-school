using EnglishSchool.BuildingBlocks.Modules;
using EnglishSchool.BuildingBlocks.Outbox;
using EnglishSchool.Modules.Discipline.Infrastructure;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace EnglishSchool.Modules.Discipline;

/// <summary>
/// Discipline moduli. Hozircha faqat chegara belgilangan.
/// Reja: 6–7-haftalar: kunlik reja, darvoza, sessiya holat mashinasi, fokus vaqti.
/// </summary>
public sealed class DisciplineModule : IModule
{
    public string Name => "Discipline";

    public void AddServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<DisciplineDbContext>(options => options
            .UseNpgsql(
                configuration.GetConnectionString("Postgres"),
                npgsql => npgsql.MigrationsHistoryTable("__ef_migrations_history", DisciplineDbContext.Schema))
            .UseSnakeCaseNamingConvention()
            .UseOutbox());
        services.AddOutboxStore<DisciplineDbContext>();
    }

    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
    }
}
