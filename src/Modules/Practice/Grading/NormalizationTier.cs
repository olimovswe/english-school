using EnglishSchool.Modules.Practice.Domain;

namespace EnglishSchool.Modules.Practice.Grading;

/// <summary>
/// Tier 1: statik qoidalar. GapFill javobini registr, boʻshliq, egri apostrof va qisqartmalardan tozalab qayta
/// solishtiradi (hello = Hello, is not = isn't). Imlo xatosi boʻlsa, javob qisman toʻgʻri deb baholanadi.
/// </summary>
internal sealed class NormalizationTier : IGradingTier
{
    private const decimal SpellingScore = 0.5m;

    public int Tier => 1;

    public bool IsAvailable => true;

    public ValueTask<GradingOutcome?> TryGradeAsync(GradingContext context, CancellationToken cancellationToken) =>
        ValueTask.FromResult(context.Exercise.Type == ExerciseType.GapFill ? Grade(context.Exercise, context.Answer.Text ?? string.Empty) : null);

    private GradingOutcome? Grade(Exercise exercise, string answer)
    {
        var canonical = AnswerText.Canonicalize(answer);
        var options = exercise.Options.OrderBy(o => o.Position).ToList();

        if (options.Any(o => o.IsCorrect && AnswerText.Canonicalize(o.Text) == canonical))
        {
            return GradingOutcome.Correct(Tier);
        }

        if (options.FirstOrDefault(o => !o.IsCorrect && AnswerText.Canonicalize(o.Text) == canonical) is { } mistake)
        {
            return ExerciseAnswers.Mistake(exercise, mistake, Tier);
        }

        var simplified = AnswerText.Simplify(answer);
        if (options.FirstOrDefault(o => o.IsCorrect && AnswerText.IsLikelyTypo(simplified, AnswerText.Simplify(o.Text))) is { } intended)
        {
            return new GradingOutcome(
                Verdict.PartiallyCorrect,
                SpellingScore,
                Tier,
                [new GradingFeedback(ErrorCodes.Spelling, intended.Text, $"Imlo xatosi: toʻgʻri yozilishi — {intended.Text}.")]);
        }

        return null;
    }
}
