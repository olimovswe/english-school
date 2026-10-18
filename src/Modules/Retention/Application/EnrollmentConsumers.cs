using EnglishSchool.BuildingBlocks.Abstractions.Events;
using EnglishSchool.Modules.Discipline.Contracts.Events;
using EnglishSchool.Modules.Practice.Contracts.Events;

namespace EnglishSchool.Modules.Retention.Application;

/// <summary>Oʻquvchi kun mashqini bajardi: shu kunning yangi soʻz va iboralari kartochkaga aylanadi.</summary>
internal sealed class EnrollOnPracticeConsumer(FlashcardEnrollment enrollment) : IIntegrationEventConsumer<AttemptEvaluated>
{
    public Task HandleAsync(AttemptEvaluated integrationEvent, CancellationToken cancellationToken) =>
        enrollment.EnrollDayAsync(integrationEvent.UserId, integrationEvent.DayNumber, cancellationToken);
}

/// <summary>Kun ochildi (ertalabki darvoza topshirildi, Discipline): shu kun lugʻati kartochkaga aylanadi.</summary>
internal sealed class EnrollOnDayUnlockedConsumer(FlashcardEnrollment enrollment) : IIntegrationEventConsumer<DailyGatePassed>
{
    public Task HandleAsync(DailyGatePassed integrationEvent, CancellationToken cancellationToken) =>
        enrollment.EnrollDayAsync(integrationEvent.UserId, integrationEvent.DayNumber, cancellationToken);
}
