using EnglishSchool.Modules.Curriculum.Domain;
using EnglishSchool.Modules.Curriculum.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EnglishSchool.Modules.Curriculum.Seed;

/// <summary>
/// Lokal muhitda migratsiyalarni qoʻllaydi va kurs versiyasi bazada boʻlmasa, uni seed qiladi.
/// Idempotent: qayta ishga tushirilganda hech narsani takrorlamaydi.
/// </summary>
internal sealed partial class CurriculumSeeder(
    IServiceScopeFactory scopeFactory,
    IOptions<CurriculumOptions> options,
    ILogger<CurriculumSeeder> logger) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var settings = options.Value;
        if (!settings.ApplyMigrationsOnStartup && !settings.SeedOnStartup)
        {
            return;
        }

        await using var scope = scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<CurriculumDbContext>();

        if (settings.ApplyMigrationsOnStartup)
        {
            await db.Database.MigrateAsync(cancellationToken);
        }

        if (!settings.SeedOnStartup)
        {
            return;
        }

        var course = CurriculumSeedLoader.LoadEmbedded();
        if (await db.Days.AnyAsync(d => d.CourseVersion == course.Version, cancellationToken))
        {
            LogAlreadySeeded(logger, course.Version);
            return;
        }

        var days = course.Days.Select(day => ToEntity(course.Version, day)).ToList();
        db.Days.AddRange(days);
        await db.SaveChangesAsync(cancellationToken);

        var words = days.Sum(d => d.Vocabulary.Count(v => !v.IsPhrase));
        var phrases = days.Sum(d => d.Vocabulary.Count(v => v.IsPhrase));
        LogSeeded(logger, course.Version, days.Count, words, phrases);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    internal static CourseDay ToEntity(int courseVersion, SeedDay day)
    {
        var id = Guid.CreateVersion7();
        var vocabulary = day.Vocabulary;

        var items = vocabulary is null
            ? []
            : vocabulary.Words.Select(w => (Term: w, IsPhrase: false))
                .Concat(vocabulary.Phrases.Select(p => (Term: p, IsPhrase: true)))
                .Select((x, index) => new VocabularyItem
                {
                    DayId = id,
                    Position = index + 1,
                    English = x.Term.En,
                    Uzbek = x.Term.Uz,
                    IsPhrase = x.IsPhrase,
                })
                .ToList();

        return new CourseDay
        {
            Id = id,
            CourseVersion = courseVersion,
            DayNumber = day.Day,
            WeekNumber = day.Week,
            Kind = Enum.Parse<DayKind>(day.Kind, ignoreCase: true),
            Title = day.Title,
            Description = day.Description,
            VocabularyTheme = vocabulary?.Theme,
            SourceRefs = vocabulary?.SourceRefs.ToList() ?? [],
            PreparesFor = vocabulary?.PreparesFor,
            Grammar = day.Grammar is null
                ? null
                : new GrammarTopic
                {
                    DayId = id,
                    Title = day.Grammar.Topic,
                    Formula = [.. day.Grammar.Formula],
                    Examples = [.. day.Grammar.Examples],
                    Notes = [.. day.Grammar.Notes],
                    Warnings = [.. day.Grammar.Warnings],
                },
            Vocabulary = items,
        };
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Curriculum v{Version} allaqachon bazada, seed oʻtkazib yuborildi.")]
    private static partial void LogAlreadySeeded(ILogger logger, int version);

    [LoggerMessage(Level = LogLevel.Information, Message = "Curriculum v{Version} seed qilindi: {Days} kun, {Words} soʻz, {Phrases} ibora.")]
    private static partial void LogSeeded(ILogger logger, int version, int days, int words, int phrases);
}
