using EnglishSchool.BuildingBlocks.Abstractions.Events;

namespace EnglishSchool.Modules.Discipline.Contracts.Events;

/// <summary>Ertalabki takrorlash testi (≥ 80%) topshirildi, kunning bloklari ochiladi.</summary>
public sealed record DailyGatePassed(Guid UserId, int DayNumber, decimal Score) : IntegrationEvent;

public sealed record BlockCompleted(Guid UserId, Guid SessionId, string BlockType, int FocusSeconds) : IntegrationEvent;

public sealed record SessionEnded(Guid UserId, Guid SessionId, int FocusSeconds, bool MinimalDay) : IntegrationEvent;
