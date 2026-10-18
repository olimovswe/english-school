using System.Globalization;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using EnglishSchool.Modules.Speech.Domain;
using Microsoft.Extensions.Options;

namespace EnglishSchool.Modules.Speech.Providers;

/// <summary>
/// OpenAI Audio API mijozi: <c>POST audio/transcriptions</c> (Whisper) va <c>POST audio/speech</c> (TTS).
/// Shu API'ni beradigan oʻz serverimiz bilan ham ishlaydi. Manzil, kalit va timeout <see cref="SpeechModule"/>da sozlanadi.
/// </summary>
internal sealed class OpenAiSpeechClient(HttpClient http, IOptions<SpeechOptions> options) : ISpeechTranscriber, ISpeechSynthesizer
{
    private const int MaxLoggedErrorLength = 500;

    private readonly SpeechProviderOptions _provider = options.Value.Provider;

    public bool IsAvailable => _provider.IsConfigured;

    public string Signature => string.Create(
        CultureInfo.InvariantCulture, $"{_provider.SynthesisModel}|{_provider.Voice}|{_provider.Speed:0.###}|mp3");

    public string Voice => _provider.Voice;

    public async Task<Transcription> TranscribeAsync(
        ReadOnlyMemory<byte> audio, AudioFormat format, string language, CancellationToken cancellationToken)
    {
        var file = new ReadOnlyMemoryContent(audio);
        file.Headers.ContentType = new MediaTypeHeaderValue(format.ContentType);

        using var request = new HttpRequestMessage(HttpMethod.Post, "audio/transcriptions")
        {
            Content = new MultipartFormDataContent
            {
                { file, "file", "recording" + format.Extension },
                { new StringContent(_provider.TranscriptionModel), "model" },
                { new StringContent(language), "language" },

                // Soʻz vaqt belgilari faqat verbose_json bilan keladi; ravonlik shulardan hisoblanadi.
                { new StringContent("verbose_json"), "response_format" },
                { new StringContent("word"), "timestamp_granularities[]" },
                { new StringContent("0"), "temperature" },
            },
        };

        using var response = await SendAsync(request, cancellationToken);
        WhisperResponse? body;
        try
        {
            body = await response.Content.ReadFromJsonAsync<WhisperResponse>(cancellationToken);
        }
        catch (JsonException exception)
        {
            throw new SpeechProviderException("Whisper javobi JSON emas.", exception);
        }

        if (body?.Text is null)
        {
            throw new SpeechProviderException("Whisper javobida text maydoni yoʻq.");
        }

        var words = (body.Words ?? [])
            .Where(w => !string.IsNullOrWhiteSpace(w.Word) && w.End >= w.Start)
            .Select(w => new TranscribedWord(w.Word.Trim(), w.Start, w.End))
            .ToList();
        return new Transcription(body.Text.Trim(), body.Duration, words);
    }

    public async Task<SynthesizedAudio> SynthesizeAsync(string text, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "audio/speech")
        {
            Content = JsonContent.Create(new SpeechRequest(_provider.SynthesisModel, text, _provider.Voice, "mp3", _provider.Speed)),
        };

        using var response = await SendAsync(request, cancellationToken);
        var content = await response.Content.ReadAsByteArrayAsync(cancellationToken);
        if (AudioFormat.Detect(content) != AudioFormat.Mp3)
        {
            throw new SpeechProviderException($"TTS javobi MP3 emas ({content.Length} bayt).");
        }

        return new SynthesizedAudio(content, AudioFormat.Mp3);
    }

    private async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        HttpResponseMessage response;
        try
        {
            response = await http.SendAsync(request, cancellationToken);
        }
        catch (HttpRequestException exception)
        {
            throw new SpeechProviderException($"Nutq provayderiga ulanib boʻlmadi ({request.RequestUri}).", exception);
        }
        catch (TaskCanceledException exception) when (!cancellationToken.IsCancellationRequested)
        {
            throw new SpeechProviderException($"Nutq provayderi {http.Timeout.TotalSeconds:0} s ichida javob bermadi.", exception);
        }

        if (response.IsSuccessStatusCode)
        {
            return response;
        }

        using (response)
        {
            var error = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new SpeechProviderException(
                $"Nutq provayderi {(int)response.StatusCode} qaytardi ({request.RequestUri}): {error[..Math.Min(error.Length, MaxLoggedErrorLength)]}");
        }
    }

    /// <summary><c>verbose_json</c> javobining bizga kerakli qismi.</summary>
    private sealed record WhisperResponse(string? Text, double? Duration, IReadOnlyList<WhisperWord>? Words);

    private sealed record WhisperWord(string Word, double Start, double End);

    private sealed record SpeechRequest(
        [property: JsonPropertyName("model")] string Model,
        [property: JsonPropertyName("input")] string Input,
        [property: JsonPropertyName("voice")] string Voice,
        [property: JsonPropertyName("response_format")] string ResponseFormat,
        [property: JsonPropertyName("speed")] double Speed);
}
