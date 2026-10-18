using EnglishSchool.BuildingBlocks.Modules;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace EnglishSchool.Modules.Speech;

/// <summary>
/// Speech moduli. Hozircha faqat chegara belgilangan.
/// Reja: 10-hafta: TTS fayllari, Whisper worker, talaffuz bahosi.
/// </summary>
public sealed class SpeechModule : IModule
{
    public string Name => "Speech";

    public void AddServices(IServiceCollection services, IConfiguration configuration)
    {
    }

    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
    }
}
