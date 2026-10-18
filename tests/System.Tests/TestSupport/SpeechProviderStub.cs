using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace EnglishSchool.SystemTests.TestSupport;

/// <summary>
/// OpenAI Audio API oʻrnidagi haqiqiy HTTP server (Kestrel, 127.0.0.1, tasodifiy port). API ham, Worker ham unga
/// ishlab chiqarishdagi mijoz orqali ulanadi: Whisper belgilangan transkriptni, TTS esa MP3 qaytaradi.
/// </summary>
public sealed class SpeechProviderStub : IAsyncDisposable
{
    public const string ApiKey = "system-test-key";

    /// <summary>Har bir soʻz 0,3 s, toʻxtashsiz: ravonlik 100 chiqadi.</summary>
    private const double SecondsPerWord = 0.3;

    private readonly WebApplication _app;
    private int _transcriptions;
    private int _syntheses;

    private SpeechProviderStub(WebApplication app) => _app = app;

    public static byte[] Mp3 { get; } = [.. "ID3"u8, 0x04, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0xFF, 0xFB, 0x90, 0x64, .. new byte[256]];

    /// <summary>Mijoz sozlamasi uchun: <c>Speech:Provider:BaseUrl</c>.</summary>
    public Uri BaseUrl => new($"{_app.Urls.Single()}/v1/");

    /// <summary>Keyingi transkripsiyalar shu matnni qaytaradi.</summary>
    public string Transcript { get; set; } = string.Empty;

    public int Transcriptions => Volatile.Read(ref _transcriptions);

    public int Syntheses => Volatile.Read(ref _syntheses);

    public static async Task<SpeechProviderStub> StartAsync()
    {
        var builder = WebApplication.CreateSlimBuilder();
        builder.WebHost.UseUrls("http://127.0.0.1:0");
        builder.Logging.ClearProviders();
        var app = builder.Build();
        var stub = new SpeechProviderStub(app);

        app.MapPost("/v1/audio/transcriptions", stub.TranscribeAsync);
        app.MapPost("/v1/audio/speech", stub.Synthesize);
        await app.StartAsync();
        return stub;
    }

    public ValueTask DisposeAsync() => _app.DisposeAsync();

    private async Task<IResult> TranscribeAsync(HttpRequest request)
    {
        if (request.Headers.Authorization != $"Bearer {ApiKey}")
        {
            return Results.Unauthorized();
        }

        var form = await request.ReadFormAsync();
        if (form.Files["file"] is not { Length: > 0 } || form["response_format"] != "verbose_json" || form["language"] != "en")
        {
            return Results.BadRequest();
        }

        Interlocked.Increment(ref _transcriptions);
        var words = Transcript.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return Results.Json(new
        {
            task = "transcribe",
            language = "english",
            duration = (words.Length * SecondsPerWord) + 0.2,
            text = Transcript,
            words = words.Select((word, index) => new
            {
                word = word.Trim('.', ',', '?', '!'),
                start = index * SecondsPerWord,
                end = (index + 1) * SecondsPerWord,
            }),
        });
    }

    private IResult Synthesize(HttpRequest request, JsonElement body)
    {
        if (request.Headers.Authorization != $"Bearer {ApiKey}" || string.IsNullOrWhiteSpace(body.GetProperty("input").GetString()))
        {
            return Results.Unauthorized();
        }

        Interlocked.Increment(ref _syntheses);
        return Results.Bytes(Mp3, "audio/mpeg");
    }
}
