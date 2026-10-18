using EnglishSchool.BuildingBlocks.Abstractions.Events;
using EnglishSchool.Modules.Discipline.Contracts.Events;

namespace EnglishSchool.Modules.Discipline.Domain;

internal sealed class DailyPlan : IntegrationEventSource
{
    private DailyPlan()
    {
    }

    public Guid Id { get; private init; } = Guid.CreateVersion7();

    public Guid UserId { get; private init; }

    public int DayNumber { get; private init; }

    public DateOnly AssignedDay { get; private init; }

    public bool MorningReviewPassed { get; private set; }

    public decimal? GatePassPercentage { get; private set; }

    public bool CurriculumCompleted { get; private set; }

    public bool PracticeCompleted { get; private set; }

    public bool FocusCompleted { get; private set; }

    public bool DayCompleted { get; private set; }

    public DateTimeOffset CreatedAt { get; private init; }

    public static DailyPlan Create(Guid userId, int dayNumber, DateOnly assignedDay, DateTimeOffset now) => new()
    {
        UserId = userId,
        DayNumber = dayNumber,
        AssignedDay = assignedDay,
        CreatedAt = now,
    };

    public bool RecordMorningReview(decimal score)
    {
        if (score is < 0 or > 100)
        {
            throw new ArgumentOutOfRangeException(nameof(score), "Gate score must be between 0 and 100.");
        }

        if (MorningReviewPassed)
        {
            return true;
        }

        GatePassPercentage = score;
        MorningReviewPassed = score >= MorningReviewPassPercentage;

        if (MorningReviewPassed)
        {
            Raise(new DailyGatePassed(UserId, DayNumber, score));
        }

        return MorningReviewPassed;
    }

    public void MarkCurriculumCompleted()
    {
        if (!MorningReviewPassed)
        {
            throw new InvalidOperationException("The morning review gate must be passed before completing curriculum lessons.");
        }

        CurriculumCompleted = true;
        UpdateDayCompletion();
    }

    public void MarkPracticeCompleted()
    {
        PracticeCompleted = true;
        UpdateDayCompletion();
    }

    public void MarkFocusCompleted()
    {
        FocusCompleted = true;
        UpdateDayCompletion();
    }

    private const decimal MorningReviewPassPercentage = 80m;

    private void UpdateDayCompletion() => DayCompleted = CurriculumCompleted && PracticeCompleted && FocusCompleted;
}