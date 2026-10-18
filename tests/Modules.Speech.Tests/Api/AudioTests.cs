using System.Net;
using System.Net.Http.Headers;
using EnglishSchool.Modules.Speech.Domain;
using EnglishSchool.Modules.Speech.Infrastructure;
using EnglishSchool.Modules.Speech.Tests.TestSupport;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace EnglishSchool.Modules.Speech.Tests.Api;

[Collection(SpeechApiFactory.CollectionName)]
public sealed class AudioTests(SpeechApiFactory factory)
{
    /// <summary>2-kun: 24 soʻz va 5 ibora, hammasi har xil matn.</summary>
    private const int DayTwoVocabulary = 29;

    [Fact]
    public async Task Unlocking_a_day_synthesizes_each_word_and_phrase_once_for_everyone()
    {
        using var student = await factory.CreateStudentAsync();
        using var classmate = await factory.CreateStudentAsync();
        var synthesesBefore = factory.Provider.Syntheses.Count;

        await factory.UnlockDayAsync(student.UserId, 2);
        await factory.UnlockDayAsync(classmate.UserId, 2);

        var syntheses = factory.Provider.Syntheses.Skip(synthesesBefore).Select(r => r.Json).ToList();
        Assert.Equal(DayTwoVocabulary, syntheses.Count);
        Assert.Equal(
            ("tts-1", "happy", "alloy", "mp3", 0.9),
            (syntheses[0].GetProperty("model").GetString(), syntheses[0].GetProperty("input").GetString(), syntheses[0].GetProperty("voice").GetString(),
                syntheses[0].GetProperty("response_format").GetString(), syntheses[0].GetProperty("speed").GetDouble()));

        var clips = await classmate.GetDayClipsAsync(2);
        Assert.Equal(DayTwoVocabulary, clips.Count);
        Assert.Equal(["happy", "sad", "tired"], clips.Take(3).Select(c => c.Text));
        Assert.Equal(5, clips.Count(c => c.IsPhrase));
        Assert.All(clips, c => Assert.NotNull(c.AudioId));

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SpeechDbContext>();
        var happy = await db.AudioArtifacts.AsNoTracking().SingleAsync(a => a.Id == clips[0].AudioId);
        Assert.Equal(
            (AudioKind.Synthesized, (Guid?)null, "happy", "alloy", "audio/mpeg", $"tts/{happy.CacheKey}.mp3"),
            (happy.Kind, happy.OwnerId, happy.Text, happy.Voice, happy.ContentType, happy.StorageKey));
        Assert.Equal(factory.Provider.Mp3, factory.Storage.Files[happy.StorageKey].Content);
    }

    [Fact]
    public async Task Day_audio_lists_clips_not_synthesized_yet_and_rejects_days_outside_the_course()
    {
        using var student = await factory.CreateStudentAsync();
        using var anonymous = factory.CreateClient();

        var pending = await student.GetDayClipsAsync(3);
        using var beyond = await student.Client.GetAsync(new Uri("/api/v1/speech/days/57/audio", UriKind.Relative));
        using var signedOut = await anonymous.GetAsync(new Uri("/api/v1/speech/days/3/audio", UriKind.Relative));

        Assert.Equal(30, pending.Count);
        Assert.All(pending, c => Assert.Null(c.AudioId));
        Assert.Equal((HttpStatusCode.NotFound, HttpStatusCode.Unauthorized), (beyond.StatusCode, signedOut.StatusCode));
    }

    [Fact]
    public async Task Recordings_are_private_to_their_owner_while_clips_are_shared()
    {
        using var owner = await factory.CreateStudentAsync();
        using var classmate = await factory.CreateStudentAsync();
        using var anonymous = factory.CreateClient();
        factory.Provider.Transcribes("Hello.");
        var wav = AudioSamples.Wav();
        var recordingId = (await owner.AssessSuccessfullyAsync(wav, "Hello")).RecordingId;
        await factory.UnlockDayAsync(owner.UserId, 1);
        var clipId = (await classmate.GetDayClipsAsync(1))[0].AudioId!.Value;

        using var own = await owner.Client.GetAudioAsync(recordingId);
        using var others = await classmate.Client.GetAudioAsync(recordingId);
        using var unknown = await owner.Client.GetAudioAsync(Guid.NewGuid());
        using var signedOut = await anonymous.GetAudioAsync(recordingId);
        using var shared = await classmate.Client.GetAudioAsync(clipId);

        Assert.Equal((HttpStatusCode.OK, "audio/wav"), (own.StatusCode, own.Content.Headers.ContentType?.MediaType));
        Assert.True(own.Headers.CacheControl is { Private: true, NoCache: true }, own.Headers.CacheControl?.ToString());
        Assert.Equal(wav, await own.Content.ReadAsByteArrayAsync());
        Assert.Equal((HttpStatusCode.NotFound, HttpStatusCode.NotFound, HttpStatusCode.Unauthorized), (others.StatusCode, unknown.StatusCode, signedOut.StatusCode));
        Assert.Equal((HttpStatusCode.OK, "audio/mpeg"), (shared.StatusCode, shared.Content.Headers.ContentType?.MediaType));
        Assert.True(shared.Headers.CacheControl is { Private: true, MaxAge.TotalDays: >= 365 }, shared.Headers.CacheControl?.ToString());
    }

    [Fact]
    public async Task Audio_supports_conditional_and_range_requests_for_players()
    {
        using var student = await factory.CreateStudentAsync();
        factory.Provider.Transcribes("Hello.");
        var recordingId = (await student.AssessSuccessfullyAsync(AudioSamples.Wav(), "Hello")).RecordingId;
        using var first = await student.Client.GetAudioAsync(recordingId);
        var etag = Assert.IsType<EntityTagHeaderValue>(first.Headers.ETag);

        using var conditional = new HttpRequestMessage(HttpMethod.Get, $"/api/v1/speech/audio/{recordingId}") { Headers = { IfNoneMatch = { etag } } };
        using var notModified = await student.Client.SendAsync(conditional);
        using var ranged = new HttpRequestMessage(HttpMethod.Get, $"/api/v1/speech/audio/{recordingId}") { Headers = { Range = new RangeHeaderValue(0, 3) } };
        using var partial = await student.Client.SendAsync(ranged);

        Assert.Equal(HttpStatusCode.NotModified, notModified.StatusCode);
        Assert.Equal(HttpStatusCode.PartialContent, partial.StatusCode);
        Assert.Equal("RIFF"u8.ToArray(), await partial.Content.ReadAsByteArrayAsync());
    }
}
