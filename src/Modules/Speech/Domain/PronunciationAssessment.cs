namespace EnglishSchool.Modules.Speech.Domain;

/// <summary>
/// Oʻquvchi yozuvining bitta talaffuz bahosi: nimani aytishi kerak edi (<see cref="TargetText"/>),
/// Whisper nimani eshitdi (<see cref="Transcript"/>) va bu ikkisi soʻzma-soʻz qanchalik mos.
/// Yozuvning oʻzi <see cref="RecordingId"/> boʻyicha audio omborida qoladi.
/// </summary>
internal sealed class PronunciationAssessment
{
    private PronunciationAssessment()
    {
    }

    public Guid Id { get; private init; } = Guid.CreateVersion7();

    /// <summary>Identity'dagi foydalanuvchi. Boshqa modul jadvaliga FK yoʻq.</summary>
    public Guid UserId { get; private init; }

    public Guid RecordingId { get; private init; }

    public string TargetText { get; private init; } = string.Empty;

    public string Transcript { get; private init; } = string.Empty;

    /// <summary>Transkripsiya tili (ISO 639-1). Kurs ingliz tilida: <c>en</c>.</summary>
    public string Language { get; private init; } = string.Empty;

    public decimal AccuracyScore { get; private init; }

    public decimal CompletenessScore { get; private init; }

    public decimal? FluencyScore { get; private init; }

    public decimal OverallScore { get; private init; }

    public PronunciationVerdict Verdict { get; private init; }

    public int? SpeechDurationMs { get; private init; }

    public double? WordsPerMinute { get; private init; }

    public int? PauseCount { get; private init; }

    public int? LongestPauseMs { get; private init; }

    /// <summary>Soʻzma-soʻz moslashuv (JSON ustunda). EF complex collection <c>IList</c> talab qiladi.</summary>
    public IList<WordAssessment> Words { get; private init; } = [];

    public DateTimeOffset CreatedAt { get; private init; }

    public static PronunciationAssessment Record(
        Guid userId,
        AudioArtifact recording,
        string targetText,
        string transcript,
        string language,
        PronunciationScore score,
        DateTimeOffset now)
    {
        if (recording.Kind != AudioKind.Recording || recording.OwnerId != userId)
        {
            throw new ArgumentException("Baho faqat shu oʻquvchining oʻz yozuviga beriladi.", nameof(recording));
        }

        return new PronunciationAssessment
        {
            UserId = userId,
            RecordingId = recording.Id,
            TargetText = targetText,
            Transcript = transcript,
            Language = language,
            AccuracyScore = score.Accuracy,
            CompletenessScore = score.Completeness,
            FluencyScore = score.Fluency,
            OverallScore = score.Overall,
            Verdict = score.Verdict,
            SpeechDurationMs = score.FluencyMetrics?.SpeechDurationMs,
            WordsPerMinute = score.FluencyMetrics?.WordsPerMinute,
            PauseCount = score.FluencyMetrics?.PauseCount,
            LongestPauseMs = score.FluencyMetrics?.LongestPauseMs,
            Words = [.. score.Words],
            CreatedAt = now,
        };
    }
}
