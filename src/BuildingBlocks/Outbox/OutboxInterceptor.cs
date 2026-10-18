using EnglishSchool.BuildingBlocks.Abstractions.Events;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace EnglishSchool.BuildingBlocks.Outbox;

/// <summary>
/// SaveChanges paytida kuzatilayotgan entitylardagi integration eventlarni <see cref="OutboxMessage"/>
/// qatorlariga aylantiradi. Ular biznes oʻzgarishlar bilan bitta tranzaksiyada yoziladi: yo hammasi, yo hech biri.
/// </summary>
public sealed class OutboxInterceptor : SaveChangesInterceptor
{
    public static readonly OutboxInterceptor Instance = new();

    private OutboxInterceptor()
    {
    }

    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        ArgumentNullException.ThrowIfNull(eventData);
        WriteOutboxMessages(eventData.Context);
        return result;
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(eventData);
        WriteOutboxMessages(eventData.Context);
        return ValueTask.FromResult(result);
    }

    private static void WriteOutboxMessages(DbContext? context)
    {
        if (context is null)
        {
            return;
        }

        var sources = context.ChangeTracker.Entries<IIntegrationEventSource>().Select(entry => entry.Entity).ToList();
        var messages = sources.SelectMany(source => source.IntegrationEvents).Select(OutboxMessage.From).ToList();

        // Eventlar faqat bir marta aylantiriladi: saqlash xato bilan tugasa ham, qayta SaveChanges ularni ikkilamaydi.
        foreach (var source in sources)
        {
            source.ClearIntegrationEvents();
        }

        if (messages.Count == 0)
        {
            return;
        }

        if (context.Model.FindEntityType(typeof(OutboxMessage)) is null)
        {
            throw new InvalidOperationException(
                $"{context.GetType().Name} event chiqaradi, lekin outbox jadvali yoʻq: OnModelCreating'da AddOutboxMessages() chaqiring.");
        }

        context.Set<OutboxMessage>().AddRange(messages);
    }
}
