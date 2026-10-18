using EnglishSchool.BuildingBlocks.Abstractions.Events;

namespace EnglishSchool.Modules.Speech.Contracts.Events;

public sealed record TranscriptReady(Guid UserId, Guid JobId, string Purpose, string Transcript) : IntegrationEvent;
