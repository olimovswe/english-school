using EnglishSchool.Modules.Practice.Domain;
using EnglishSchool.Modules.Practice.Grading;
using EnglishSchool.Modules.Practice.Tests.TestSupport;

namespace EnglishSchool.Modules.Practice.Tests.Grading;

public sealed class RuleBasedTierTests
{
    private readonly RuleBasedTier _tier = new();

    [Theory]
    [InlineData("am")]
    [InlineData("  am ")]
    public async Task Exact_accepted_answer_is_correct(string answer)
    {
        var outcome = await _tier.TryGradeAsync(TestExercises.GapFill(["am"]).Typed(answer), CancellationToken.None);

        Assert.Equal(GradingOutcome.Correct(0), outcome);
    }

    [Theory]
    [InlineData("colour", true)]
    [InlineData("color", true)]
    [InlineData("colours", false)]
    public async Task Answer_pattern_must_match_the_whole_answer(string answer, bool correct)
    {
        var exercise = TestExercises.GapFill(["colour"], pattern: "colou?r");

        var outcome = await _tier.TryGradeAsync(exercise.Typed(answer), CancellationToken.None);

        Assert.Equal(correct, outcome?.Verdict == Verdict.Correct);
    }

    [Fact]
    public async Task Known_mistake_reports_its_error_code_feedback_and_the_correct_answer()
    {
        var exercise = TestExercises.GapFill(["am"], [("is", ErrorCodes.BeForm)]);

        var outcome = await _tier.TryGradeAsync(exercise.Typed("is"), CancellationToken.None);

        Assert.NotNull(outcome);
        Assert.Equal((Verdict.Incorrect, 0m, 0), (outcome.Verdict, outcome.Score, outcome.Tier));
        Assert.Equal(new GradingFeedback(ErrorCodes.BeForm, "am", "BE_FORM izohi."), Assert.Single(outcome.Feedback));
    }

    [Theory]
    [InlineData("Am")]
    [InlineData("I am")]
    [InlineData("xyz")]
    public async Task Anything_else_is_left_to_the_next_tier(string answer)
    {
        var exercise = TestExercises.GapFill(["am"], [("is", ErrorCodes.BeForm)]);

        Assert.Null(await _tier.TryGradeAsync(exercise.Typed(answer), CancellationToken.None));
    }

    [Fact]
    public async Task Multiple_choice_is_always_decided()
    {
        var exercise = TestExercises.Choice("I'm a developer.", ("I'm developer.", ErrorCodes.ArticleMissing));

        var right = await _tier.TryGradeAsync(exercise.Selected("I'm a developer."), CancellationToken.None);
        var wrong = await _tier.TryGradeAsync(exercise.Selected("I'm developer."), CancellationToken.None);

        Assert.Equal(Verdict.Correct, right?.Verdict);
        Assert.Equal(Verdict.Incorrect, wrong?.Verdict);
        Assert.Equal(ErrorCodes.ArticleMissing, Assert.Single(wrong!.Feedback).ErrorCode);
    }

    [Fact]
    public async Task Matching_scores_each_pair_and_names_the_wrong_ones()
    {
        var exercise = TestExercises.Matching(("happy", "xursand"), ("sad", "xafa"), ("busy", "band"));

        var all = await _tier.TryGradeAsync(exercise.Matched(("happy", "XURSAND "), ("sad", "xafa"), ("busy", "band")), CancellationToken.None);
        var some = await _tier.TryGradeAsync(exercise.Matched(("happy", "xursand"), ("sad", "band"), ("busy", "xafa")), CancellationToken.None);
        var none = await _tier.TryGradeAsync(exercise.Matched(("happy", "xafa"), ("sad", "band"), ("busy", "xursand")), CancellationToken.None);

        Assert.Equal((Verdict.Correct, 1m), (all!.Verdict, all.Score));
        Assert.Equal((Verdict.PartiallyCorrect, 0.33m), (some!.Verdict, some.Score));
        Assert.Equal(["sad — xafa", "busy — band"], some.Feedback.Select(f => f.Correction));
        Assert.All(some.Feedback, f => Assert.Equal(ErrorCodes.Vocabulary, f.ErrorCode));
        Assert.Equal((Verdict.Incorrect, 0m), (none!.Verdict, none.Score));
    }
}
