using EnglishSchool.Modules.Practice.Domain;
using EnglishSchool.Modules.Practice.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EnglishSchool.Modules.Practice.Seed;

/// <summary>
/// Lokal muhitda migratsiyalarni qoʻllaydi va mashqlar versiyasi bazada boʻlmasa, uni seed qiladi.
/// Idempotent: qayta ishga tushirilganda hech narsani takrorlamaydi.
/// </summary>
internal sealed partial class PracticeSeeder(
    IServiceScopeFactory scopeFactory,
    IOptions<PracticeOptions> options,
    ILogger<PracticeSeeder> logger) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var settings = options.Value;
        if (!settings.ApplyMigrationsOnStartup && !settings.SeedOnStartup)
        {
            return;
        }

        await using var scope = scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<PracticeDbContext>();

        if (settings.ApplyMigrationsOnStartup)
        {
            await db.Database.MigrateAsync(cancellationToken);
        }

        if (!settings.SeedOnStartup)
        {
            return;
        }

        var course = PracticeSeedLoader.LoadEmbedded();
        if (await db.Exercises.AnyAsync(e => e.CourseVersion == course.Version, cancellationToken))
        {
            LogAlreadySeeded(logger, course.Version);
            return;
        }

        var exercises = ToEntities(course);
        db.Exercises.AddRange(exercises);
        await db.SaveChangesAsync(cancellationToken);

        var days = exercises.DistinctBy(e => e.DayNumber).Count();
        LogSeeded(logger, course.Version, exercises.Count, days);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    /// <summary>Mashqning kun ichidagi oʻrni JSON'dagi tartibdan olinadi.</summary>
    internal static List<Exercise> ToEntities(SeedPracticeCourse course) =>
    [
        .. course.Exercises
            .GroupBy(e => e.Day)
            .SelectMany(day => day.Select((exercise, index) => ToEntity(course.Version, exercise, index + 1))),
    ];

    internal static Exercise ToEntity(int courseVersion, SeedExercise seed, int position)
    {
        var id = Guid.CreateVersion7();
        var type = Enum.Parse<ExerciseType>(seed.Type, ignoreCase: true);

        var options = type switch
        {
            ExerciseType.GapFill => (seed.Accepted ?? []).Select(text => new OptionSeed(text, IsCorrect: true))
                .Concat((seed.Mistakes ?? []).Select(m => new OptionSeed(m.Text, IsCorrect: false, ErrorCode: m.ErrorCode, Feedback: m.Feedback))),
            ExerciseType.MultipleChoice => (seed.Options ?? [])
                .Select(c => new OptionSeed(c.Text, c.Correct, ErrorCode: c.ErrorCode, Feedback: c.Feedback)),
            ExerciseType.Matching => (seed.Pairs ?? []).Select(p => new OptionSeed(p.En, IsCorrect: true, MatchText: p.Uz)),
            _ => throw new InvalidOperationException($"Nomaʼlum mashq turi: {seed.Type}."),
        };

        return new Exercise
        {
            Id = id,
            CourseVersion = courseVersion,
            DayNumber = seed.Day,
            Position = position,
            Type = type,
            Instruction = seed.Instruction,
            Prompt = seed.Prompt,
            Explanation = seed.Explanation,
            AnswerPattern = seed.Pattern,
            Options =
            [
                .. options.Select((o, index) => new ExerciseOption
                {
                    ExerciseId = id,
                    Position = index + 1,
                    Text = o.Text,
                    IsCorrect = o.IsCorrect,
                    MatchText = o.MatchText,
                    ErrorCode = o.ErrorCode,
                    Feedback = o.Feedback,
                }),
            ],
        };
    }

    private sealed record OptionSeed(
        string Text, bool IsCorrect, string? MatchText = null, string? ErrorCode = null, string? Feedback = null);

    [LoggerMessage(Level = LogLevel.Information, Message = "Practice v{Version} allaqachon bazada, seed oʻtkazib yuborildi.")]
    private static partial void LogAlreadySeeded(ILogger logger, int version);

    [LoggerMessage(Level = LogLevel.Information, Message = "Practice v{Version} seed qilindi: {Exercises} mashq, {Days} kun.")]
    private static partial void LogSeeded(ILogger logger, int version, int exercises, int days);
}
