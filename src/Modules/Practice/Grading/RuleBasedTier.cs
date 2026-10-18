using System.Text.RegularExpressions;
using EnglishSchool.Modules.Practice.Domain;

namespace EnglishSchool.Modules.Practice.Grading;

/// <summary>
/// Tier 0: bir zumda, deterministik qarorlar. MultipleChoice va Matching shu yerda toʻliq baholanadi;
/// GapFill — faqat javob qabul qilinadigan javob, regex yoki maʼlum xato bilan aynan mos kelsa.
/// </summary>
internal sealed class RuleBasedTier : IGradingTier
{
    private static readonly TimeSpan RegexTimeout = TimeSpan.FromMilliseconds(100);

    public int Tier => 0;

    public bool IsAvailable => true;

    public ValueTask<GradingOutcome?> TryGradeAsync(GradingContext context, CancellationToken cancellationToken) =>
        ValueTask.FromResult(context.Exercise.Type switch
        {
            ExerciseType.MultipleChoice => GradeChoice(context.Exercise, context.Answer),
            ExerciseType.Matching => GradeMatching(context.Exercise, context.Answer),
            ExerciseType.GapFill => GradeGap(context.Exercise, context.Answer),
            _ => null,
        });

    private GradingOutcome? GradeChoice(Exercise exercise, SubmittedAnswer answer)
    {
        var selected = exercise.Options.SingleOrDefault(o => o.Id == answer.SelectedOptionId);
        if (selected is null)
        {
            return null;
        }

        return selected.IsCorrect ? GradingOutcome.Correct(Tier) : ExerciseAnswers.Mistake(exercise, selected, Tier);
    }

    private GradingOutcome GradeMatching(Exercise exercise, SubmittedAnswer answer)
    {
        var submitted = answer.Matches.ToDictionary(m => m.OptionId, m => m.MatchText.Trim());
        var wrong = exercise.Options
            .OrderBy(o => o.Position)
            .Where(o => !submitted.TryGetValue(o.Id, out var text) || !string.Equals(text, o.MatchText, StringComparison.OrdinalIgnoreCase))
            .ToList();

        var total = exercise.Options.Count;
        var score = Math.Round((decimal)(total - wrong.Count) / total, 2);
        var verdict = wrong.Count == 0 ? Verdict.Correct : wrong.Count == total ? Verdict.Incorrect : Verdict.PartiallyCorrect;

        return new GradingOutcome(
            verdict,
            score,
            Tier,
            [.. wrong.Select(o => new GradingFeedback(ErrorCodes.Vocabulary, $"{o.Text} — {o.MatchText}", null))]);
    }

    private GradingOutcome? GradeGap(Exercise exercise, SubmittedAnswer answer)
    {
        var text = answer.Text?.Trim() ?? string.Empty;

        if (exercise.Options.Any(o => o.IsCorrect && string.Equals(o.Text, text, StringComparison.Ordinal))
            || MatchesPattern(exercise.AnswerPattern, text))
        {
            return GradingOutcome.Correct(Tier);
        }

        var mistake = exercise.Options.FirstOrDefault(o => !o.IsCorrect && string.Equals(o.Text, text, StringComparison.Ordinal));
        return mistake is null ? null : ExerciseAnswers.Mistake(exercise, mistake, Tier);
    }

    /// <summary>Regex mualliflar kontentidan; javob esa foydalanuvchidan, shuning uchun timeout bilan.</summary>
    private static bool MatchesPattern(string? pattern, string text)
    {
        if (string.IsNullOrEmpty(pattern))
        {
            return false;
        }

        try
        {
            return Regex.IsMatch(text, $"^(?:{pattern})$", RegexOptions.CultureInvariant, RegexTimeout);
        }
        catch (RegexMatchTimeoutException)
        {
            return false;
        }
    }
}
