using EnglishSchool.Modules.Analytics.Domain;

namespace EnglishSchool.Modules.Analytics.Tests.Domain;

public sealed class UserProgressReadModelTests
{
    [Fact]
    public void Attempts_update_current_day_count_accuracy_and_timestamp()
    {
        var userId = Guid.NewGuid();
        var occurredAt = new DateTimeOffset(2026, 10, 18, 9, 0, 0, TimeSpan.Zero);
        var progress = UserProgressReadModel.Create(userId, occurredAt.AddMinutes(-1));

        progress.RecordAttempt(4, 0.8m, occurredAt);
        progress.RecordAttempt(3, 0.6m, occurredAt.AddMinutes(-1));

        Assert.Equal(userId, progress.UserId);
        Assert.Equal(4, progress.CurrentDay);
        Assert.Equal(2, progress.TotalPracticeAttempts);
        Assert.Equal(0.7m, progress.AverageAccuracy);
        Assert.Equal(occurredAt, progress.UpdatedAt);
    }

    [Fact]
    public void Study_streak_advances_once_per_consecutive_day_and_resets_after_a_gap()
    {
        var progress = UserProgressReadModel.Create(Guid.NewGuid(), DateTimeOffset.UtcNow);
        var firstDay = new DateTimeOffset(2026, 10, 16, 8, 0, 0, TimeSpan.Zero);

        progress.RecordGatePassed(1, 10, firstDay);
        progress.RecordSessionEnded(firstDay.AddHours(1), completedDay: true);
        progress.RecordGatePassed(2, 12, firstDay.AddDays(1));
        progress.RecordSessionEnded(firstDay.AddDays(1).AddHours(1), completedDay: true);

        Assert.Equal(2, progress.StreakCount);
        Assert.Equal(2, progress.CompletedDays);
        Assert.Equal(22, progress.ActiveVocabulary);

        progress.RecordGatePassed(3, 8, firstDay.AddDays(3));

        Assert.Equal(1, progress.StreakCount);
        Assert.Equal(30, progress.ActiveVocabulary);
    }

    [Fact]
    public void Repeated_completion_on_the_same_day_does_not_increment_completed_days()
    {
        var progress = UserProgressReadModel.Create(Guid.NewGuid(), DateTimeOffset.UtcNow);
        var today = new DateTimeOffset(2026, 10, 18, 8, 0, 0, TimeSpan.Zero);

        progress.RecordSessionEnded(today, completedDay: true);
        progress.RecordSessionEnded(today.AddHours(1), completedDay: true);

        Assert.Equal(1, progress.CompletedDays);
        Assert.Equal(1, progress.StreakCount);
    }
}