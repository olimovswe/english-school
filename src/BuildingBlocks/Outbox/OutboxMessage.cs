using EnglishSchool.BuildingBlocks.Abstractions.Events;

namespace EnglishSchool.BuildingBlocks.Outbox;

/// <summary>
/// Outbox jadvalidagi bitta event. Har bir modul uni oʻz schema'sida saqlaydi (<c>identity.outbox_messages</c>),
/// biznes maʼlumot bilan bitta tranzaksiyada. Id event'ning <c>EventId</c>'si: handlerlar uni idempotentlik kaliti qiladi.
/// </summary>
public sealed class OutboxMessage
{
    private const int MaxErrorLength = 2000;
    private static readonly TimeSpan MaxRetryDelay = TimeSpan.FromMinutes(5);

    private OutboxMessage(Guid id, string type, string content, DateTimeOffset occurredAt)
    {
        Id = id;
        Type = type;
        Content = content;
        OccurredAt = occurredAt;
    }

    public Guid Id { get; private set; }

    /// <summary>Event tipining toʻliq nomi (<see cref="OutboxSerializer.TypeName"/>).</summary>
    public string Type { get; private set; }

    /// <summary>JSON (jsonb).</summary>
    public string Content { get; private set; }

    public DateTimeOffset OccurredAt { get; private set; }

    public DateTimeOffset? ProcessedAt { get; private set; }

    public int FailedAttempts { get; private set; }

    /// <summary>Xatodan keyingi urinish vaqti: kechikish har safar ikki baravar oshadi (eng koʻpi 5 daqiqa).</summary>
    public DateTimeOffset? NextAttemptAt { get; private set; }

    public string? LastError { get; private set; }

    public static OutboxMessage From(IIntegrationEvent integrationEvent)
    {
        var (type, content) = OutboxSerializer.Serialize(integrationEvent);
        return new OutboxMessage(integrationEvent.EventId, type, content, integrationEvent.OccurredAt);
    }

    public void MarkProcessed(DateTimeOffset now)
    {
        ProcessedAt = now;
        NextAttemptAt = null;
    }

    public void RecordFailure(string error, DateTimeOffset now)
    {
        FailedAttempts++;
        LastError = error.Length <= MaxErrorLength ? error : error[..MaxErrorLength];

        var delay = TimeSpan.FromSeconds(Math.Pow(2, FailedAttempts));
        NextAttemptAt = now + (delay < MaxRetryDelay ? delay : MaxRetryDelay);
    }
}
