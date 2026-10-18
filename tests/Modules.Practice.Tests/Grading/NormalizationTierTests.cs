using EnglishSchool.Modules.Practice.Domain;
using EnglishSchool.Modules.Practice.Grading;
using EnglishSchool.Modules.Practice.Tests.TestSupport;

namespace EnglishSchool.Modules.Practice.Tests.Grading;

public sealed class NormalizationTierTests
{
    private readonly NormalizationTier _tier = new();

    [Theory]
    [InlineData("Hello", "hello")]
    [InlineData("Hello", "  HELLO!  ")]
    [InlineData("isn't", "is not")]
    [InlineData("is not", "isn't")]
    [InlineData("isn't", "isn’t")]
    [InlineData("isn't", "isnʼt")]
    [InlineData("I'm a student", "i am   a student.")]
    [InlineData("cannot", "can not")]
    public async Task Variants_of_an_accepted_answer_are_correct(string accepted, string answer)
    {
        var outcome = await _tier.TryGradeAsync(TestExercises.GapFill([accepted]).Typed(answer), CancellationToken.None);

        Assert.Equal(GradingOutcome.Correct(1), outcome);
    }

    [Fact]
    public async Task Variants_of_a_known_mistake_keep_its_error_code()
    {
        var exercise = TestExercises.GapFill(["am"], [("is", ErrorCodes.BeForm)]);

        var outcome = await _tier.TryGradeAsync(exercise.Typed("IS."), CancellationToken.None);

        Assert.Equal(ErrorCodes.BeForm, Assert.Single(outcome!.Feedback).ErrorCode);
        Assert.Equal(1, outcome.Tier);
    }

    [Theory]
    [InlineData("children", "childern")]
    [InlineData("children", "chidren")]
    [InlineData("teacher", "teachr")]
    [InlineData("isn't", "isnt")]
    [InlineData("headphones", "hedphons")]
    public async Task Misspelling_is_partially_correct(string accepted, string answer)
    {
        var outcome = await _tier.TryGradeAsync(TestExercises.GapFill([accepted]).Typed(answer), CancellationToken.None);

        Assert.NotNull(outcome);
        Assert.Equal((Verdict.PartiallyCorrect, 0.5m), (outcome.Verdict, outcome.Score));
        Assert.Equal(new GradingFeedback(ErrorCodes.Spelling, accepted, $"Imlo xatosi: toʻgʻri yozilishi — {accepted}."), Assert.Single(outcome.Feedback));
    }

    [Theory]
    [InlineData("these", "those")]
    [InlineData("this", "thus")]
    [InlineData("am", "an")]
    [InlineData("children", "kids")]
    public async Task Grammar_word_swaps_short_words_and_other_words_are_not_typos(string accepted, string answer)
    {
        Assert.Null(await _tier.TryGradeAsync(TestExercises.GapFill([accepted]).Typed(answer), CancellationToken.None));
    }

    [Theory]
    [InlineData("children", "childern", 1)]
    [InlineData("children", "chidren", 1)]
    [InlineData("kitten", "sitting", 3)]
    [InlineData("same", "same", 0)]
    public void Edit_distance_counts_adjacent_swaps_as_one_change(string a, string b, int expected) =>
        Assert.Equal(expected, AnswerText.EditDistance(a, b));
}
