using EnglishSchool.BuildingBlocks.Abstractions.Events;

namespace EnglishSchool.Modules.Retention.Contracts.Events;

/// <summary>Kartochka unutildi (FSRS lapse). Analytics va shaxsiy mashqlar uchun signal.</summary>
public sealed record CardLapsed(Guid UserId, Guid CardId, string ItemType, Guid ItemId) : IntegrationEvent;
