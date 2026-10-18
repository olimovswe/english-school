using EnglishSchool.Modules.AiGateway.Contracts;
using EnglishSchool.Modules.Practice.Domain;

namespace EnglishSchool.Modules.Practice.Grading;

/// <summary>
/// Tier 2–3: qoidalar hal qila olmagan GapFill javoblari AiGateway'ga (Haiku → Opus) yuboriladi. Gateway
/// <see cref="IAiGateway"/> faqat Contracts orqali tanish; u roʻyxatga olinmaguncha (8-hafta) tier oʻtkazib yuboriladi.
/// </summary>
internal sealed class AiGatewayTier(IEnumerable<IAiGateway> gateways) : IGradingTier
{
    private readonly IAiGateway? _gateway = gateways.FirstOrDefault();

    public int Tier => 2;

    public bool IsAvailable => _gateway is not null;

    public async ValueTask<GradingOutcome?> TryGradeAsync(GradingContext context, CancellationToken cancellationToken)
    {
        var exercise = context.Exercise;
        if (_gateway is null || exercise.Type != ExerciseType.GapFill)
        {
            return null;
        }

        var result = await _gateway.EvaluateAsync(
            new EvaluationRequest(
                context.UserId,
                exercise.Id,
                exercise.Type.ToString(),
                exercise.Prompt,
                context.Answer.Text ?? string.Empty,
                RuleSheetKey: $"day-{exercise.DayNumber}"),
            cancellationToken);

        var verdict = Enum.TryParse<Verdict>(result.Verdict, ignoreCase: true, out var parsed) ? parsed : Verdict.Incorrect;
        return new GradingOutcome(
            verdict,
            result.Score,
            result.EvaluationTier,
            [.. result.Feedback.Select(f => new GradingFeedback(f.ErrorCode, f.Correction, f.ExplanationUz))]);
    }
}
