using EnglishSchool.BuildingBlocks.Abstractions.Events;
using EnglishSchool.Modules.Discipline.Contracts.Events;

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

internal sealed class StudySession : IntegrationEventSource
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

    public void BeginMorningGate()
    {
        EnsureStatus(StudySessionStatus.Created);
        Status = StudySessionStatus.MorningGate;
    }

    public void PassMorningGate()
    {
        EnsureStatus(StudySessionStatus.MorningGate);
        Status = StudySessionStatus.Active;
    }

    public FocusBlock StartFocusBlock(string blockType, DateTimeOffset now)
    {
        EnsureStatus(StudySessionStatus.Active);
        Status = StudySessionStatus.FocusBlock;
        return FocusBlock.Create(UserId, Id, blockType, now);
    }

    public void CompleteFocusBlock(FocusBlock block, bool minimalDay, DateTimeOffset now)
    {
        EnsureStatus(StudySessionStatus.FocusBlock);

        if (!block.IsCompleted || block.StudySessionId != Id)
        {
            throw new InvalidOperationException("The focus block must be completed and belong to this session.");
        }

        TotalFocusSeconds += block.DurationSeconds;
        MinimalDay = minimalDay;
        EndedAt = now;
        Status = StudySessionStatus.Completed;
        Raise(new SessionEnded(UserId, Id, TotalFocusSeconds, MinimalDay));
    }

    public void Abandon(DateTimeOffset now)
    {
        if (Status is StudySessionStatus.Completed or StudySessionStatus.Abandoned)
        {
            throw new InvalidOperationException("A terminal session cannot transition again.");
        }

        EndedAt = now;
        Status = StudySessionStatus.Abandoned;
        Raise(new SessionEnded(UserId, Id, TotalFocusSeconds, MinimalDay));
    }

    private void EnsureStatus(StudySessionStatus expected)
    {
        if (Status != expected)
        {
            throw new InvalidOperationException($"Session transition requires status {expected}; current status is {Status}.");
        }
    }
}