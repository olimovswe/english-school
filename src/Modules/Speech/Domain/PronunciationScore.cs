namespace EnglishSchool.Modules.Speech.Domain;

internal enum PronunciationVerdict
{
    Correct,
    PartiallyCorrect,
    Incorrect,
}

internal enum WordStatus
{
    /// <summary>Soʻz aynan (yoki teng shaklda: <c>I'm</c> = <c>I am</c>, <c>7</c> = <c>seven</c>) eshitildi.</summary>
    Correct,

    /// <summary>Juda yaqin: bitta tovush farqi yoki talaffuzi bir xil boshqa yozuv (<c>their</c> / <c>there</c>).</summary>
    Close,

    /// <summary>Oʻxshash, lekin boshqa soʻz eshitildi (<c>think</c> oʻrniga <c>sink</c>).</summary>
    Mispronounced,

    /// <summary>Soʻz eshitilmadi yoki uning oʻrnida umuman boshqa soʻz aytildi.</summary>
    Missing,

    /// <summary>Matnda yoʻq soʻz aytildi.</summary>
    Extra,
}

/// <summary>Matn va transkript soʻzlarining bitta moslashuvi.</summary>
/// <param name="Expected">Matndagi soʻz; ortiqcha aytilgan soʻzda <c>null</c>.</param>
/// <param name="Heard">Eshitilgan soʻz; tushib qolgan soʻzda <c>null</c>.</param>
/// <param name="Similarity">0 (umuman boshqa) … 1 (aynan): yozuv va talaffuz kaliti boʻyicha.</param>
/// <param name="FeedbackCode">Oʻzbek tilida soʻzlashuvchilarga xos xato turi (<c>TH_SOUND</c>, …), aniqlansa.</param>
internal sealed record WordAssessment(string? Expected, string? Heard, double Similarity, WordStatus Status, string? FeedbackCode);

/// <summary>Soʻzlarning vaqt belgilaridan olingan ravonlik koʻrsatkichlari.</summary>
/// <param name="SpeechDurationMs">Birinchi soʻz boshidan oxirgi soʻz oxirigacha.</param>
/// <param name="PauseCount">Soʻzlar orasidagi sezilarli (≥ 0,5 s) toʻxtashlar.</param>
internal sealed record FluencyMetrics(int SpeechDurationMs, double WordsPerMinute, int PauseCount, int LongestPauseMs);

/// <summary>Talaffuz bahosi (0–100 shkalada).</summary>
/// <param name="Accuracy">Fonetik aniqlik: matndagi har bir soʻz qanchalik toʻgʻri eshitilgani.</param>
/// <param name="Completeness">Matn soʻzlarining qancha qismi aytilgani.</param>
/// <param name="Fluency">Ravonlik; provayder soʻz vaqtlarini bermasa <c>null</c>.</param>
internal sealed record PronunciationScore(
    decimal Accuracy,
    decimal Completeness,
    decimal? Fluency,
    decimal Overall,
    PronunciationVerdict Verdict,
    FluencyMetrics? FluencyMetrics,
    IReadOnlyList<WordAssessment> Words);
