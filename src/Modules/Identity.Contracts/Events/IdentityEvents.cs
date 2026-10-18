using EnglishSchool.BuildingBlocks.Abstractions.Events;

namespace EnglishSchool.Modules.Identity.Contracts.Events;

public sealed record UserRegistered(Guid UserId, string DisplayName, string TimeZone) : IntegrationEvent;
