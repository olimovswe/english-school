using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using EnglishSchool.BuildingBlocks.Outbox;
using EnglishSchool.Modules.Speech.Contracts;
using EnglishSchool.Modules.Speech.Contracts.Events;
using EnglishSchool.Modules.Speech.Domain;
using EnglishSchool.Modules.Speech.Infrastructure;
using EnglishSchool.Modules.Speech.Tests.TestSupport;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace EnglishSchool.Modules.Speech.Tests.Api;

[Collection(SpeechApiFactory.CollectionName)]
public sealed class AssessmentTests(SpeechApiFactory factory)
{
    [Fact]
    public async Task A_recording_is_transcribed_scored_stored_and_announced_through_the_outbox()
    {
        using var student = await factory.CreateStudentAsync();
        var wav = AudioSamples.Wav();
        factory.Provider.Transcribes(
            "I sink it is tree o'clock.",
            ("I", 0.0, 0.2), ("sink", 0.3, 0.6), ("it", 0.7, 0.8), ("is", 0.8, 0.9), ("tree", 1.0, 1.3), ("o'clock", 1.4, 1.9));

        var result = await student.AssessSuccessfullyAsync(wav, "I think it's three o'clock.");

        // Aniqlik 78,4 (ikki th xatosi), ravonlik 100 (toʻxtashsiz, 189 soʻz/daqiqa): 0,8 × 78,4 + 0,2 × 100.
        Assert.Equal((82.7m, 78.4m, 100m, (decimal?)100m, "PartiallyCorrect"), (result.OverallScore, result.AccuracyScore, result.CompletenessScore, result.FluencyScore, result.Verdict));
        Assert.Equal(new FluencyMetricsDto(1900, 189.5, 0, 0), result.Fluency);
        Assert.Equal(("I think it's three o'clock.", "I sink it is tree o'clock."), (result.TargetText, result.Transcript));
        var think = Assert.Single(result.Words, w => w.Expected == "think");
        Assert.Equal(("sink", "Mispronounced", "TH_SOUND"), (think.Heard, think.Status, think.FeedbackCode));
        Assert.StartsWith("th tovushi", think.Hint, StringComparison.Ordinal);
        Assert.Null(result.ReferenceAudioId);

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SpeechDbContext>();
        var assessment = await db.PronunciationAssessments.AsNoTracking().SingleAsync(a => a.Id == result.AssessmentId);
        Assert.Equal(
            result.Words.Select(w => (w.Expected, w.Heard, w.Status)),
            assessment.Words.Select(w => (w.Expected, w.Heard, w.Status.ToString())));
        Assert.Equal((student.UserId, (double?)189.5, (int?)1900), (assessment.UserId, assessment.WordsPerMinute, assessment.SpeechDurationMs));

        var recording = await db.AudioArtifacts.AsNoTracking().SingleAsync(a => a.Id == result.RecordingId);
        Assert.Equal(
            (AudioKind.Recording, (Guid?)student.UserId, "audio/wav", (long)wav.Length, (int?)2000, $"recordings/{student.UserId:N}/{recording.Id:N}.wav"),
            (recording.Kind, recording.OwnerId, recording.ContentType, recording.SizeBytes, recording.DurationMs, recording.StorageKey));
        Assert.Equal(Convert.ToHexStringLower(SHA256.HashData(wav)), recording.Sha256);
        Assert.Equal(wav, factory.Storage.Files[recording.StorageKey].Content);

        var (published, processedAt) = Assert.Single(await TranscriptsOfAsync(db, student.UserId));
        Assert.Equal((result.AssessmentId, SpeechPurposes.Pronunciation, "I sink it is tree o'clock."), (published.JobId, published.Purpose, published.Transcript));
        Assert.Null(processedAt);

        var request = factory.Provider.Transcriptions[^1];
        var form = await request.ReadFormAsync();
        Assert.Equal(("Bearer test-key", "whisper-1", "en"), (request.Authorization, Text(form["model"].Content), Text(form["language"].Content)));
        Assert.Equal(wav, form["file"].Content);
    }

    [Fact]
    public async Task An_exact_browser_recording_is_correct_and_links_the_reference_clip()
    {
        using var student = await factory.CreateStudentAsync();
        await factory.UnlockDayAsync(student.UserId, 1);
        factory.Provider.Transcribes("Nice to meet you.");

        using var response = await student.Client.AssessAsync(AudioSamples.Webm(), "Nice to meet you.", "recording.webm", "audio/webm");
        var result = await Ok<PronunciationAssessmentDto>(response);

        Assert.Equal((100m, (decimal?)null, "Correct"), (result.OverallScore, result.FluencyScore, result.Verdict));
        Assert.All(result.Words, w => Assert.Equal(("Correct", (string?)null), (w.Status, w.FeedbackCode)));
        Assert.NotNull(result.ReferenceAudioId);

        using var clip = await student.Client.GetAudioAsync(result.ReferenceAudioId.Value);
        Assert.Equal((HttpStatusCode.OK, "audio/mpeg"), (clip.StatusCode, clip.Content.Headers.ContentType?.MediaType));
        Assert.Equal(factory.Provider.Mp3, await clip.Content.ReadAsByteArrayAsync());

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SpeechDbContext>();
        var recording = await db.AudioArtifacts.AsNoTracking().SingleAsync(a => a.Id == result.RecordingId);
        Assert.Equal(("audio/webm", ".webm"), (recording.ContentType, Path.GetExtension(recording.StorageKey)));
    }

