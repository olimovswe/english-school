using System.Diagnostics.CodeAnalysis;
using EnglishSchool.BuildingBlocks.Abstractions.Events;
using Microsoft.Extensions.DependencyInjection;

namespace EnglishSchool.BuildingBlocks.Outbox;

/// <summary>Bitta event tipiga barcha obunachilarni chaqiradi (har bir xabar uchun alohida DI scope'da).</summary>
internal abstract class IntegrationEventSubscription
{
    public abstract Type EventType { get; }

    public abstract Task DispatchAsync(IServiceProvider services, IIntegrationEvent integrationEvent, CancellationToken cancellationToken);
}

internal sealed class IntegrationEventSubscription<TEvent> : IntegrationEventSubscription
    where TEvent : IIntegrationEvent
{
    public override Type EventType => typeof(TEvent);

    public override async Task DispatchAsync(
        IServiceProvider services, IIntegrationEvent integrationEvent, CancellationToken cancellationToken)
    {
        foreach (var consumer in services.GetServices<IIntegrationEventConsumer<TEvent>>())
        {
            await consumer.HandleAsync((TEvent)integrationEvent, cancellationToken);
        }
    }
}

/// <summary>Outbox'dagi tip nomidan obunani topadi. Obunachisi yoʻq event shunchaki ishlov berilgan deb belgilanadi.</summary>
public sealed class IntegrationEventRegistry
{
    private readonly Dictionary<string, IntegrationEventSubscription> _subscriptions;

    internal IntegrationEventRegistry(IEnumerable<IntegrationEventSubscription> subscriptions) =>
        _subscriptions = subscriptions
            .DistinctBy(subscription => subscription.EventType)
            .ToDictionary(subscription => OutboxSerializer.TypeName(subscription.EventType), StringComparer.Ordinal);

    internal bool TryGet(string typeName, [NotNullWhen(true)] out IntegrationEventSubscription? subscription) =>
        _subscriptions.TryGetValue(typeName, out subscription);
}
