using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EnglishSchool.BuildingBlocks.Outbox;

/// <summary>
/// Barcha modullarning outbox jadvallaridan bitta partiya oʻqib, eventlarni obunachilarga yetkazadi.
/// Bitta modul jadvalidagi muammo (masalan, migratsiya hali qoʻllanmagan) boshqalarini toʻxtatmaydi.
/// </summary>
public sealed partial class OutboxProcessor(
    IEnumerable<IOutboxStore> stores,
    IntegrationEventRegistry registry,
    IServiceScopeFactory scopeFactory,
    IOptions<OutboxOptions> options,
    TimeProvider clock,
    ILogger<OutboxProcessor> logger)
{
    /// <returns>Muvaffaqiyatli yetkazilgan xabarlar soni.</returns>
    public async Task<int> ProcessAsync(CancellationToken cancellationToken)
    {
        var settings = options.Value;
        var delivered = 0;

        foreach (var store in stores)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                // Toʻxtash soʻraldi: yangi jadval partiyasi boshlanmaydi.
                break;
            }

            try
            {
                delivered += await store.ProcessBatchAsync(
                    DispatchAsync, settings.BatchSize, settings.MaxAttempts, clock, cancellationToken);
            }
            catch (Exception exception) when (!cancellationToken.IsCancellationRequested)
            {
                // Bitta jadvaldagi xato (yoki begona OperationCanceledException) dispatcher'ni toʻxtatmaydi.
                LogStoreFailed(logger, store.Name, exception);
            }
        }

        return delivered;
    }

    private async Task DispatchAsync(OutboxMessage message, CancellationToken cancellationToken)
    {
        if (!registry.TryGet(message.Type, out var subscription))
        {
            return;
        }

        try
        {
            var integrationEvent = OutboxSerializer.Deserialize(message.Content, subscription.EventType);
            await using var scope = scopeFactory.CreateAsyncScope();
            await subscription.DispatchAsync(scope.ServiceProvider, integrationEvent, cancellationToken);
        }
        catch (Exception exception) when (!cancellationToken.IsCancellationRequested)
        {
            if (message.FailedAttempts + 1 >= options.Value.MaxAttempts)
            {
                LogParked(logger, message.Id, message.Type, exception);
            }
            else
            {
                LogWillRetry(logger, message.Id, message.Type, exception);
            }

            throw;
        }
    }

    [LoggerMessage(Level = LogLevel.Error, Message = "Outbox {Store} jadvalini oʻqib boʻlmadi.")]
    private static partial void LogStoreFailed(ILogger logger, string store, Exception exception);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Event {MessageId} ({Type}) yetkazilmadi, keyinroq qayta uriniladi.")]
    private static partial void LogWillRetry(ILogger logger, Guid messageId, string type, Exception exception);

    [LoggerMessage(Level = LogLevel.Error, Message = "Event {MessageId} ({Type}) urinishlar chegarasiga yetdi va toʻxtatildi: qoʻlda koʻrib chiqing.")]
    private static partial void LogParked(ILogger logger, Guid messageId, string type, Exception exception);
}
