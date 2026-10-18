using EnglishSchool.Modules.AiGateway.Contracts;
using EnglishSchool.Modules.Practice.Domain;
using EnglishSchool.Modules.Practice.Grading;
using EnglishSchool.Modules.Practice.Tests.TestSupport;

namespace EnglishSchool.Modules.Practice.Tests.Grading;

public sealed class GradingPipelineTests
{
    private static readonly Exercise Gap = TestExercises.GapFill(["am"], [("is", ErrorCodes.BeForm)]);

    [Theory]
    [InlineData("am", 0)]
    [InlineData("AM", 1)]
    public async Task The_cheapest_tier_that_can_decide_wins(string answer, int tier)
    {
        var outcome = await Pipeline().GradeAsync(Gap.Typed(answer), CancellationToken.None);

        Assert.Equal(GradingOutcome.Correct(tier), outcome);
    }

    [Fact]
    public async Task Without_AiGateway_an_unrecognised_answer_is_incorrect_at_tier_1_with_the_rule()
    {
        var outcome = await Pipeline().GradeAsync(Gap.Typed("was"), CancellationToken.None);

        Assert.Equal((Verdict.Incorrect, 0m, 1), (outcome.Verdict, outcome.Score, outcome.Tier));
        Assert.Equal(new GradingFeedback(null, "am", TestExercises.Rule), Assert.Single(outcome.Feedback));
    }

    [Fact]
    public async Task AiGateway_judges_only_what_the_rules_could_not()
    {
        var gateway = new FakeGateway(new EvaluationResult("PartiallyCorrect", 0.5m, 3, [new("TENSE", "was", "am", "Hozirgi zamon kerak.")]));
        var pipeline = Pipeline(gateway);

        var byRules = await pipeline.GradeAsync(Gap.Typed("is"), CancellationToken.None);
        var byAi = await pipeline.GradeAsync(Gap.Typed("was"), CancellationToken.None);

        Assert.Equal(0, byRules.Tier);
        Assert.Equal((Verdict.PartiallyCorrect, 0.5m, 3), (byAi.Verdict, byAi.Score, byAi.Tier));
        Assert.Equal(new GradingFeedback("TENSE", "am", "Hozirgi zamon kerak."), Assert.Single(byAi.Feedback));
        var request = Assert.Single(gateway.Requests);
        Assert.Equal(("was", "day-1", "GapFill"), (request.Answer, request.RuleSheetKey, request.ExerciseType));
    }

    private static GradingPipeline Pipeline(params IAiGateway[] gateways) =>
        new([new AiGatewayTier(gateways), new NormalizationTier(), new RuleBasedTier()]);

    private sealed class FakeGateway(EvaluationResult result) : IAiGateway
    {
        public List<EvaluationRequest> Requests { get; } = [];

        public Task<EvaluationResult> EvaluateAsync(EvaluationRequest request, CancellationToken cancellationToken)
        {
            Requests.Add(request);
            return Task.FromResult(result);
        }
    }
}
