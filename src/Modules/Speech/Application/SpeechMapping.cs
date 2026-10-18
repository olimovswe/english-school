using EnglishSchool.Modules.Speech.Contracts;
using EnglishSchool.Modules.Speech.Domain;
using EnglishSchool.Modules.Speech.Scoring;

namespace EnglishSchool.Modules.Speech.Application;

internal static class SpeechMapping
{
    public static PronunciationAssessmentDto ToDto(this PronunciationAssessment assessment, Guid? referenceAudioId) => new(
        assessment.Id,
        assessment.RecordingId,
        referenceAudioId,
        assessment.TargetText,
        assessment.Transcript,
        assessment.OverallScore,
        assessment.AccuracyScore,
        assessment.CompletenessScore,
        assessment.FluencyScore,
        assessment.Verdict.ToString(),
        assessment is { SpeechDurationMs: { } duration, WordsPerMinute: { } rate, PauseCount: { } pauses, LongestPauseMs: { } longest }
            ? new FluencyMetricsDto(duration, rate, pauses, longest)
            : null,
        [.. assessment.Words.Select(w => new WordAssessmentDto(
            w.Expected, w.Heard, w.Similarity, w.Status.ToString(), w.FeedbackCode, FeedbackCodes.Hint(w.FeedbackCode)))],
        assessment.CreatedAt);
}
