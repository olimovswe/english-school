namespace EnglishSchool.Modules.AiGateway.Contracts;

/// <summary>
/// Barcha LLM chaqiruvlari shu kontrakt orqali oʻtadi. Tier tanlash, kesh, budjet va
/// xarajat daftari gateway ichida yashiringan; chaqiruvchi modul model nomini bilmaydi.
/// </summary>
public interface IAiGateway
{
    Task<EvaluationResult> EvaluateAsync(EvaluationRequest request, CancellationToken cancellationToken);
}

public sealed record EvaluationRequest(
    Guid UserId,
    Guid ExerciseId,
    string ExerciseType,
    string Prompt,
    string Answer,
    string RuleSheetKey);

public sealed record EvaluationResult(
    string Verdict,
    decimal Score,
    int EvaluationTier,
    IReadOnlyList<EvaluationFeedback> Feedback);

public sealed record EvaluationFeedback(string ErrorCode, string Span, string Correction, string ExplanationUz);
