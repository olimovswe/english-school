using EnglishSchool.Modules.Retention.Domain;
using EnglishSchool.Modules.Retention.Infrastructure;
using EnglishSchool.Modules.Retention.Scheduling;
using Microsoft.EntityFrameworkCore;

namespace EnglishSchool.Modules.Retention.Application;

/// <summary>
/// Bahoni qoʻllaydi: FSRS yangi holat va navbatni hisoblaydi, takrorlash yozuvi va (unutilgan boʻlsa)
/// <c>CardLapsed</c> eventi kartochka bilan bitta tranzaksiyada saqlanadi.
/// </summary>
internal sealed class ReviewService(RetentionDbContext db, FsrsScheduler scheduler, TimeProvider clock)
{
    public async Task<ReviewOutcome> ReviewAsync(
        Guid userId, Guid cardId, Rating rating, int? durationMs, CancellationToken cancellationToken)
    {
        var card = await db.Flashcards.SingleOrDefaultAsync(f => f.Id == cardId && f.UserId == userId, cancellationToken);
        if (card is null)
        {
            return new ReviewOutcome(ReviewStatus.CardNotFound);
        }

        var now = clock.GetUtcNow();
        var schedule = scheduler.Schedule(card.State, rating, now);
        db.CardReviews.Add(card.Apply(schedule, rating, now, durationMs));

        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            // Shu kartochka parallel soʻrovda (masalan, ikki marta bosilgan tugma) allaqachon baholandi.
            return new ReviewOutcome(ReviewStatus.Conflict);
        }

        return new ReviewOutcome(
            ReviewStatus.Reviewed,
            new ReviewResultResponse(
                card.Id,
                card.State.Phase.ToString(),
                card.DueAt,
                card.ScheduledDays,
                Math.Round(card.State.Stability, 4),
                Math.Round(card.State.Difficulty, 4),
                schedule.IsLapse));
    }
}
