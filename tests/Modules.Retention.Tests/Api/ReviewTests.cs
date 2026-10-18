using System.Net;
using EnglishSchool.BuildingBlocks.Outbox;
using EnglishSchool.Modules.Retention.Contracts.Events;
using EnglishSchool.Modules.Retention.Domain;
using EnglishSchool.Modules.Retention.Infrastructure;
using EnglishSchool.Modules.Retention.Scheduling;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace EnglishSchool.Modules.Retention.Tests.Api;

[Collection(RetentionApiFactory.CollectionName)]
public sealed class ReviewTests(RetentionApiFactory factory)
{
    [Fact]
    public async Task Due_list_shows_only_the_students_own_cards_in_course_order_up_to_the_limit()
    {
        using var student = await factory.CreateStudentAsync();
        using var classmate = await factory.CreateStudentAsync();
        await factory.EnrollAsync(student.UserId, 2);
        await factory.EnrollAsync(classmate.UserId, 3);

        var due = await student.GetDueAsync(limit: 3);
        var all = await student.GetDueAsync(limit: 100);

        Assert.Equal(RetentionApi.DayTwoVocabulary, due.TotalDue);
        Assert.Equal(RetentionApi.DayTwoFirstWords, due.Cards.Select(c => c.Front));
        Assert.Equal(["xursand", "xafa, gʻamgin", "charchagan"], due.Cards.Select(c => c.Back));
        Assert.Equal(RetentionApi.DayTwoVocabulary, all.Cards.Count);
        Assert.Equal(all.Cards.OrderBy(c => c.DueAt).Select(c => c.Id), all.Cards.Select(c => c.Id));
    }

    [Fact]
    public async Task Rating_a_new_card_good_starts_learning_and_logs_the_review()
    {
        using var student = await factory.CreateStudentAsync();
        await factory.EnrollAsync(student.UserId, 2);
        var card = (await student.GetDueAsync(limit: 1)).Cards[0];

        var result = await student.ReviewSuccessfullyAsync(card.Id, rating: 3);

        Assert.Equal(("Learning", 0, 3.1262, 5.3146, false), (result.Phase, result.ScheduledDays, result.Stability, result.Difficulty, result.Lapsed));
        Assert.InRange(result.DueAt - DateTimeOffset.UtcNow, TimeSpan.FromMinutes(9), TimeSpan.FromMinutes(10));
        Assert.Equal(RetentionApi.DayTwoVocabulary - 1, (await student.GetDueAsync()).TotalDue);

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<RetentionDbContext>();
        var review = await db.CardReviews.SingleAsync(r => r.FlashcardId == card.Id);
        Assert.Equal((Rating.Good, CardPhase.New, 0, 3_000), (review.Rating, review.PhaseBefore, review.ElapsedDays, review.DurationMs));
        Assert.Null(review.Retrievability);
    }

    [Fact]
    public async Task Forgetting_a_learned_card_publishes_CardLapsed_through_the_outbox()
    {
        using var student = await factory.CreateStudentAsync();
        await factory.EnrollAsync(student.UserId, 2);
        var cards = (await student.GetDueAsync(limit: 2)).Cards;
        var (learned, fresh) = (cards[0], cards[1]);

        var graduated = await student.ReviewSuccessfullyAsync(learned.Id, rating: 4);
        var forgotten = await student.ReviewSuccessfullyAsync(learned.Id, rating: 1);
        var notYetLearned = await student.ReviewSuccessfullyAsync(fresh.Id, rating: 1);

        Assert.Equal(("Review", 15), (graduated.Phase, graduated.ScheduledDays));
        Assert.Equal(("Relearning", true), (forgotten.Phase, forgotten.Lapsed));
        Assert.Equal(("Learning", false), (notYetLearned.Phase, notYetLearned.Lapsed));

        var lapses = await LapsesOfAsync(student.UserId);
        var lapse = Assert.Single(lapses);
        Assert.Equal((learned.Id, "Word"), (lapse.Event.CardId, lapse.Event.ItemType));
        Assert.Null(lapse.ProcessedAt);

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<RetentionDbContext>();
        var card = await db.Flashcards.SingleAsync(f => f.Id == learned.Id);
        Assert.Equal((card.VocabularyItemId, 1), (lapse.Event.ItemId, card.State.Lapses));

        await factory.DispatchOutboxAsync();
        Assert.NotNull(Assert.Single(await LapsesOfAsync(student.UserId)).ProcessedAt);
    }

