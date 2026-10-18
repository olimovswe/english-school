using EnglishSchool.BuildingBlocks.Abstractions.Events;

namespace EnglishSchool.Modules.Practice.Contracts.Events;

/// <summary>Javob baholandi. Tier: 0 qoidalar · 1 kesh/LanguageTool · 2 Haiku · 3 Opus.</summary>
public sealed record AttemptEvaluated(
    Guid UserId,
    Guid AttemptId,
    Guid ExerciseId,
    string Verdict,
    decimal Score,
    int EvaluationTier) : IntegrationEvent;

/// <summary>Taksonomiya boʻyicha xato (masalan 3SG_S, ART_MISSING) aniqlandi.</summary>
public sealed record ErrorDetected(Guid UserId, Guid AttemptId, string ErrorCode, string Correction) : IntegrationEvent;
