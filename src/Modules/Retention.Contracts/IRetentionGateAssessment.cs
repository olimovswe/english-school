namespace EnglishSchool.Modules.Retention.Contracts;

public interface IRetentionGateAssessment
{
    Task<RetentionGateAssessmentResult> AssessAsync(
        Guid userId,
        IReadOnlyList<RetentionGateAnswer> answers,
        CancellationToken cancellationToken);
}

public sealed record RetentionGateAnswer(Guid CardId, string Answer);

public sealed record RetentionGateAssessmentResult(
    bool IsValid,
    decimal Score,
    int CorrectAnswers,
    int TotalCards);