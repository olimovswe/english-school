using System.Net;
using System.Text;
using EnglishSchool.Modules.Speech.Domain;
using EnglishSchool.Modules.Speech.Providers;
using EnglishSchool.Modules.Speech.Tests.TestSupport;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace EnglishSchool.Modules.Speech.Tests.Providers;

/// <summary>Mijoz modul sozlagan HttpClient orqali (manzil, kalit, timeout) soxta provayderga ulanadi.</summary>
public sealed class OpenAiSpeechClientTests
{
    private static readonly string[] WhisperFields = ["model", "language", "response_format", "timestamp_granularities[]", "temperature"];

    [Fact]
    public async Task Transcription_uploads_the_recording_for_whisper_verbose_json_and_reads_word_timestamps()
    {
        var provider = new FakeSpeechProvider();
        provider.Transcribes(" Nice to meet you. ", ("Nice", 0.0, 0.3), ("to", 0.3, 0.4), ("meet", 0.4, 0.7), ("you", 0.7, 1.0));
        using var services = Build(provider);
        var audio = AudioSamples.Webm();

        var transcription = await services.GetRequiredService<ISpeechTranscriber>()
            .TranscribeAsync(audio, AudioFormat.Webm, "en", CancellationToken.None);

        Assert.Equal(("Nice to meet you.", 1.1), (transcription.Text, transcription.DurationSeconds));
        Assert.Equal(new TranscribedWord("meet", 0.4, 0.7), transcription.Words[2]);

        var request = Assert.Single(provider.Transcriptions);
        Assert.Equal("Bearer test-key", request.Authorization);
        var form = await request.ReadFormAsync();
        Assert.Equal(audio, form["file"].Content);
        Assert.Equal("audio/webm", form["file"].ContentType);
        Assert.Equal(
            ["whisper-1", "en", "verbose_json", "word", "0"],
            WhisperFields.Select(f => Encoding.UTF8.GetString(form[f].Content)));
    }

    [Fact]
    public async Task Synthesis_posts_the_text_and_voice_settings_and_returns_mp3()
    {
        var provider = new FakeSpeechProvider();
        using var services = Build(provider);
        var synthesizer = services.GetRequiredService<ISpeechSynthesizer>();

        var audio = await synthesizer.SynthesizeAsync("How are you?", CancellationToken.None);

        Assert.Equal(provider.Mp3, audio.Content);
        Assert.Equal(AudioFormat.Mp3, audio.Format);
        Assert.Equal(("tts-1|alloy|0.9|mp3", "alloy"), (synthesizer.Signature, synthesizer.Voice));
        var body = Assert.Single(provider.Syntheses).Json;
        Assert.Equal(
            ("tts-1", "How are you?", "alloy", "mp3", 0.9),
            (body.GetProperty("model").GetString(), body.GetProperty("input").GetString(), body.GetProperty("voice").GetString(),
                body.GetProperty("response_format").GetString(), body.GetProperty("speed").GetDouble()));
    }

    [Theory]
    [InlineData(HttpStatusCode.TooManyRequests)]
    [InlineData(HttpStatusCode.InternalServerError)]
    [InlineData(HttpStatusCode.Unauthorized)]
    public async Task Provider_errors_surface_as_SpeechProviderException_with_the_status(HttpStatusCode status)
    {
        var provider = new FakeSpeechProvider();
        using var failing = provider.FailsWith(status);
        using var services = Build(provider);

        var exception = await Assert.ThrowsAsync<SpeechProviderException>(() => services.GetRequiredService<ISpeechTranscriber>()
            .TranscribeAsync(AudioSamples.Wav(), AudioFormat.Wav, "en", CancellationToken.None));

        Assert.Contains($"{(int)status} qaytardi", exception.Message, StringComparison.Ordinal);
        Assert.Contains("provider is down", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task A_slow_provider_times_out_as_a_provider_failure_not_a_cancellation()
    {
        using var services = Build(new StallingHandler(), timeout: "00:00:00.200");

        await Assert.ThrowsAsync<SpeechProviderException>(() => services.GetRequiredService<ISpeechSynthesizer>()
            .SynthesizeAsync("hello", CancellationToken.None));
    }

    [Fact]
    public async Task A_synthesis_answer_that_is_not_mp3_is_rejected()
    {
        using var services = Build(new StaticHandler(AudioSamples.Png()));

        await Assert.ThrowsAsync<SpeechProviderException>(() => services.GetRequiredService<ISpeechSynthesizer>()
            .SynthesizeAsync("hello", CancellationToken.None));
    }

    [Fact]
    public void Without_an_api_key_both_directions_are_unavailable_and_send_no_credentials()
    {
        using var services = Build(new FakeSpeechProvider(), apiKey: string.Empty);

        Assert.False(services.GetRequiredService<ISpeechTranscriber>().IsAvailable);
        Assert.False(services.GetRequiredService<ISpeechSynthesizer>().IsAvailable);
    }

    private static ServiceProvider Build(HttpMessageHandler handler, string apiKey = FakeSpeechProvider.ApiKey, string timeout = "00:00:05")
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Postgres"] = "Host=localhost;Database=unused",
                // Oxirida «/» yoʻq: mijoz baribir …/v1/audio/… ga murojaat qilishi kerak.
                ["Speech:Provider:BaseUrl"] = FakeSpeechProvider.BaseUrl,
                ["Speech:Provider:ApiKey"] = apiKey,
                ["Speech:Provider:Timeout"] = timeout,
            })
            .Build();

        var services = new ServiceCollection().AddLogging();
        new SpeechModule().AddServices(services, configuration);
        services.AddHttpClient<ISpeechTranscriber, OpenAiSpeechClient>().ConfigurePrimaryHttpMessageHandler(() => handler);
        services.AddHttpClient<ISpeechSynthesizer, OpenAiSpeechClient>().ConfigurePrimaryHttpMessageHandler(() => handler);
        return services.BuildServiceProvider();
    }

    private sealed class StallingHandler : HttpMessageHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            await Task.Delay(Timeout.Infinite, cancellationToken);
            return new HttpResponseMessage(HttpStatusCode.OK);
        }
    }

    private sealed class StaticHandler(byte[] body) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new ByteArrayContent(body) });
    }
}
