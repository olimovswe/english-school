using EnglishSchool.BuildingBlocks.Abstractions.Events;
using EnglishSchool.Modules.Analytics.Domain;
using EnglishSchool.Modules.Analytics.Infrastructure;
using EnglishSchool.Modules.Curriculum.Contracts;
using EnglishSchool.Modules.Discipline.Contracts.Events;
using EnglishSchool.Modules.Practice.Contracts.Events;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace EnglishSchool.Modules.Analytics.Application;

internal sealed class AnalyticsEventUpdater(AnalyticsDbContext db, ICurriculumQueries curriculum)
{
    public Task HandleAsync(AttemptEvaluated integrationEvent, CancellationToken cancellationToken) =>
        ProcessOnceAsync(integrationEvent, async () =>
        {
            var progress = await GetProgressAsync(integrationEvent.UserId, integrationEvent.OccurredAt, cancellationToken);
            progress.RecordAttempt(integrationEvent.DayNumber, integrationEvent.Score, integrationEvent.OccurredAt);
            var report = await GetWeeklyReportAsync(integrationEvent.UserId, integrationEvent.OccurredAt, cancellationToken);
            report.RecordAttempt(integrationEvent.Score);
        }, cancellationToken);

    public Task HandleAsync(ErrorDetected integrationEvent, CancellationToken cancellationToken) =>
        ProcessOnceAsync(integrationEvent, async () =>
        {
            var error = await db.ErrorSummaries.SingleOrDefaultAsync(
                item => item.UserId == integrationEvent.UserId && item.ErrorCode == integrationEvent.ErrorCode,
                cancellationToken);
            if (error is null)
            {
                db.ErrorSummaries.Add(ErrorSummaryReadModel.Create(
                    integrationEvent.UserId,
                    integrationEvent.ErrorCode,
                    integrationEvent.Correction,
                    integrationEvent.AttemptId,
                    integrationEvent.OccurredAt));
            }
            else
            {
                error.Recur(integrationEvent.Correction, integrationEvent.AttemptId, integrationEvent.OccurredAt);
            }
        }, cancellationToken);

    public Task HandleAsync(DailyGatePassed integrationEvent, CancellationToken cancellationToken) =>
        ProcessOnceAsync(integrationEvent, async () =>
        {
            var vocabulary = await curriculum.GetVocabularyAsync(integrationEvent.DayNumber, cancellationToken);
            var progress = await GetProgressAsync(integrationEvent.UserId, integrationEvent.OccurredAt, cancellationToken);
            progress.RecordGatePassed(integrationEvent.DayNumber, vocabulary.Count, integrationEvent.OccurredAt);
            var report = await GetWeeklyReportAsync(integrationEvent.UserId, integrationEvent.OccurredAt, cancellationToken);
            report.AddWordsLearned(vocabulary.Count);
        }, cancellationToken);

    public Task HandleAsync(SessionEnded integrationEvent, CancellationToken cancellationToken) =>
        ProcessOnceAsync(integrationEvent, async () =>
        {
            var progress = await GetProgressAsync(integrationEvent.UserId, integrationEvent.OccurredAt, cancellationToken);
            progress.RecordSessionEnded(integrationEvent.OccurredAt, integrationEvent.MinimalDay);
            var report = await GetWeeklyReportAsync(integrationEvent.UserId, integrationEvent.OccurredAt, cancellationToken);
            report.AddFocusSeconds(integrationEvent.FocusSeconds);
        }, cancellationToken);

    private async Task<UserProgressReadModel> GetProgressAsync(
        Guid userId, DateTimeOffset occurredAt, CancellationToken cancellationToken)
    {
        var progress = await db.UserProgress.SingleOrDefaultAsync(item => item.UserId == userId, cancellationToken);
        if (progress is not null)
        {
            return progress;
        }

        progress = UserProgressReadModel.Create(userId, occurredAt);
        db.UserProgress.Add(progress);
        return progress;
    }

    private async Task<WeeklyReportReadModel> GetWeeklyReportAsync(
        Guid userId, DateTimeOffset occurredAt, CancellationToken cancellationToken)
    {
        var eventDay = DateOnly.FromDateTime(occurredAt.UtcDateTime);
        var weekStarting = eventDay.AddDays(-(((int)eventDay.DayOfWeek + 6) % 7));
        var report = await db.WeeklyReports.SingleOrDefaultAsync(
            item => item.UserId == userId && item.WeekStarting == weekStarting,
            cancellationToken);
        if (report is not null)
        {
            return report;
        }

        report = WeeklyReportReadModel.Create(userId, weekStarting);
        db.WeeklyReports.Add(report);
        return report;
    }

    private async Task ProcessOnceAsync(
        IIntegrationEvent integrationEvent,
        Func<Task> update,
        CancellationToken cancellationToken)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        if (await db.ProcessedEvents.AnyAsync(item => item.EventId == integrationEvent.EventId, cancellationToken))
        {
            await transaction.RollbackAsync(cancellationToken);
            return;
        }

        db.ProcessedEvents.Add(ProcessedAnalyticsEvent.Create(
            integrationEvent.EventId,
            integrationEvent.GetType().FullName ?? integrationEvent.GetType().Name,
            integrationEvent.OccurredAt));
        await update();

        try
        {
            await db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (IsDuplicateEvent(exception))
        {
            await transaction.RollbackAsync(cancellationToken);
            db.ChangeTracker.Clear();
        }
    }

    private static bool IsDuplicateEvent(DbUpdateException exception) =>
        exception.InnerException is PostgresException
        {
            SqlState: PostgresErrorCodes.UniqueViolation,
            ConstraintName: "pk_processed_events",
        };
}