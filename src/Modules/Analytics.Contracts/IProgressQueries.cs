namespace EnglishSchool.Modules.Analytics.Contracts;

public interface IProgressQueries
{
    Task<ProgressSummary> GetSummaryAsync(Guid userId, CancellationToken cancellationToken);

    Task<WeeklyPerformanceReport> GetWeeklyReportAsync(Guid userId, CancellationToken cancellationToken);
}

public sealed record ProgressSummary(
    Guid UserId,
    int CurrentDay,
    int FocusMinutesThisWeek,
    decimal ReviewAccuracy,
    int ActiveVocabulary,
    int CompletedDays,
    int TotalPracticeAttempts,
    int ActiveStreak);

public sealed record WeeklyPerformanceReport(
    Guid UserId,
    DateOnly WeekStarting,
    decimal Accuracy,
    decimal TotalFocusMinutes,
    int WordsLearned,
    int TotalPracticeAttempts);
