using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace EnglishSchool.BuildingBlocks.Modules;

/// <summary>
/// Modulning yagona ommaviy kirish nuqtasi. Modulning qolgan barcha tiplari
/// <c>internal</c> boʻladi; buni Architecture.Tests tekshiradi.
/// </summary>
public interface IModule
{
    string Name { get; }

    /// <summary>API va Worker hostlarining ikkalasi ham chaqiradi.</summary>
    void AddServices(IServiceCollection services, IConfiguration configuration);

    /// <summary>Faqat API host chaqiradi. Endpointlar <c>/api/v1</c> guruhi ostiga ulanadi.</summary>
    void MapEndpoints(IEndpointRouteBuilder endpoints);
}