    [Fact]
    public async Task Invalid_uploads_are_rejected_before_anything_is_transcribed_or_stored()
    {
        using var student = await factory.CreateStudentAsync();
        using var anonymous = factory.CreateClient();
        var transcriptionsBefore = factory.Provider.Transcriptions.Count;
        var tooLarge = AudioSamples.Wav(SpeechApiFactory.MaxRecordingBytes / 2);

        (string Field, HttpResponseMessage Response)[] rejected =
        [
            ("audio", await student.Client.AssessAsync(null, "Hello")),
            ("audio", await student.Client.AssessAsync([], "Hello")),
            ("audio", await student.Client.AssessAsync(tooLarge, "Hello")),
            ("audio", await student.Client.AssessAsync(AudioSamples.Png(), "Hello", "recording.wav")),
            ("targetText", await student.Client.AssessAsync(AudioSamples.Wav(), null)),
            ("targetText", await student.Client.AssessAsync(AudioSamples.Wav(), new string('a', 301))),
            ("targetText", await student.Client.AssessAsync(AudioSamples.Wav(), "… ?!")),
        ];
        using var signedOut = await anonymous.AssessAsync(AudioSamples.Wav(), "Hello");

        foreach (var (field, response) in rejected)
        {
            using (response)
            {
                Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
                Assert.True((await response.ProblemAsync()).GetProperty("errors").TryGetProperty(field, out _), $"{field} xatosi kutilgan edi.");
            }
        }

        Assert.Equal(HttpStatusCode.Unauthorized, signedOut.StatusCode);
        Assert.Equal(transcriptionsBefore, factory.Provider.Transcriptions.Count);
        Assert.DoesNotContain(factory.Storage.Files.Keys, key => key.Contains($"{student.UserId:N}", StringComparison.Ordinal));
    }

    [Fact]
    public async Task A_provider_failure_returns_503_and_keeps_nothing()
    {
        using var student = await factory.CreateStudentAsync();

        using (factory.Provider.FailsWith(HttpStatusCode.InternalServerError))
        {
            using var response = await student.Client.AssessAsync(AudioSamples.Wav(), "Hello");

            Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
            Assert.Contains("vaqtincha ishlamayapti", (await response.ProblemAsync()).GetProperty("detail").GetString(), StringComparison.Ordinal);
        }

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SpeechDbContext>();
        Assert.False(await db.AudioArtifacts.AnyAsync(a => a.OwnerId == student.UserId));
        Assert.False(await db.PronunciationAssessments.AnyAsync(a => a.UserId == student.UserId));
        Assert.Empty(await TranscriptsOfAsync(db, student.UserId));
        Assert.DoesNotContain(factory.Storage.Files.Keys, key => key.Contains($"{student.UserId:N}", StringComparison.Ordinal));
    }

    [Fact]
    public async Task A_student_gets_a_limited_number_of_assessments_per_day()
    {
        using var student = await factory.CreateStudentAsync();
        factory.Provider.Transcribes("Hello.");
        for (var i = 0; i < SpeechApiFactory.DailyAssessmentLimit; i++)
        {
            await student.AssessSuccessfullyAsync(AudioSamples.Wav(), "Hello");
        }

        var transcriptionsBefore = factory.Provider.Transcriptions.Count;
        using var response = await student.Client.AssessAsync(AudioSamples.Wav(), "Hello");
        using var classmate = await factory.CreateStudentAsync();

        Assert.Equal(HttpStatusCode.TooManyRequests, response.StatusCode);
        Assert.Equal(transcriptionsBefore, factory.Provider.Transcriptions.Count);
        await classmate.AssessSuccessfullyAsync(AudioSamples.Wav(), "Hello");
    }

    [Fact]
    public async Task Without_a_provider_key_speech_services_are_switched_off()
    {
        await using var unconfigured = factory.WithWebHostBuilder(builder => builder.UseSetting("Speech:Provider:ApiKey", string.Empty));
        using var student = await unconfigured.CreateStudentAsync();
        var (transcriptions, syntheses) = (factory.Provider.Transcriptions.Count, factory.Provider.Syntheses.Count);

        using var response = await student.Client.AssessAsync(AudioSamples.Wav(), "Hello");
        await unconfigured.UnlockDayAsync(student.UserId, 5);

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        Assert.Equal("Talaffuzni baholash hozircha yoqilmagan.", (await response.ProblemAsync()).GetProperty("detail").GetString());
        Assert.Equal((transcriptions, syntheses), (factory.Provider.Transcriptions.Count, factory.Provider.Syntheses.Count));
        Assert.All(await student.GetDayClipsAsync(5), clip => Assert.Null(clip.AudioId));
    }

    private static async Task<T> Ok<T>(HttpResponseMessage response)
    {
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return await response.Content.ReadFromJsonAsync<T>() ?? throw new InvalidOperationException("Javob boʻsh.");
    }

    private static string Text(byte[] content) => Encoding.UTF8.GetString(content);

    private static async Task<List<(TranscriptReady Event, DateTimeOffset? ProcessedAt)>> TranscriptsOfAsync(SpeechDbContext db, Guid userId)
    {
        var typeName = OutboxSerializer.TypeName(typeof(TranscriptReady));
        var messages = await db.Set<OutboxMessage>().AsNoTracking().Where(m => m.Type == typeName).ToListAsync();
        return
        [
            .. messages
                .Select(m => ((TranscriptReady)OutboxSerializer.Deserialize(m.Content, typeof(TranscriptReady)), m.ProcessedAt))
                .Where(x => x.Item1.UserId == userId),
        ];
    }
}
