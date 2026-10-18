using EnglishSchool.BuildingBlocks.Modules;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace EnglishSchool.Modules.Retention;

/// <summary>
/// Retention moduli. Hozircha faqat chegara belgilangan.
/// Reja: 4–5-haftalar: kartochkalar, Leitner → FSRS.
/// </summary>
public sealed class RetentionModule : IModule
{
    public string Name => "Retention";

    public void AddServices(IServiceCollection services, IConfiguration configuration)
    {
    }

    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
    }
}
