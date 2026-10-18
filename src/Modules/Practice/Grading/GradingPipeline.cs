using EnglishSchool.Modules.Practice.Domain;

namespace EnglishSchool.Modules.Practice.Grading;

/// <summary>
/// Javobni eng arzon tierdan boshlab baholaydi; birinchi aniq qaror bergan tier natijasi qaytadi:
/// 0 — qoidalar (aniq moslik, regex, variant), 1 — normalizatsiya va imlo, 2–3 — AiGateway (8-hafta).
/// Hech qaysi tier hal qilmasa, javob notoʻgʻri deb baholanadi va toʻgʻri javob koʻrsatiladi.
/// </summary>
internal sealed class GradingPipeline(IEnumerable<IGradingTier> tiers)
{
    private readonly IGradingTier[] _tiers = [.. tiers.OrderBy(tier => tier.Tier)];

    public async Task<GradingOutcome> GradeAsync(GradingContext context, CancellationToken cancellationToken)
    {
        var lastTier = 0;
        foreach (var tier in _tiers.Where(tier => tier.IsAvailable))
        {
            lastTier = tier.Tier;
            if (await tier.TryGradeAsync(context, cancellationToken) is { } outcome)
            {
                return outcome;
            }
        }

        var exercise = context.Exercise;
        return new GradingOutcome(
            Verdict.Incorrect, 0m, lastTier, [new GradingFeedback(null, ExerciseAnswers.Correct(exercise), exercise.Explanation)]);
    }
}

internal static class ExerciseAnswers
{
    /// <summary>Foydalanuvchiga koʻrsatiladigan toʻgʻri javob.</summary>
    public static string Correct(Exercise exercise) => exercise.Type switch
    {
        ExerciseType.Matching => string.Join("; ", exercise.Options.OrderBy(o => o.Position).Select(o => $"{o.Text} — {o.MatchText}")),
        _ => exercise.Options.Where(o => o.IsCorrect).OrderBy(o => o.Position).Select(o => o.Text).FirstOrDefault() ?? string.Empty,
    };

    public static GradingOutcome Mistake(Exercise exercise, ExerciseOption mistake, int tier) =>
        new(Verdict.Incorrect, 0m, tier, [new GradingFeedback(mistake.ErrorCode, Correct(exercise), mistake.Feedback ?? exercise.Explanation)]);
}
