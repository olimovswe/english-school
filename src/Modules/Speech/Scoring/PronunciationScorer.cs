using EnglishSchool.Modules.Speech.Domain;
using EnglishSchool.Modules.Speech.Providers;

namespace EnglishSchool.Modules.Speech.Scoring;

/// <summary>
/// Whisper transkriptini aytilishi kerak boʻlgan matn bilan solishtiradi. Ikkalasi soʻzlarga ajratiladi va
/// soʻzlar darajasida Levenshtein moslashuvi qilinadi: almashtirish narxi — ikki soʻzning oʻxshashligi
/// (yozuvi va talaffuz kaliti), tushib qolgan yoki ortiqcha soʻz — 1. Har bir matn soʻzi shu moslashuvdan baholanadi.
/// </summary>
internal static class PronunciationScorer
{
    /// <summary>Shundan oʻxshash soʻz «yaqin» (bitta tovush yoki omofon).</summary>
    public const double CloseThreshold = 0.75;

    /// <summary>Shundan pastda eshitilgan soʻz boshqa soʻz hisoblanadi.</summary>
    public const double MispronouncedThreshold = 0.45;

    /// <summary>Soʻzlar orasidagi shundan uzun jimlik — toʻxtash.</summary>
    public const double PauseSeconds = 0.5;

    /// <summary>Notoʻgʻri aytilgan soʻz (boshqa soʻz eshitilgan) oʻxshashligining faqat shu ulushini oladi.</summary>
    public const double MispronouncedCredit = 0.5;

    /// <summary>Har bir ortiqcha soʻz aniqlik maxrajiga shuncha soʻz qoʻshadi.</summary>
    public const double ExtraWordPenalty = 0.5;

    /// <summary>Boshlovchi uchun ravon tezlik: bundan sekinroq nutq ravonlik bahosini kamaytiradi.</summary>
    public const double FluentWordsPerMinute = 70;

    /// <summary>Bir-ikki soʻzda ravonlikni oʻlchab boʻlmaydi.</summary>
    public const int MinimumWordsForFluency = 3;

    private const decimal CorrectFrom = 90;
    private const decimal PartiallyCorrectFrom = 60;
    private const double Epsilon = 1e-9;

    public static PronunciationScore Score(string targetText, Transcription transcription)
    {
        ArgumentNullException.ThrowIfNull(transcription);

        var expected = TextNormalizer.Words(targetText);
        if (expected.Count == 0)
        {
            throw new ArgumentException("Baholanadigan matnda soʻz yoʻq.", nameof(targetText));
        }

        var words = Align(expected, TextNormalizer.Words(transcription.Text));
        var target = words.Where(w => w.Expected is not null).ToList();

        var extra = words.Count - target.Count;
        var accuracy = 100 * target.Sum(Credit) / (target.Count + (ExtraWordPenalty * extra));
        var completeness = 100.0 * target.Count(w => w.Status != WordStatus.Missing) / target.Count;
        var (fluency, metrics) = Fluency(expected.Count, transcription.Words);

        var accuracyScore = Round(accuracy);
        var overall = fluency is null ? accuracyScore : Round((0.8 * accuracy) + (0.2 * (double)fluency.Value));
        // «Toʻgʻri» faqat har bir soʻz toʻgʻri yoki juda yaqin aytilganda: bitta notoʻgʻri soʻz uzun gapda ham sezilsin.
        var verdict = overall >= CorrectFrom && target.All(w => w.Status is WordStatus.Correct or WordStatus.Close)
            ? PronunciationVerdict.Correct
            : overall >= PartiallyCorrectFrom ? PronunciationVerdict.PartiallyCorrect
            : PronunciationVerdict.Incorrect;

        return new PronunciationScore(accuracyScore, Round(completeness), fluency, overall, verdict, metrics, words);
    }

    /// <summary>
    /// 1 — bir xil soʻz. Aks holda yozuv va talaffuz kaliti oʻxshashligining oʻrtachasi; kaliti bir xil soʻz
    /// (omofon yoki faqat unlisi farqli) kamida «yaqin».
    /// </summary>
    public static double Similarity(string expected, string heard)
    {
        if (string.Equals(expected, heard, StringComparison.Ordinal))
        {
            return 1;
        }

        var phonetic = Levenshtein.Similarity(PhoneticKey.Encode(expected), PhoneticKey.Encode(heard));
        var spelling = Levenshtein.Similarity(expected, heard);
        var similarity = (phonetic + spelling) / 2;
        return Math.Round(phonetic >= 1 ? Math.Max(CloseThreshold + 0.05, similarity) : similarity, 3);
    }

    private static double Credit(WordAssessment word) => word.Status switch
    {
        WordStatus.Correct or WordStatus.Close => word.Similarity,
        WordStatus.Mispronounced => word.Similarity * MispronouncedCredit,
        _ => 0,
    };

