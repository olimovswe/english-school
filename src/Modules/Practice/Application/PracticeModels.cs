namespace EnglishSchool.Modules.Practice.Application;

/// <summary>
/// Oʻquvchiga koʻrsatiladigan mashq: toʻgʻri javob va xato kodlari yashirilgan.
/// MultipleChoice'da <see cref="Choices"/> — tanlovlar; Matching'da — chap ustun, <see cref="MatchTargets"/> — oʻng ustun.
/// </summary>
internal sealed record ExerciseDto(
    Guid Id,
    int Position,
    string Type,
    string Instruction,
    string Prompt,
    IReadOnlyList<ExerciseChoiceDto> Choices,
    IReadOnlyList<string> MatchTargets);

internal sealed record ExerciseChoiceDto(Guid Id, string Text);

/// <param name="Answer">GapFill uchun.</param>
/// <param name="SelectedOptionId">MultipleChoice uchun.</param>
/// <param name="Matches">Matching uchun: har bir chap element uchun bitta juftlik.</param>
internal sealed record SubmitAttemptRequest(
    Guid? ExerciseId,
    string? Answer,
    Guid? SelectedOptionId,
    IReadOnlyList<MatchAnswer>? Matches,
    int? TimeTakenMs);

internal sealed record MatchAnswer(Guid OptionId, string? MatchText);

internal sealed record AttemptResultResponse(
    Guid AttemptId,
    string Verdict,
    decimal Score,
    int EvaluationTier,
    IReadOnlyList<FeedbackDto> Feedback);

internal sealed record FeedbackDto(string? ErrorCode, string Correction, string? Explanation);

internal enum SubmitStatus
{
    Evaluated,
    ExerciseNotFound,
    Invalid,
}

internal sealed record SubmitAttemptResult(
    SubmitStatus Status,
    AttemptResultResponse? Result = null,
    Dictionary<string, string[]>? Errors = null);
