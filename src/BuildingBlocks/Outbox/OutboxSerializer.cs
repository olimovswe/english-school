using System.Text.Json;
using EnglishSchool.BuildingBlocks.Abstractions.Events;

namespace EnglishSchool.BuildingBlocks.Outbox;

/// <summary>
/// Event va outbox qatori oʻrtasidagi format. Tip nomi saqlangan: event tipi yoki namespace'i
/// oʻzgartirilsa, ishlov berilmagan eski qatorlar yangi nomni topa olmaydi.
/// </summary>
public static class OutboxSerializer
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web);

    public static string TypeName(Type eventType) =>
        eventType.FullName ?? throw new ArgumentException($"{eventType} tipining toʻliq nomi yoʻq.", nameof(eventType));

    public static (string Type, string Content) Serialize(IIntegrationEvent integrationEvent)
    {
        ArgumentNullException.ThrowIfNull(integrationEvent);

        var eventType = integrationEvent.GetType();
        return (TypeName(eventType), JsonSerializer.Serialize(integrationEvent, eventType, Options));
    }

    public static IIntegrationEvent Deserialize(string content, Type eventType) =>
        JsonSerializer.Deserialize(content, eventType, Options) as IIntegrationEvent
        ?? throw new JsonException($"Outbox qatori {eventType.Name} eventiga aylanmadi.");
}
