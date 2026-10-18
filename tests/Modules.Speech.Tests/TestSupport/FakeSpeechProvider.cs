using System.Collections.Concurrent;
using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Net.Http.Headers;

namespace EnglishSchool.Modules.Speech.Tests.TestSupport;

/// <summary>Bitta soʻrov: tana oʻqib olingan, chunki asl soʻrov provayder javobidan keyin yopiladi.</summary>
internal sealed record RecordedRequest(string Path, string? Authorization, string? ContentType, byte[] Body)
{
    public JsonElement Json => JsonDocument.Parse(Body).RootElement;

    /// <summary>Multipart forma: maydon nomi → (mazmun, Content-Type).</summary>
    public async Task<Dictionary<string, (byte[] Content, string? ContentType)>> ReadFormAsync()
    {
        var boundary = HeaderUtilities.RemoveQuotes(MediaTypeHeaderValue.Parse(ContentType).Boundary).Value!;
        var reader = new MultipartReader(boundary, new MemoryStream(Body));
        var fields = new Dictionary<string, (byte[], string?)>(StringComparer.Ordinal);
        while (await reader.ReadNextSectionAsync() is { } section)
        {
            var name = HeaderUtilities.RemoveQuotes(ContentDispositionHeaderValue.Parse(section.ContentDisposition).Name).Value!;
            using var content = new MemoryStream();
            await section.Body.CopyToAsync(content);
            fields[name] = (content.ToArray(), section.ContentType);
        }

        return fields;
    }
}

/// <summary>
/// OpenAI Audio API oʻrnidagi soxta provayder: transkripsiyaga belgilangan matnni, TTS'ga MP3 qaytaradi
/// va kelgan soʻrovlarni yozib boradi. Bitta collection testlari ketma-ket ishlaydi, holat shu tartibda oʻrnatiladi.
/// </summary>
internal sealed class FakeSpeechProvider : HttpMessageHandler
{
    public const string ApiKey = "test-key";
    public const string BaseUrl = "http://speech.test/v1";

    private readonly ConcurrentQueue<RecordedRequest> _requests = new();
    private string _transcript = string.Empty;
    private (string Word, double Start, double End)[] _words = [];
    private HttpStatusCode? _failure;

    public byte[] Mp3 { get; } = AudioSamples.Mp3();

    public IReadOnlyList<RecordedRequest> Transcriptions => [.. _requests.Where(r => r.Path == "/v1/audio/transcriptions")];

    public IReadOnlyList<RecordedRequest> Syntheses => [.. _requests.Where(r => r.Path == "/v1/audio/speech")];

    /// <summary>Keyingi transkripsiyalar shu matnni (va soʻz vaqtlarini) qaytaradi.</summary>
    public void Transcribes(string transcript, params (string Word, double Start, double End)[] words)
    {
        _transcript = transcript;
        _words = words;
    }

    /// <summary>Qaytarilgan obyekt yopilguncha har bir soʻrov shu status bilan tugaydi.</summary>
    public IDisposable FailsWith(HttpStatusCode status)
    {
        _failure = status;
        return new Restore(() => _failure = null);
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var body = request.Content is null ? [] : await request.Content.ReadAsByteArrayAsync(cancellationToken);
        _requests.Enqueue(new RecordedRequest(
            request.RequestUri!.AbsolutePath, request.Headers.Authorization?.ToString(), request.Content?.Headers.ContentType?.ToString(), body));

        if (_failure is { } status)
        {
            return new HttpResponseMessage(status)
            {
                Content = new StringContent("""{"error":{"message":"provider is down"}}""", Encoding.UTF8, "application/json"),
            };
        }

        return request.RequestUri.AbsolutePath switch
        {
            "/v1/audio/transcriptions" => new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new
                {
                    task = "transcribe",
                    language = "english",
                    duration = _words.Length == 0 ? 1.5 : _words[^1].End + 0.1,
                    text = _transcript,
                    words = _words.Select(w => new { word = w.Word, start = w.Start, end = w.End }),
                }),
            },
            "/v1/audio/speech" => new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new ByteArrayContent(Mp3) { Headers = { ContentType = new("audio/mpeg") } },
            },
            _ => new HttpResponseMessage(HttpStatusCode.NotFound),
        };
    }

    private sealed class Restore(Action restore) : IDisposable
    {
        public void Dispose() => restore();
    }
}
