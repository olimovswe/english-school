using EnglishSchool.Modules.Curriculum.Contracts;
using EnglishSchool.Modules.Retention.Domain;
using EnglishSchool.Modules.Retention.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace EnglishSchool.Modules.Retention.Application;

/// <summary>
/// Kurs kunining lugʻatidan oʻquvchiga kartochkalar ochadi. Idempotent: eventlar kamida bir marta yetkaziladi,
/// shuning uchun mavjud kartochkalar qayta yaratilmaydi (unikal indeks parallel yetkazishni ham toʻsadi).
/// </summary>
internal sealed class FlashcardEnrollment(RetentionDbContext db, ICurriculumQueries curriculum, TimeProvider clock)
{
    /// <returns>Yangi ochilgan kartochkalar soni.</returns>
    public Task<int> EnrollDayAsync(Guid userId, int dayNumber, CancellationToken cancellationToken) =>
        EnrollAsync(userId, dayNumber, retryOnConflict: true, cancellationToken);

    private async Task<int> EnrollAsync(Guid userId, int dayNumber, bool retryOnConflict, CancellationToken cancellationToken)
    {
        if (dayNumber <= 0)
        {
            return 0;
        }

        var vocabulary = await curriculum.GetVocabularyAsync(dayNumber, cancellationToken);
        var ids = vocabulary.Select(v => v.Id).ToList();
        var existing = (await db.Flashcards
                .Where(f => f.UserId == userId && ids.Contains(f.VocabularyItemId))
                .Select(f => f.VocabularyItemId)
                .ToListAsync(cancellationToken))
            .ToHashSet();

        // Yangi kartochkalar kurs tartibida navbatga turishi uchun har biri bir mikrosekund keyin.
        var now = clock.GetUtcNow();
        var cards = vocabulary
            .Where(v => !existing.Contains(v.Id))
            .Select((v, index) => Flashcard.Create(
                userId, v.Id, v.IsPhrase ? FlashcardKind.Phrase : FlashcardKind.Word, v.English, v.Uzbek, dayNumber, now.AddMicroseconds(index)))
            .ToList();

        if (cards.Count == 0)
        {
            return 0;
        }

        db.Flashcards.AddRange(cards);
        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (retryOnConflict && IsUniqueViolation(exception))
        {
            // Shu kunning eventi parallel ham yetkazildi: qolgan kartochkalarni qayta hisoblaymiz.
            db.ChangeTracker.Clear();
            return await EnrollAsync(userId, dayNumber, retryOnConflict: false, cancellationToken);
        }

        return cards.Count;
    }

    private static bool IsUniqueViolation(DbUpdateException exception) =>
        exception.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation };
}
