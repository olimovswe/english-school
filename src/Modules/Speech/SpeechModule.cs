using EnglishSchool.BuildingBlocks.Modules;
using EnglishSchool.Modules.Speech.Infrastructure;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace EnglishSchool.Modules.Speech;

/// <summary>
/// Speech moduli: audio fayllar (namuna talaffuz klipi va oʻquvchi yozuvi) va talaffuz baholari.
/// </summary>
public sealed class SpeechModule : IModule
{
    public string Name => "Speech";

    public void AddServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<SpeechOptions>().Bind(configuration.GetSection(SpeechOptions.Section));

        services.AddDbContext<SpeechDbContext>(options => options
            .UseNpgsql(
                configuration.GetConnectionString("Postgres"),
                npgsql => npgsql.MigrationsHistoryTable("__ef_migrations_history", SpeechDbContext.Schema))
            .UseSnakeCaseNamingConvention());

        services.AddHostedService<SpeechDatabaseMigrator>();
        services.AddHealthChecks().AddDbContextCheck<SpeechDbContext>("speech-db");
    }

    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
    }
}
