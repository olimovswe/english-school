namespace EnglishSchool.Modules.Discipline.Domain;

internal sealed class FocusBlock
{
    private FocusBlock()
    {
    }

    public Guid Id { get; private init; } = Guid.CreateVersion7();

    public Guid StudySessionId { get; private init; }

    public string BlockType { get; private init; } = string.Empty;

    public int DurationSeconds { get; private set; }

    public int DistractionCount { get; private set; }

    public bool IsCompleted { get; private set; }

    public DateTimeOffset StartedAt { get; private init; }

    public DateTimeOffset? CompletedAt { get; private set; }

    public static FocusBlock Create(Guid studySessionId, string blockType, DateTimeOffset now) => new()
    {
        StudySessionId = studySessionId,
        BlockType = blockType,
        StartedAt = now,
    };
}