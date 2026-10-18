namespace EnglishSchool.Modules.Analytics.Domain;

internal sealed class UserProgressReadModel
{
    private UserProgressReadModel()
    {
    }

    public Guid UserId { get; private init; }

    public int CurrentDay { get; private set; }

    public int CompletedDays { get; private set; }

    public int TotalPracticeAttempts { get; private set; }

    public int StreakCount { get; private set; }

    public int ActiveVocabulary { get; private set; }

    public decimal AccuracyScoreTotal { get; private set; }

    public int AccuracyAttemptCount { get; private set; }

    public DateOnly? LastStudyDay { get; private set; }

    public DateOnly? LastCompletedDay { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; }

    public static UserProgressReadModel Create(Guid userId, DateTimeOffset now) => new()
    {
        UserId = userId,
        UpdatedAt = now,
    };

    public void RecordAttempt(int dayNumber, decimal score, DateTimeOffset occurredAt)
    {
        CurrentDay = Math.Max(CurrentDay, dayNumber);
        TotalPracticeAttempts++;
        AccuracyScoreTotal += score;
        AccuracyAttemptCount++;
        UpdatedAt = Max(UpdatedAt, occurredAt);
    }

    public void RecordGatePassed(int dayNumber, int activeVocabulary, DateTimeOffset occurredAt)
    {
        CurrentDay = Math.Max(CurrentDay, dayNumber);
        ActiveVocabulary += activeVocabulary;
        RecordStudyDay(DateOnly.FromDateTime(occurredAt.UtcDateTime));
        UpdatedAt = Max(UpdatedAt, occurredAt);
    }

    public void RecordSessionEnded(DateTimeOffset occurredAt, bool completedDay)
    {
        var studyDay = DateOnly.FromDateTime(occurredAt.UtcDateTime);
        RecordStudyDay(studyDay);
        if (completedDay && (LastCompletedDay is null || studyDay > LastCompletedDay))
        {
            CompletedDays++;
            LastCompletedDay = studyDay;
        }

        UpdatedAt = Max(UpdatedAt, occurredAt);
    }

    public decimal AverageAccuracy => AccuracyAttemptCount == 0 ? 0 : AccuracyScoreTotal / AccuracyAttemptCount;

    private void RecordStudyDay(DateOnly studyDay)
    {
        if (LastStudyDay is null)
        {
            StreakCount = 1;
            LastStudyDay = studyDay;
        }
        else if (studyDay > LastStudyDay)
        {
            StreakCount = studyDay == LastStudyDay.Value.AddDays(1) ? StreakCount + 1 : 1;
            LastStudyDay = studyDay;
        }
    }

    private static DateTimeOffset Max(DateTimeOffset left, DateTimeOffset right) => left > right ? left : right;
}