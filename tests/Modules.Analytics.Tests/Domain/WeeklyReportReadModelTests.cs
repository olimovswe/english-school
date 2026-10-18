using EnglishSchool.Modules.Analytics.Domain;

namespace EnglishSchool.Modules.Analytics.Tests.Domain;

public sealed class WeeklyReportReadModelTests
{
    [Fact]
    public void Weekly_metrics_aggregate_accuracy_focus_and_words()
    {
        var report = WeeklyReportReadModel.Create(Guid.NewGuid(), new DateOnly(2026, 10, 12));

        report.RecordAttempt(0.8m);
        report.RecordAttempt(0.6m);
        report.AddFocusSeconds(2_700);
        report.AddWordsLearned(18);

        Assert.Equal(0.7m, report.Accuracy);
        Assert.Equal(2, report.AttemptCount);
        Assert.Equal(45m, report.TotalFocusMinutes);
        Assert.Equal(18, report.WordsLearned);
    }

    [Fact]
    public void Empty_week_reports_zero_accuracy_and_focus()
    {
        var report = WeeklyReportReadModel.Create(Guid.NewGuid(), new DateOnly(2026, 10, 12));

        Assert.Equal(0m, report.Accuracy);
        Assert.Equal(0m, report.TotalFocusMinutes);
        Assert.Equal(0, report.WordsLearned);
    }
}