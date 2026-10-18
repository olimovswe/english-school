using EnglishSchool.BuildingBlocks.Abstractions.Events;
using EnglishSchool.Modules.Discipline.Contracts.Events;
using EnglishSchool.Modules.Discipline.Domain;

namespace EnglishSchool.Modules.Discipline.Tests.Domain;

public sealed class DailyPlanTests
{
    [Theory]
    [InlineData(79.99, false)]
    [InlineData(80, true)]
    [InlineData(100, true)]
    public void Morning_review_gate_uses_the_eighty_percent_boundary(decimal score, bool expected)
    {
        var plan = DailyPlan.Create(Guid.NewGuid(), 1, new DateOnly(2026, 10, 18), DateTimeOffset.UtcNow);

        var passed = plan.RecordMorningReview(score);

        Assert.Equal(expected, passed);
        Assert.Equal(expected, plan.MorningReviewPassed);
        Assert.Equal(score, plan.GatePassPercentage);
        Assert.Equal(expected ? 1 : 0, ((IIntegrationEventSource)plan).IntegrationEvents.Count);
    }

    [Fact]
    public void Passing_the_gate_is_durable_and_emits_one_unlock_event()
    {
        var plan = DailyPlan.Create(Guid.NewGuid(), 4, new DateOnly(2026, 10, 18), DateTimeOffset.UtcNow);

        Assert.True(plan.RecordMorningReview(80));
        Assert.True(plan.RecordMorningReview(0));

        var gateEvent = Assert.IsType<DailyGatePassed>(Assert.Single(((IIntegrationEventSource)plan).IntegrationEvents));
        Assert.Equal((4, 80m), (gateEvent.DayNumber, gateEvent.Score));
        Assert.Equal(80m, plan.GatePassPercentage);
    }

    [Fact]
    public void Curriculum_completion_requires_a_passed_gate()
    {
        var plan = DailyPlan.Create(Guid.NewGuid(), 1, new DateOnly(2026, 10, 18), DateTimeOffset.UtcNow);

        Assert.Throws<InvalidOperationException>(plan.MarkCurriculumCompleted);
        Assert.True(plan.RecordMorningReview(80));
        plan.MarkCurriculumCompleted();
        plan.MarkPracticeCompleted();
        plan.MarkFocusCompleted();

        Assert.True(plan.DayCompleted);
    }
}