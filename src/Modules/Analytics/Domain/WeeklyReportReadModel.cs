namespace EnglishSchool.Modules.Analytics.Domain;

internal sealed class WeeklyReportReadModel
{
    private WeeklyReportReadModel()
    {
    }

    public Guid UserId { get; private init; }

    public DateOnly WeekStarting { get; private init; }

    public decimal AccuracyScoreTotal { get; private set; }

    public int AttemptCount { get; private set; }

    public int TotalFocusSeconds { get; private set; }

    public int WordsLearned { get; private set; }

    public decimal Accuracy => AttemptCount == 0 ? 0 : AccuracyScoreTotal / AttemptCount;

    public decimal TotalFocusMinutes => TotalFocusSeconds / 60m;

    public static WeeklyReportReadModel Create(Guid userId, DateOnly weekStarting) => new()
    {
        UserId = userId,
        WeekStarting = weekStarting,
    };
}