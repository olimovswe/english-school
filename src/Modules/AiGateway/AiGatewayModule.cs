using EnglishSchool.BuildingBlocks.Modules;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace EnglishSchool.Modules.AiGateway;

/// <summary>
/// AiGateway moduli. Hozircha faqat chegara belgilangan.
/// Reja: 8–9-haftalar: LanguageTool, claude-haiku-5-5, eval_cache, budjet.
/// </summary>
public sealed class AiGatewayModule : IModule
{
    public string Name => "AiGateway";

    public void AddServices(IServiceCollection services, IConfiguration configuration)
    {
    }

    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
    }
}
