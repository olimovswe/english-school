using EnglishSchool.Modules.Practice.Domain;

namespace EnglishSchool.Modules.Practice.Grading;

internal sealed record SubmittedMatch(Guid OptionId, string MatchText);

/// <summary>Mashq turiga qarab bittasi toʻldiriladi: matn (GapFill), variant (MultipleChoice) yoki juftliklar (Matching).</summary>
internal sealed record SubmittedAnswer(string? Text, Guid? SelectedOptionId, IReadOnlyList<SubmittedMatch> Matches);

internal sealed record GradingContext(Guid UserId, Exercise Exercise, SubmittedAnswer Answer);

/// <param name="ErrorCode">Taksonomiyadagi kod; xato turi aniqlanmagan boʻlsa <c>null</c>.</param>
internal sealed record GradingFeedback(string? ErrorCode, string Correction, string? Explanation);

internal sealed record GradingOutcome(Verdict Verdict, decimal Score, int Tier, IReadOnlyList<GradingFeedback> Feedback)
{
    public static GradingOutcome Correct(int tier) => new(Verdict.Correct, 1m, tier, []);
}

/// <summary>
/// Baholash bosqichi. Javobni hal qila olsa natija qaytaradi, aks holda <c>null</c>: javob keyingi,
/// qimmatroq tierga oʻtadi.
/// </summary>
internal interface IGradingTier
{
    int Tier { get; }

    /// <summary>Masalan, AiGateway hali ulanmagan boʻlsa, tier oʻtkazib yuboriladi.</summary>
    bool IsAvailable { get; }

    ValueTask<GradingOutcome?> TryGradeAsync(GradingContext context, CancellationToken cancellationToken);
}
