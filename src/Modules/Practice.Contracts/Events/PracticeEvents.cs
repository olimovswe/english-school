using EnglishSchool.BuildingBlocks.Abstractions.Events;

namespace EnglishSchool.Modules.Practice.Contracts.Events;

/// <summary>
/// Javob baholandi. Tier: 0 qoidalar · 1 kesh/LanguageTool · 2 Haiku · 3 Opus.
/// <see cref="DayNumber"/> — mashq qaysi kurs kuniga tegishli (masalan, Retention shu kun soʻzlaridan kartochka ochadi).
/// </summary>
public sealed record AttemptEvaluated(
    Guid UserId,
    Guid AttemptId,
    Guid ExerciseId,
    int DayNumber,
    string Verdict,
    decimal Score,
    int EvaluationTier) : IntegrationEvent;

/// <summary>Taksonomiya boʻyicha xato (masalan 3SG_S, ART_MISSING) aniqlandi.</summary>
public sealed record ErrorDetected(Guid UserId, Guid AttemptId, string ErrorCode, string Correction) : IntegrationEvent;