    [Fact]
    public async Task Reviews_and_due_lists_validate_input_and_ownership()
    {
        using var student = await factory.CreateStudentAsync();
        using var classmate = await factory.CreateStudentAsync();
        await factory.EnrollAsync(classmate.UserId, 2);
        var classmatesCard = (await classmate.GetDueAsync(limit: 1)).Cards[0].Id;
        using var anonymous = factory.CreateClient();

        using var tooHigh = await student.ReviewAsync(classmatesCard, 5);
        using var tooLow = await student.ReviewAsync(classmatesCard, 0);
        using var noCard = await student.ReviewAsync(null, 3);
        using var notMine = await student.ReviewAsync(classmatesCard, 3);
        using var zeroLimit = await student.Client.GetAsync(new Uri("/api/v1/retention/due?limit=0", UriKind.Relative));
        using var hugeLimit = await student.Client.GetAsync(new Uri("/api/v1/retention/due?limit=101", UriKind.Relative));
        using var signedOut = await anonymous.GetAsync(new Uri("/api/v1/retention/due", UriKind.Relative));

        Assert.All([tooHigh, tooLow, noCard, zeroLimit, hugeLimit], r => Assert.Equal(HttpStatusCode.BadRequest, r.StatusCode));
        Assert.Equal(HttpStatusCode.NotFound, notMine.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, signedOut.StatusCode);
        Assert.Equal(RetentionApi.DayTwoVocabulary, (await classmate.GetDueAsync()).TotalDue);
    }

    [Fact]
    public async Task A_stale_copy_of_a_card_cannot_overwrite_a_newer_review()
    {
        using var student = await factory.CreateStudentAsync();
        await factory.EnrollAsync(student.UserId, 2);
        var cardId = (await student.GetDueAsync(limit: 1)).Cards[0].Id;

        using var first = factory.Services.CreateScope();
        using var second = factory.Services.CreateScope();
        var (db1, db2) = (first.ServiceProvider.GetRequiredService<RetentionDbContext>(), second.ServiceProvider.GetRequiredService<RetentionDbContext>());
        var scheduler = first.ServiceProvider.GetRequiredService<FsrsScheduler>();
        var (card1, card2) = (await db1.Flashcards.SingleAsync(f => f.Id == cardId), await db2.Flashcards.SingleAsync(f => f.Id == cardId));
        var now = DateTimeOffset.UtcNow;

        _ = card1.Apply(scheduler.Schedule(card1.State, Rating.Good, now), Rating.Good, now, null);
        await db1.SaveChangesAsync();
        _ = card2.Apply(scheduler.Schedule(card2.State, Rating.Easy, now), Rating.Easy, now, null);

        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => db2.SaveChangesAsync());
    }

    private async Task<List<(CardLapsed Event, DateTimeOffset? ProcessedAt)>> LapsesOfAsync(Guid userId)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<RetentionDbContext>();
        var typeName = OutboxSerializer.TypeName(typeof(CardLapsed));

        var messages = await db.Set<OutboxMessage>().AsNoTracking().Where(m => m.Type == typeName).ToListAsync();
        return
        [
            .. messages
                .Select(m => ((CardLapsed)OutboxSerializer.Deserialize(m.Content, typeof(CardLapsed)), m.ProcessedAt))
                .Where(x => x.Item1.UserId == userId),
        ];
    }
}
