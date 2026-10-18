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

    public DateOnly? LastStudyDay { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; }

    public static UserProgressReadModel Create(Guid userId, DateTimeOffset now) => new()
    {
        UserId = userId,
        UpdatedAt = now,
    };
}