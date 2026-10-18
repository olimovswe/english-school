using EnglishSchool.BuildingBlocks.Modules;
using Microsoft.AspNetCore.Routing;
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
    }

    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
    }
}
