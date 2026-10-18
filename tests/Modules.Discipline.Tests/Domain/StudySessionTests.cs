using EnglishSchool.BuildingBlocks.Abstractions.Events;
using EnglishSchool.Modules.Discipline.Contracts.Events;
using EnglishSchool.Modules.Discipline.Domain;

namespace EnglishSchool.Modules.Discipline.Tests.Domain;

public sealed class StudySessionTests
{
    [Fact]
    public void Session_requires_each_state_transition_in_order_and_publishes_completion_events()
    {
        var now = DateTimeOffset.UtcNow;
        var session = StudySession.Create(Guid.NewGuid(), Guid.NewGuid(), now);

        Assert.Throws<InvalidOperationException>(session.PassMorningGate);
        session.BeginMorningGate();
        Assert.Throws<InvalidOperationException>(() => session.StartFocusBlock("Daily", now));
        session.PassMorningGate();
        var block = session.StartFocusBlock("Daily", now);
        block.Complete(1_200, 2, now.AddMinutes(20));
        session.CompleteFocusBlock(block, minimalDay: false, now.AddMinutes(20));

        Assert.Equal(StudySessionStatus.Completed, session.Status);
        Assert.Equal(1_200, session.TotalFocusSeconds);
        Assert.IsType<SessionEnded>(Assert.Single(((IIntegrationEventSource)session).IntegrationEvents));
        Assert.IsType<BlockCompleted>(Assert.Single(((IIntegrationEventSource)block).IntegrationEvents));
    }

    [Fact]
    public void Abandoned_session_is_terminal()
    {
        var session = StudySession.Create(Guid.NewGuid(), Guid.NewGuid(), DateTimeOffset.UtcNow);
        session.BeginMorningGate();
        session.Abandon(DateTimeOffset.UtcNow);

        Assert.Equal(StudySessionStatus.Abandoned, session.Status);
        Assert.Throws<InvalidOperationException>(() => session.Abandon(DateTimeOffset.UtcNow));
    }

    [Fact]
    public void Focus_block_rejects_negative_tracking_values_and_duplicate_completion()
    {
        var block = FocusBlock.Create(Guid.NewGuid(), Guid.NewGuid(), "Daily", DateTimeOffset.UtcNow);

        Assert.Throws<ArgumentOutOfRangeException>(() => block.Complete(-1, 0, DateTimeOffset.UtcNow));
        Assert.Throws<ArgumentOutOfRangeException>(() => block.Complete(0, -1, DateTimeOffset.UtcNow));
        block.Complete(10, 1, DateTimeOffset.UtcNow);
        Assert.Throws<InvalidOperationException>(() => block.Complete(10, 1, DateTimeOffset.UtcNow));
    }
}