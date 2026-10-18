namespace EnglishSchool.Modules.Discipline.Domain;

internal enum StudySessionStatus
{
    Created,
    MorningGate,
    Active,
    FocusBlock,
    Completed,
    Abandoned,
}

internal sealed class StudySession
{
    private StudySession()
    {
    }

    public Guid Id { get; private init; } = Guid.CreateVersion7();

    public Guid UserId { get; private init; }

    public Guid DailyPlanId { get; private init; }

    public StudySessionStatus Status { get; private set; }

    public DateTimeOffset StartedAt { get; private init; }

    public DateTimeOffset? EndedAt { get; private set; }

    public int TotalFocusSeconds { get; private set; }

    public bool MinimalDay { get; private set; }

    public static StudySession Create(Guid userId, Guid dailyPlanId, DateTimeOffset now) => new()
    {
        UserId = userId,
        DailyPlanId = dailyPlanId,
        Status = StudySessionStatus.Created,
        StartedAt = now,
    };
}