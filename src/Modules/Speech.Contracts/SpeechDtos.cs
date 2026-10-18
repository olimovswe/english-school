namespace EnglishSchool.Modules.Speech.Contracts;

/// <summary><see cref="Events.TranscriptReady.Purpose"/> qiymatlari.</summary>
public static class SpeechPurposes
{
    /// <summary>Talaffuz mashqi: yozuv berilgan matn bilan solishtirilib baholandi.</summary>
    public const string Pronunciation = "pronunciation";
}

/// <summary>Talaffuz bahosi (0–100 shkalada).</summary>
/// <param name="RecordingId">Oʻquvchining saqlangan yozuvi: <c>GET /speech/audio/{id}</c>.</param>
/// <param name="ReferenceAudioId">Shu matnning namuna talaffuzi (TTS), agar u allaqachon sintez qilingan boʻlsa.</param>
/// <param name="AccuracyScore">Fonetik aniqlik: matndagi har bir soʻz qanchalik toʻgʻri eshitilgani.</param>
/// <param name="CompletenessScore">Matn soʻzlarining qancha qismi aytilgani.</param>
/// <param name="FluencyScore">Ravonlik (toʻxtashlar va tezlik); qisqa matnda yoki soʻz vaqtlari boʻlmasa <c>null</c>.</param>
/// <param name="Verdict"><c>Correct</c> (≥ 90), <c>PartiallyCorrect</c> (≥ 60) yoki <c>Incorrect</c>.</param>
public sealed record PronunciationAssessmentDto(
    Guid AssessmentId,
    Guid RecordingId,
    Guid? ReferenceAudioId,
    string TargetText,
    string Transcript,
    decimal OverallScore,
    decimal AccuracyScore,
    decimal CompletenessScore,
    decimal? FluencyScore,
    string Verdict,
    FluencyMetricsDto? Fluency,
    IReadOnlyList<WordAssessmentDto> Words,
    DateTimeOffset CreatedAt);

/// <param name="Expected">Matndagi soʻz; ortiqcha aytilgan soʻzda <c>null</c>.</param>
/// <param name="Heard">Eshitilgan soʻz; tushib qolgan soʻzda <c>null</c>.</param>
/// <param name="Status"><c>Correct</c>, <c>Close</c>, <c>Mispronounced</c>, <c>Missing</c> yoki <c>Extra</c>.</param>
/// <param name="FeedbackCode">Aniqlangan xato turi: <c>TH_SOUND</c>, <c>W_V_SOUND</c>, <c>FINAL_CONSONANT</c>, <c>MISSING_WORD</c>, <c>EXTRA_WORD</c>.</param>
/// <param name="Hint">Xato turi boʻyicha oʻzbekcha maslahat.</param>
public sealed record WordAssessmentDto(
    string? Expected,
    string? Heard,
    double Similarity,
    string Status,
    string? FeedbackCode,
    string? Hint);

/// <param name="PauseCount">Soʻzlar orasidagi sezilarli (≥ 0,5 s) toʻxtashlar.</param>
public sealed record FluencyMetricsDto(int SpeechDurationMs, double WordsPerMinute, int PauseCount, int LongestPauseMs);

/// <summary>Kurs kuni lugʻatining bitta elementi va uning namuna talaffuzi.</summary>
/// <param name="AudioId">Sintez qilingan klip; hali tayyor boʻlmasa <c>null</c> (klipni Worker yaratadi).</param>
public sealed record VocabularyClipDto(Guid VocabularyItemId, string Text, bool IsPhrase, Guid? AudioId);
