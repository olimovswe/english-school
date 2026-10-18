using EnglishSchool.BuildingBlocks.Abstractions.Events;
using EnglishSchool.Modules.Discipline.Contracts.Events;
using EnglishSchool.Modules.Practice.Contracts.Events;

namespace EnglishSchool.Modules.Speech.Application;

/// <summary>Kun ochildi (Discipline): uning soʻz va iboralari uchun namuna talaffuz kliplari tayyorlanadi.</summary>
internal sealed class SynthesizeOnDayUnlockedConsumer(SpeechClipService clips) : IIntegrationEventConsumer<DailyGatePassed>
{
    public Task HandleAsync(DailyGatePassed integrationEvent, CancellationToken cancellationToken) =>
        clips.SynthesizeDayAsync(integrationEvent.DayNumber, cancellationToken);
}

/// <summary>
/// Oʻquvchi kun mashqini bajardi (Practice). Kliplar umumiy, shuning uchun bu odatda bitta bazaviy soʻrov;
/// TTS kaliti keyinroq sozlansa, yetishmagan kliplar shu yoʻl bilan toʻldiriladi.
/// </summary>
internal sealed class SynthesizeOnPracticeConsumer(SpeechClipService clips) : IIntegrationEventConsumer<AttemptEvaluated>
{
    public Task HandleAsync(AttemptEvaluated integrationEvent, CancellationToken cancellationToken) =>
        clips.SynthesizeDayAsync(integrationEvent.DayNumber, cancellationToken);
}
