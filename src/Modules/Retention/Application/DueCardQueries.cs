using EnglishSchool.Modules.Retention.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace EnglishSchool.Modules.Retention.Application;

/// <summary>
/// Hozir navbati kelgan kartochkalar: eng koʻp kutganidan boshlab (yangilari kurs tartibida). Daqiqalik
/// oʻrganish qadamlari vaqti kelganda roʻyxatga qaytadi, shuning uchun mijoz roʻyxatni sessiya davomida qayta soʻraydi.
/// </summary>
internal sealed class DueCardQueries(RetentionDbContext db, TimeProvider clock)
{
    public async Task<DueCardsResponse> GetDueAsync(Guid userId, int limit, CancellationToken cancellationToken)
    {
        var now = clock.GetUtcNow();
        var due = db.Flashcards.AsNoTracking().Where(f => f.UserId == userId && f.DueAt <= now);

        var total = await due.CountAsync(cancellationToken);
        var cards = await due
            .OrderBy(f => f.DueAt)
            .Take(limit)
            .Select(f => new DueCardDto(
                f.Id, f.Kind.ToString(), f.Front, f.Back, f.State.Phase.ToString(), f.DueAt, f.State.Reps, f.State.Lapses))
            .ToListAsync(cancellationToken);

        return new DueCardsResponse(total, cards);
    }
}
