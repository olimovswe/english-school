using EnglishSchool.BuildingBlocks.Modules;
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
    }

    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
    }
}
