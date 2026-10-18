using EnglishSchool.Modules.Curriculum.Contracts;
using EnglishSchool.Modules.Curriculum.Domain;
using EnglishSchool.Modules.Curriculum.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Options;

namespace EnglishSchool.Modules.Curriculum.Application;

/// <summary>
/// Kun kontenti hamma oʻquvchi uchun bir xil va versiyalangan, shuning uchun HybridCache'da
/// (L1 xotira + L2 Redis) saqlanadi. Kalitda kurs versiyasi bor: yangi versiya eski keshni chetlab oʻtadi.
/// </summary>
internal sealed class CurriculumQueries(
    CurriculumDbContext db,
    HybridCache cache,
    IOptions<CurriculumOptions> options) : ICurriculumQueries
{
    private static readonly HybridCacheEntryOptions EntryOptions = new()
    {
        Expiration = TimeSpan.FromHours(6),
        LocalCacheExpiration = TimeSpan.FromMinutes(10),
    };

    private int Version => options.Value.ActiveCourseVersion;

    public async Task<IReadOnlyList<DaySummaryDto>> GetDaysAsync(CancellationToken cancellationToken) =>
        await cache.GetOrCreateAsync(
            $"cur:days:{Version}",
            async token => await LoadDaysAsync(token),
            EntryOptions,
            cancellationToken: cancellationToken);

    public async Task<DayDto?> GetDayAsync(int dayNumber, CancellationToken cancellationToken) =>
        await cache.GetOrCreateAsync(
            $"cur:day:{Version}:{dayNumber}",
            async token => await LoadDayAsync(dayNumber, token),
            EntryOptions,
            cancellationToken: cancellationToken);

    private async Task<IReadOnlyList<DaySummaryDto>> LoadDaysAsync(CancellationToken cancellationToken) =>
        await db.Days
            .AsNoTracking()
            .Where(d => d.CourseVersion == Version)
            .OrderBy(d => d.DayNumber)
            .Select(d => new DaySummaryDto(d.DayNumber, d.WeekNumber, d.Kind.ToString(), d.Title))
            .ToListAsync(cancellationToken);

    private async Task<DayDto?> LoadDayAsync(int dayNumber, CancellationToken cancellationToken)
    {
        var day = await db.Days
            .AsNoTracking()
            .Include(d => d.Grammar)
            .Include(d => d.Vocabulary)
            .SingleOrDefaultAsync(d => d.CourseVersion == Version && d.DayNumber == dayNumber, cancellationToken);

        return day is null ? null : ToDto(day);
    }

    private static DayDto ToDto(CourseDay day)
    {
        var ordered = day.Vocabulary.OrderBy(v => v.Position).ToList();

        return new DayDto(
            day.DayNumber,
            day.WeekNumber,
            day.Kind.ToString(),
            day.Title,
            day.Description,
            day.Grammar is null
                ? null
                : new GrammarTopicDto(day.Grammar.Title, day.Grammar.Formula, day.Grammar.Examples, day.Grammar.Notes, day.Grammar.Warnings),
            day.VocabularyTheme is null
                ? null
                : new VocabularySetDto(
                    day.VocabularyTheme,
                    day.SourceRefs,
                    day.PreparesFor,
                    [.. ordered.Where(v => !v.IsPhrase).Select(v => new VocabularyItemDto(v.English, v.Uzbek))],
                    [.. ordered.Where(v => v.IsPhrase).Select(v => new VocabularyItemDto(v.English, v.Uzbek))]));
    }
}
