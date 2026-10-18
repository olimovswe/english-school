using EnglishSchool.BuildingBlocks.Abstractions.Events;
using EnglishSchool.Modules.Retention.Contracts.Events;
using EnglishSchool.Modules.Retention.Domain;

namespace EnglishSchool.Modules.Retention.Tests.Domain;

public sealed class FlashcardTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 19, 9, 0, 0, TimeSpan.Zero);

    [Fact]
    public void New_card_is_due_immediately_with_an_empty_memory()
    {
        var card = Phrase();

        Assert.Equal(Now, card.DueAt);
        Assert.Equal(CardState.Initial, card.State);
        Assert.Empty(((IIntegrationEventSource)card).IntegrationEvents);
    }

    [Fact]
    public void Applying_a_lapse_raises_CardLapsed_with_the_vocabulary_item()
    {
        var card = Phrase();
        var lapsed = new CardState(CardPhase.Relearning, 2.85, 5.36, 2, 1, Now);

        var review = card.Apply(new CardSchedule(lapsed, Now.AddMinutes(5), 0, 15, 0.9, IsLapse: true), Rating.Again, Now, 4_000);

        var raised = Assert.IsType<CardLapsed>(Assert.Single(((IIntegrationEventSource)card).IntegrationEvents));
        Assert.Equal((card.UserId, card.Id, "Phrase", card.VocabularyItemId), (raised.UserId, raised.CardId, raised.ItemType, raised.ItemId));
        Assert.Equal((lapsed, Now.AddMinutes(5)), (card.State, card.DueAt));
        Assert.Equal((Rating.Again, CardPhase.New, 15, 0.9, 2.85, 4_000), (review.Rating, review.PhaseBefore, review.ElapsedDays, review.Retrievability, review.Stability, review.DurationMs));
    }

    [Fact]
    public void Applying_any_other_schedule_raises_nothing()
    {
        var card = Phrase();
        var learning = new CardState(CardPhase.Learning, 3.1262, 5.31, 1, 0, Now);

        _ = card.Apply(new CardSchedule(learning, Now.AddMinutes(10), 0, 0, null, IsLapse: false), Rating.Good, Now, null);

        Assert.Empty(((IIntegrationEventSource)card).IntegrationEvents);
    }

    private static Flashcard Phrase() =>
        Flashcard.Create(Guid.NewGuid(), Guid.NewGuid(), FlashcardKind.Phrase, "Are you OK?", "Yaxshimisiz?", 2, Now);
}
