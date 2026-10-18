using System.Net.Http.Headers;
using EnglishSchool.BuildingBlocks.Modules;
using EnglishSchool.BuildingBlocks.Outbox;
using EnglishSchool.Modules.Discipline.Contracts.Events;
using EnglishSchool.Modules.Practice.Contracts.Events;
using EnglishSchool.Modules.Speech.Application;
using EnglishSchool.Modules.Speech.Infrastructure;
using EnglishSchool.Modules.Speech.Providers;
using EnglishSchool.Modules.Speech.Storage;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace EnglishSchool.Modules.Speech;

/// <summary>
/// Speech moduli: kurs soʻz va iboralarining namuna talaffuzi (TTS) va oʻquvchi yozuvining talaffuz bahosi
/// (Whisper transkripti matn bilan soʻzma-soʻz solishtiriladi). Audio fayllar audio omborida, metamaʼlumot va
/// baholar <c>speech</c> schema'sida. Kliplar kun ochilganda yoki mashq qilinganda (eventlar orqali) Worker'da yaratiladi.
/// </summary>
public sealed class SpeechModule : IModule
{
    public string Name => "Speech";

    public void AddServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<SpeechOptions>()
            .Bind(configuration.GetSection(SpeechOptions.Section))
            .Validate(
                o => !string.IsNullOrWhiteSpace(o.StoragePath) &&
                    o.MaxRecordingBytes is >= 1024 and <= 25 * 1024 * 1024 &&
                    o.DailyAssessmentLimit >= 1 &&
                    o.Provider.Speed is >= 0.25 and <= 4 &&
                    o.Provider.Timeout > TimeSpan.Zero,
                "Speech: StoragePath boʻsh boʻlmasin, MaxRecordingBytes 1 KB–25 MB, DailyAssessmentLimit kamida 1, " +
                "Provider:Speed 0,25–4, Provider:Timeout musbat boʻlishi kerak.");

        services.AddDbContext<SpeechDbContext>(options => options
            .UseNpgsql(
                configuration.GetConnectionString("Postgres"),
                npgsql => npgsql.MigrationsHistoryTable("__ef_migrations_history", SpeechDbContext.Schema))
            .UseSnakeCaseNamingConvention());

        services.TryAddSingleton(TimeProvider.System);
        services.AddSingleton<IAudioStorage, LocalAudioStorage>();
        services.AddHttpClient<ISpeechTranscriber, OpenAiSpeechClient>(ConfigureProvider);
        services.AddHttpClient<ISpeechSynthesizer, OpenAiSpeechClient>(ConfigureProvider);
        services.AddScoped<SpeechClipService>();
        services.AddScoped<PronunciationService>();
        services.AddIntegrationEventConsumer<DailyGatePassed, SynthesizeOnDayUnlockedConsumer>();
        services.AddIntegrationEventConsumer<AttemptEvaluated, SynthesizeOnPracticeConsumer>();

        services.AddHostedService<SpeechDatabaseMigrator>();
        services.AddHealthChecks().AddDbContextCheck<SpeechDbContext>("speech-db");
    }

    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
    }

    private static void ConfigureProvider(IServiceProvider services, HttpClient http)
    {
        var provider = services.GetRequiredService<IOptions<SpeechOptions>>().Value.Provider;

        // Nisbiy yoʻllar (audio/speech) bazaviy manzilning oxirgi segmentini (v1) oʻchirmasligi uchun oxirida «/».
        var baseUrl = provider.BaseUrl.AbsoluteUri;
        http.BaseAddress = new Uri(baseUrl.EndsWith('/') ? baseUrl : baseUrl + "/");
        http.Timeout = provider.Timeout;
        if (provider.IsConfigured)
        {
            http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", provider.ApiKey);
        }
    }
}
