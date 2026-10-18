using EnglishSchool.Modules.Practice.Domain;
using EnglishSchool.Modules.Practice.Grading;

namespace EnglishSchool.Modules.Practice.Tests.TestSupport;

/// <summary>Bazasiz baholash testlari uchun mashqlar va javoblar.</summary>
internal static class TestExercises
{
    public const string Rule = "Qoida izohi.";

    public static Exercise GapFill(string[] accepted, (string Text, string ErrorCode)[]? mistakes = null, string? pattern = null) =>
        Build(ExerciseType.GapFill, pattern,
        [
            .. accepted.Select(text => (text, true, (string?)null, (string?)null)),
            .. (mistakes ?? []).Select(m => (m.Text, false, (string?)null, (string?)m.ErrorCode)),
        ]);

    public static Exercise Choice(string correct, params (string Text, string ErrorCode)[] wrong) =>
        Build(ExerciseType.MultipleChoice, null,
        [
            (correct, true, null, null),
            .. wrong.Select(w => (w.Text, false, (string?)null, (string?)w.ErrorCode)),
        ]);

    public static Exercise Matching(params (string En, string Uz)[] pairs) =>
        Build(ExerciseType.Matching, null, [.. pairs.Select(p => (p.En, true, (string?)p.Uz, (string?)null))]);

    public static GradingContext Typed(this Exercise exercise, string text) =>
        new(Guid.NewGuid(), exercise, new SubmittedAnswer(text, null, []));

    public static GradingContext Selected(this Exercise exercise, string optionText) =>
        new(Guid.NewGuid(), exercise, new SubmittedAnswer(null, exercise.Options.Single(o => o.Text == optionText).Id, []));

    public static GradingContext Matched(this Exercise exercise, params (string En, string Uz)[] pairs) =>
        new(Guid.NewGuid(), exercise, new SubmittedAnswer(null, null,
            [.. pairs.Select(p => new SubmittedMatch(exercise.Options.Single(o => o.Text == p.En).Id, p.Uz))]));

    private static Exercise Build(
        ExerciseType type, string? pattern, List<(string Text, bool IsCorrect, string? MatchText, string? ErrorCode)> options)
    {
        var id = Guid.NewGuid();
        return new Exercise
        {
            Id = id,
            CourseVersion = 1,
            DayNumber = 1,
            Position = 1,
            Type = type,
            Instruction = "Test",
            Prompt = "Test",
            Explanation = Rule,
            AnswerPattern = pattern,
            Options =
            [
                .. options.Select((o, index) => new ExerciseOption
                {
                    ExerciseId = id,
                    Position = index + 1,
                    Text = o.Text,
                    IsCorrect = o.IsCorrect,
                    MatchText = o.MatchText,
                    ErrorCode = o.ErrorCode,
                    Feedback = o.ErrorCode is null ? null : $"{o.ErrorCode} izohi.",
                }),
            ],
        };
    }
}