    private static List<WordAssessment> Align(IReadOnlyList<string> expected, IReadOnlyList<string> heard)
    {
        var similarity = new double[expected.Count, heard.Count];
        var cost = new double[expected.Count + 1, heard.Count + 1];
        for (var i = 0; i <= expected.Count; i++)
        {
            cost[i, 0] = i;
        }

        for (var j = 0; j <= heard.Count; j++)
        {
            cost[0, j] = j;
        }

        for (var i = 1; i <= expected.Count; i++)
        {
            for (var j = 1; j <= heard.Count; j++)
            {
                similarity[i - 1, j - 1] = Similarity(expected[i - 1], heard[j - 1]);
                cost[i, j] = Math.Min(
                    cost[i - 1, j - 1] + (1 - similarity[i - 1, j - 1]),
                    Math.Min(cost[i - 1, j] + 1, cost[i, j - 1] + 1));
            }
        }

        // Oxiridan orqaga: teng narxda juftlik (almashtirish) afzal.
        var words = new List<WordAssessment>(Math.Max(expected.Count, heard.Count));
        var (row, column) = (expected.Count, heard.Count);
        while (row > 0 || column > 0)
        {
            if (row > 0 && column > 0 &&
                Math.Abs(cost[row, column] - (cost[row - 1, column - 1] + 1 - similarity[row - 1, column - 1])) < Epsilon)
            {
                words.Add(Pair(expected[row - 1], heard[column - 1], similarity[row - 1, column - 1]));
                (row, column) = (row - 1, column - 1);
            }
            else if (row > 0 && Math.Abs(cost[row, column] - (cost[row - 1, column] + 1)) < Epsilon)
            {
                words.Add(new WordAssessment(expected[row - 1], null, 0, WordStatus.Missing, FeedbackCodes.MissingWord));
                row--;
            }
            else
            {
                words.Add(new WordAssessment(null, heard[column - 1], 0, WordStatus.Extra, FeedbackCodes.ExtraWord));
                column--;
            }
        }

        words.Reverse();
        return words;
    }

    private static WordAssessment Pair(string expected, string heard, double similarity)
    {
        var status = similarity >= 1 ? WordStatus.Correct
            : similarity >= CloseThreshold ? WordStatus.Close
            : similarity >= MispronouncedThreshold ? WordStatus.Mispronounced
            : WordStatus.Missing;

        var code = status switch
        {
            WordStatus.Correct => null,
            WordStatus.Missing => DetectError(expected, heard) ?? FeedbackCodes.WrongWord,
            _ => DetectError(expected, heard),
        };

        return new WordAssessment(expected, heard, similarity, status, code);
    }

    /// <summary>Talaffuz kalitlaridagi farqdan maʼlum xato turini topadi.</summary>
    private static string? DetectError(string expected, string heard)
    {
        var want = PhoneticKey.Encode(expected);
        var got = PhoneticKey.Encode(heard);

        if (want.Contains('θ', StringComparison.Ordinal) && "sztdf".Any(c => want.Replace('θ', c) == got))
        {
            return FeedbackCodes.ThSound;
        }

        if (want != got && (want.Replace('w', 'v') == got || want.Replace('v', 'w') == got))
        {
            return FeedbackCodes.WvSound;
        }

        if (want.Length > 1 && want[^1] != PhoneticKey.Vowel && want[..^1] == got)
        {
            return FeedbackCodes.FinalConsonant;
        }

        return null;
    }

    /// <summary>
    /// Ravonlik soʻz vaqt belgilaridan: toʻxtashlar ulushi nutq vaqtidan ayiriladi, sekin nutq
    /// (<see cref="FluentWordsPerMinute"/>dan past) mutanosib kamaytiriladi.
    /// </summary>
    private static (decimal? Score, FluencyMetrics? Metrics) Fluency(int expectedWords, IReadOnlyList<TranscribedWord> words)
    {
        if (expectedWords < MinimumWordsForFluency || words.Count < 2)
        {
            return (null, null);
        }

        var ordered = words.OrderBy(w => w.Start).ToList();
        var span = ordered[^1].End - ordered[0].Start;
        if (span <= 0)
        {
            return (null, null);
        }

        var (pauses, pauseSeconds, longestPause) = (0, 0.0, 0.0);
        for (var i = 1; i < ordered.Count; i++)
        {
            var gap = ordered[i].Start - ordered[i - 1].End;
            if (gap >= PauseSeconds)
            {
                pauses++;
                pauseSeconds += gap;
                longestPause = Math.Max(longestPause, gap);
            }
        }

        var wordsPerMinute = ordered.Count / span * 60;
        var fluency = 100 * (1 - (pauseSeconds / span)) * Math.Min(1, wordsPerMinute / FluentWordsPerMinute);
        var metrics = new FluencyMetrics(
            (int)Math.Round(span * 1000), Math.Round(wordsPerMinute, 1), pauses, (int)Math.Round(longestPause * 1000));
        return (Round(fluency), metrics);
    }

    private static decimal Round(double score) =>
        Math.Round((decimal)Math.Clamp(score, 0, 100), 1, MidpointRounding.AwayFromZero);
}
