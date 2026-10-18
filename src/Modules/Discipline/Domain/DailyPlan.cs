using EnglishSchool.BuildingBlocks.Abstractions.Events;

namespace EnglishSchool.Modules.Discipline.Domain;

internal sealed class DailyPlan : IntegrationEventSource
{
    private DailyPlan()
    {
    }

    public Guid Id { get; private init; } = Guid.CreateVersion7();

    public Guid UserId { get; private init; }

    public DateOnly AssignedDay { get; private init; }

    public bool MorningReviewPassed { get; private set; }

    public decimal? GatePassPercentage { get; private set; }

    public bool CurriculumCompleted { get; private set; }

    public bool PracticeCompleted { get; private set; }

    public bool FocusCompleted { get; private set; }

    public bool DayCompleted { get; private set; }

    public DateTimeOffset CreatedAt { get; private init; }

    public static DailyPlan Create(Guid userId, DateOnly assignedDay, DateTimeOffset now) => new()
    {
        UserId = userId,
        AssignedDay = assignedDay,
        CreatedAt = now,
    };
}