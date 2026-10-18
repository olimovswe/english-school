using EnglishSchool.BuildingBlocks.Abstractions.Events;
using EnglishSchool.Modules.Discipline.Contracts.Events;

namespace EnglishSchool.Modules.Discipline.Domain;

internal sealed class FocusBlock : IntegrationEventSource
{
    private FocusBlock()
    {
    }

    public Guid Id { get; private init; } = Guid.CreateVersion7();

    public Guid UserId { get; private init; }

    public Guid StudySessionId { get; private init; }

    public string BlockType { get; private init; } = string.Empty;

    public int DurationSeconds { get; private set; }

    public int DistractionCount { get; private set; }

    public bool IsCompleted { get; private set; }

    public DateTimeOffset StartedAt { get; private init; }

    public DateTimeOffset? CompletedAt { get; private set; }

    public static FocusBlock Create(Guid userId, Guid studySessionId, string blockType, DateTimeOffset now) => new()
    {
        UserId = userId,
        StudySessionId = studySessionId,
        BlockType = blockType,
        StartedAt = now,
    };

    public void Complete(int durationSeconds, int distractionCount, DateTimeOffset now)
    {
        if (IsCompleted)
        {
            throw new InvalidOperationException("A completed focus block cannot be completed again.");
        }

        ArgumentOutOfRangeException.ThrowIfNegative(durationSeconds);
        ArgumentOutOfRangeException.ThrowIfNegative(distractionCount);

        DurationSeconds = durationSeconds;
        DistractionCount = distractionCount;
        IsCompleted = true;
        CompletedAt = now;
        Raise(new BlockCompleted(UserId, StudySessionId, BlockType, durationSeconds));
    }
}