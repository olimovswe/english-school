using EnglishSchool.Modules.Analytics.Contracts;
using EnglishSchool.Modules.Analytics.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace EnglishSchool.Modules.Analytics.Application;

internal sealed class ProgressQueries(AnalyticsDbContext db, TimeProvider clock) : IProgressQueries
{
    public async Task<ProgressSummary> GetSummaryAsync(Guid userId, CancellationToken cancellationToken)
    {
        var progress = await db.UserProgress.AsNoTracking()
            .SingleOrDefaultAsync(item => item.UserId == userId, cancellationToken);
        var weekStarting = CurrentWeekStarting();
        var focusSeconds = await db.WeeklyReports.AsNoTracking()
            .Where(item => item.UserId == userId && item.WeekStarting == weekStarting)
            .Select(item => (int?)item.TotalFocusSeconds)
            .SingleOrDefaultAsync(cancellationToken) ?? 0;

        return new ProgressSummary(
            userId,
            progress?.CurrentDay ?? 0,
            focusSeconds / 60,
            progress?.AverageAccuracy ?? 0,
            progress?.ActiveVocabulary ?? 0,
            progress?.CompletedDays ?? 0,
            progress?.TotalPracticeAttempts ?? 0,
            progress?.StreakCount ?? 0);
    }

    public async Task<WeeklyPerformanceReport> GetWeeklyReportAsync(Guid userId, CancellationToken cancellationToken)
    {
        var weekStarting = CurrentWeekStarting();
        var aggregate = await db.WeeklyReports.AsNoTracking()
            .Where(item => item.UserId == userId && item.WeekStarting == weekStarting)
            .Select(item => new
            {
                item.AccuracyScoreTotal,
                item.AttemptCount,
                item.TotalFocusSeconds,
                item.WordsLearned,
            })
            .SingleOrDefaultAsync(cancellationToken);

        return new WeeklyPerformanceReport(
            userId,
            weekStarting,
            aggregate is null || aggregate.AttemptCount == 0
                ? 0
                : aggregate.AccuracyScoreTotal / aggregate.AttemptCount,
            aggregate is null ? 0 : aggregate.TotalFocusSeconds / 60m,
            aggregate?.WordsLearned ?? 0,
            aggregate?.AttemptCount ?? 0);
    }

    private DateOnly CurrentWeekStarting()
    {
        var today = DateOnly.FromDateTime(clock.GetUtcNow().UtcDateTime);
        return today.AddDays(-(((int)today.DayOfWeek + 6) % 7));
    }
}