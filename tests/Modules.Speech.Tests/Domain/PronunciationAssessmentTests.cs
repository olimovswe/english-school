using EnglishSchool.BuildingBlocks.Abstractions.Events;
using EnglishSchool.Modules.Speech.Contracts;
using EnglishSchool.Modules.Speech.Contracts.Events;
using EnglishSchool.Modules.Speech.Domain;

namespace EnglishSchool.Modules.Speech.Tests.Domain;

public sealed class PronunciationAssessmentTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 18, 9, 0, 0, TimeSpan.Zero);

    private static readonly PronunciationScore Score = new(
        80m, 100m, null, 80m, PronunciationVerdict.PartiallyCorrect, null,
        [new WordAssessment("hello", "hollow", 0.8, WordStatus.Close, null)]);

    [Fact]
    public void Recording_an_assessment_publishes_the_transcript_for_other_modules()
    {
        var userId = Guid.NewGuid();
        var recording = AudioArtifact.Recording(userId, AudioFormat.Webm, 2048, new string('a', 64), 1500, Now);

        var assessment = PronunciationAssessment.Record(userId, recording, "Hello", "Hollow.", "en", Score, Now);

        var published = Assert.IsType<TranscriptReady>(Assert.Single(((IIntegrationEventSource)assessment).IntegrationEvents));
        Assert.Equal((userId, assessment.Id, SpeechPurposes.Pronunciation, "Hollow."), (published.UserId, published.JobId, published.Purpose, published.Transcript));
        Assert.Equal((recording.Id, 80m, PronunciationVerdict.PartiallyCorrect), (assessment.RecordingId, assessment.OverallScore, assessment.Verdict));
        Assert.Equal(Score.Words, assessment.Words);
    }

    [Fact]
    public void Only_the_students_own_recording_can_be_assessed()
    {
        var recording = AudioArtifact.Recording(Guid.NewGuid(), AudioFormat.Wav, 2048, new string('a', 64), null, Now);
        var clip = AudioArtifact.Synthesized("Hello", "alloy", new string('b', 64), AudioFormat.Mp3, 512, new string('c', 64), Now);

        Assert.Throws<ArgumentException>(() => PronunciationAssessment.Record(Guid.NewGuid(), recording, "Hello", "Hello", "en", Score, Now));
        Assert.Throws<ArgumentException>(() => PronunciationAssessment.Record(Guid.NewGuid(), clip, "Hello", "Hello", "en", Score, Now));
    }

    [Fact]
    public void Recordings_are_private_while_synthesized_clips_are_shared()
    {
        var owner = Guid.NewGuid();
        var recording = AudioArtifact.Recording(owner, AudioFormat.Webm, 2048, new string('a', 64), null, Now);
        var cacheKey = new string('b', 64);
        var clip = AudioArtifact.Synthesized("Hello", "alloy", cacheKey, AudioFormat.Mp3, 512, new string('c', 64), Now);

        Assert.Equal($"recordings/{owner:N}/{recording.Id:N}.webm", recording.StorageKey);
        Assert.Equal($"tts/{cacheKey}.mp3", clip.StorageKey);
        Assert.Equal((true, false), (recording.IsAvailableTo(owner), recording.IsAvailableTo(Guid.NewGuid())));
        Assert.True(clip.IsAvailableTo(Guid.NewGuid()));
    }
}
