using System.Security.Cryptography;
using EnglishSchool.Modules.Speech.Providers;
using EnglishSchool.Modules.Speech.Storage;
using EnglishSchool.Modules.Speech.Tests.TestSupport;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;

namespace EnglishSchool.Modules.Speech.Tests.Api;

/// <summary>
/// Butun API host haqiqiy PostgreSQL ustida. Nutq provayderi (Whisper, TTS) soxta HTTP handler, audio ombori
/// xotirada. Kliplar Curriculum lugʻatidan yaratiladi, token'ni Identity tekshiradi: uchala modul migratsiya qilinadi.
/// </summary>
public sealed class SpeechApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    public const string CollectionName = "speech-api";
    public const int DailyAssessmentLimit = 3;
    public const int MaxRecordingBytes = 64 * 1024;

    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("docker.io/pgvector/pgvector:pg18").Build();

    // Kalit factory'ga bitta: WithWebHostBuilder bilan yasalgan host ham shu tokenlarni qabul qiladi.
    private readonly string _signingKey = Convert.ToBase64String(RandomNumberGenerator.GetBytes(48));

    internal FakeSpeechProvider Provider { get; } = new();

    internal InMemoryAudioStorage Storage { get; } = new();

    public Task InitializeAsync() => _postgres.StartAsync();

    async Task IAsyncLifetime.DisposeAsync()
    {
        await DisposeAsync();
        await _postgres.DisposeAsync();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("ConnectionStrings:Postgres", _postgres.GetConnectionString());
        builder.UseSetting("ConnectionStrings:Redis", string.Empty);
        builder.UseSetting("Identity:ApplyMigrationsOnStartup", "true");
        builder.UseSetting("Identity:Jwt:SigningKey", _signingKey);
        builder.UseSetting("Curriculum:ApplyMigrationsOnStartup", "true");
        builder.UseSetting("Curriculum:SeedOnStartup", "true");
        builder.UseSetting("Speech:ApplyMigrationsOnStartup", "true");
        builder.UseSetting("Speech:DailyAssessmentLimit", DailyAssessmentLimit.ToString(System.Globalization.CultureInfo.InvariantCulture));
        builder.UseSetting("Speech:MaxRecordingBytes", MaxRecordingBytes.ToString(System.Globalization.CultureInfo.InvariantCulture));
        builder.UseSetting("Speech:Provider:BaseUrl", FakeSpeechProvider.BaseUrl);
        builder.UseSetting("Speech:Provider:ApiKey", FakeSpeechProvider.ApiKey);

        builder.ConfigureTestServices(services =>
        {
            services.AddSingleton<IAudioStorage>(Storage);
            services.AddHttpClient<ISpeechTranscriber, OpenAiSpeechClient>().ConfigurePrimaryHttpMessageHandler(() => Provider);
            services.AddHttpClient<ISpeechSynthesizer, OpenAiSpeechClient>().ConfigurePrimaryHttpMessageHandler(() => Provider);
        });
        builder.UseDefaultServiceProvider(options =>
        {
            options.ValidateScopes = true;
            options.ValidateOnBuild = true;
        });
    }
}

[CollectionDefinition(SpeechApiFactory.CollectionName)]
public sealed class SpeechApiCollectionDefinition : ICollectionFixture<SpeechApiFactory>;
