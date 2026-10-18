using EnglishSchool.BuildingBlocks.Abstractions.Events;
using EnglishSchool.Modules.Discipline.Contracts.Events;
using EnglishSchool.Modules.Practice.Contracts.Events;

namespace EnglishSchool.Modules.Analytics.Application;

internal sealed class AttemptEvaluatedConsumer(AnalyticsEventUpdater updater) : IIntegrationEventConsumer<AttemptEvaluated>
{
    public Task HandleAsync(AttemptEvaluated integrationEvent, CancellationToken cancellationToken) =>
        updater.HandleAsync(integrationEvent, cancellationToken);
}

internal sealed class ErrorDetectedConsumer(AnalyticsEventUpdater updater) : IIntegrationEventConsumer<ErrorDetected>
{
    public Task HandleAsync(ErrorDetected integrationEvent, CancellationToken cancellationToken) =>
        updater.HandleAsync(integrationEvent, cancellationToken);
}

internal sealed class DailyGatePassedConsumer(AnalyticsEventUpdater updater) : IIntegrationEventConsumer<DailyGatePassed>
{
    public Task HandleAsync(DailyGatePassed integrationEvent, CancellationToken cancellationToken) =>
        updater.HandleAsync(integrationEvent, cancellationToken);
}

internal sealed class SessionEndedConsumer(AnalyticsEventUpdater updater) : IIntegrationEventConsumer<SessionEnded>
{
    public Task HandleAsync(SessionEnded integrationEvent, CancellationToken cancellationToken) =>
        updater.HandleAsync(integrationEvent, cancellationToken);
}