namespace EnglishSchool.BuildingBlocks.Abstractions.Events;

/// <summary>
/// Integration event chiqaradigan entity. Eventlar entity ichida yigʻiladi; SaveChanges paytida outbox
/// interceptori ularni biznes maʼlumot bilan bitta tranzaksiyada modulning outbox jadvaliga yozadi.
/// </summary>
public interface IIntegrationEventSource
{
    IReadOnlyCollection<IIntegrationEvent> IntegrationEvents { get; }

    void ClearIntegrationEvents();
}

/// <summary>
/// <see cref="IIntegrationEventSource"/> uchun asosiy klass. Aʼzolar oshkor (explicit) qilingan,
/// shuning uchun EF Core ularni ustun yoki navigatsiya deb hisoblamaydi.
/// </summary>
public abstract class IntegrationEventSource : IIntegrationEventSource
{
    private readonly List<IIntegrationEvent> _integrationEvents = [];

    IReadOnlyCollection<IIntegrationEvent> IIntegrationEventSource.IntegrationEvents => _integrationEvents;

    void IIntegrationEventSource.ClearIntegrationEvents() => _integrationEvents.Clear();

    protected void Raise(IIntegrationEvent integrationEvent) => _integrationEvents.Add(integrationEvent);
}
