namespace EnglishSchool.BuildingBlocks.Abstractions.Events;

/// <summary>
/// Boshqa modul eventiga obuna. Worker outbox'dan oʻqib chaqiradi; yetkazish kamida bir marta
/// (at-least-once) kafolatlanadi, shuning uchun consumer idempotent boʻlishi shart.
/// </summary>
public interface IIntegrationEventConsumer<in TEvent>
    where TEvent : IIntegrationEvent
{
    Task HandleAsync(TEvent integrationEvent, CancellationToken cancellationToken);
}
