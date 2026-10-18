using EnglishSchool.BuildingBlocks.Modules;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace EnglishSchool.Modules.Practice;

/// <summary>
/// Practice moduli. Hozircha faqat chegara belgilangan.
/// Reja: 4–5-haftalar: mashq urinishlari, Tier 0 baholash, xatolar taksonomiyasi.
/// </summary>
public sealed class PracticeModule : IModule
{
    public string Name => "Practice";

    public void AddServices(IServiceCollection services, IConfiguration configuration)
    {
    }

    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
    }
}
