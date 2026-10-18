using EnglishSchool.Modules.Practice.Domain;
using EnglishSchool.Modules.Practice.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Options;

namespace EnglishSchool.Modules.Practice.Application;

/// <summary>
/// Kun mashqlari hamma uchun bir xil va versiyalangan: Curriculum kabi HybridCache'da saqlanadi.
/// Javoblar oshkor boʻlmasligi uchun MultipleChoice variantlari va Matching oʻng ustuni alifbo tartibida beriladi.
/// </summary>
internal sealed class ExerciseQueries(PracticeDbContext db, HybridCache cache, IOptions<PracticeOptions> options)
{
    private static readonly HybridCacheEntryOptions EntryOptions = new()
    {
        Expiration = TimeSpan.FromHours(6),
        LocalCacheExpiration = TimeSpan.FromMinutes(10),
    };

    private int Version => options.Value.ActiveCourseVersion;

    public async Task<IReadOnlyList<ExerciseDto>> GetForDayAsync(int dayNumber, CancellationToken cancellationToken) =>
        await cache.GetOrCreateAsync(
            $"prac:ex:{Version}:{dayNumber}",
            async token => await LoadAsync(dayNumber, token),
            EntryOptions,
            cancellationToken: cancellationToken);

    private async Task<IReadOnlyList<ExerciseDto>> LoadAsync(int dayNumber, CancellationToken cancellationToken)
    {
        var exercises = await db.Exercises
            .AsNoTracking()
            .Include(e => e.Options)
            .Where(e => e.CourseVersion == Version && e.DayNumber == dayNumber)
            .OrderBy(e => e.Position)
            .ToListAsync(cancellationToken);

        return [.. exercises.Select(ToDto)];
    }

    private static ExerciseDto ToDto(Exercise exercise)
    {
        IReadOnlyList<ExerciseChoiceDto> choices = exercise.Type switch
        {
            ExerciseType.MultipleChoice => [.. exercise.Options.OrderBy(o => o.Text, StringComparer.Ordinal).Select(Choice)],
            ExerciseType.Matching => [.. exercise.Options.OrderBy(o => o.Position).Select(Choice)],
            _ => [],
        };

        IReadOnlyList<string> targets = exercise.Type == ExerciseType.Matching
            ? [.. exercise.Options.Select(o => o.MatchText ?? string.Empty).Order(StringComparer.Ordinal)]
            : [];

        return new ExerciseDto(exercise.Id, exercise.Position, exercise.Type.ToString(), exercise.Instruction, exercise.Prompt, choices, targets);
    }

    private static ExerciseChoiceDto Choice(ExerciseOption option) => new(option.Id, option.Text);
}
