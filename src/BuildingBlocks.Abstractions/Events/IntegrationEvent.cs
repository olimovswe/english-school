namespace EnglishSchool.BuildingBlocks.Abstractions.Events;

/// <summary>
/// Modullar oʻrtasida yuboriladigan event. Event biznes maʼlumot bilan bitta
/// tranzaksiyada outbox jadvaliga yoziladi, keyin Worker uni tarqatadi.
/// </summary>
public interface IIntegrationEvent
{
    Guid EventId { get; }

    DateTimeOffset OccurredAt { get; }
}

public abstract record IntegrationEvent : IIntegrationEvent
{
    public Guid EventId { get; init; } = Guid.CreateVersion7();

    public DateTimeOffset OccurredAt { get; init; } = DateTimeOffset.UtcNow;
}
