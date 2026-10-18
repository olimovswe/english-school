namespace EnglishSchool.Modules.Analytics.Domain;

internal sealed class ProcessedAnalyticsEvent
{
    private ProcessedAnalyticsEvent()
    {
    }

    public Guid EventId { get; private init; }

    public string EventType { get; private init; } = string.Empty;

    public DateTimeOffset OccurredAt { get; private init; }

    public static ProcessedAnalyticsEvent Create(Guid eventId, string eventType, DateTimeOffset occurredAt) => new()
    {
        EventId = eventId,
        EventType = eventType,
        OccurredAt = occurredAt,
    };
}