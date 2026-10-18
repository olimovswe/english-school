using EnglishSchool.BuildingBlocks.Abstractions.Events;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace EnglishSchool.BuildingBlocks.Outbox;

public sealed class OutboxOptions
{
    public const string Section = "Outbox";

    public TimeSpan PollingInterval { get; set; } = TimeSpan.FromSeconds(2);

    public int BatchSize { get; set; } = 50;

    /// <summary>Shuncha muvaffaqiyatsiz urinishdan keyin xabar toʻxtatiladi (LastError bilan jadvalda qoladi).</summary>
    public int MaxAttempts { get; set; } = 10;
}

public static class OutboxServiceCollectionExtensions
{
    /// <summary>Modul chaqiradi (ikkala host'da ham): shu DbContext'ning outbox jadvali dispatcher'ga koʻrinadi.</summary>
    public static IServiceCollection AddOutboxStore<TContext>(this IServiceCollection services)
        where TContext : DbContext
    {
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IOutboxStore, OutboxStore<TContext>>());
        return services;
    }

    /// <summary>Isteʼmolchi modul chaqiradi: event Worker'da shu consumer'ga yetkaziladi.</summary>
    public static IServiceCollection AddIntegrationEventConsumer<TEvent, TConsumer>(this IServiceCollection services)
        where TEvent : IIntegrationEvent
        where TConsumer : class, IIntegrationEventConsumer<TEvent>
    {
        services.TryAddEnumerable(ServiceDescriptor.Scoped<IIntegrationEventConsumer<TEvent>, TConsumer>());
        services.AddSingleton<IntegrationEventSubscription>(new IntegrationEventSubscription<TEvent>());
        return services;
    }

    /// <summary>Faqat Worker host chaqiradi: outbox'larni soʻrab, eventlarni yetkazadigan fon xizmati.</summary>
    public static IServiceCollection AddOutboxDispatcher(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<OutboxOptions>()
            .Bind(configuration.GetSection(OutboxOptions.Section))
            .Validate(o => o.PollingInterval > TimeSpan.Zero && o.BatchSize is > 0 and <= 1000 && o.MaxAttempts > 0,
                "Outbox: PollingInterval musbat, BatchSize 1–1000, MaxAttempts musbat boʻlishi kerak.")
            .ValidateOnStart();

        services.TryAddSingleton(TimeProvider.System);
        services.TryAddSingleton(sp => new IntegrationEventRegistry(sp.GetServices<IntegrationEventSubscription>()));
        services.TryAddSingleton<OutboxProcessor>();
        services.AddHostedService<OutboxDispatcher>();
        return services;
    }
}
