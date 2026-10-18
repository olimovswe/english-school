using EnglishSchool.BuildingBlocks.Modules;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace EnglishSchool.Modules.Identity;

/// <summary>
/// Identity moduli. Hozircha faqat chegara belgilangan.
/// Reja: 2-hafta: roʻyxatdan oʻtish, login (email + Telegram), JWT.
/// </summary>
public sealed class IdentityModule : IModule
{
    public string Name => "Identity";

    public void AddServices(IServiceCollection services, IConfiguration configuration)
    {
    }

    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
    }
}
