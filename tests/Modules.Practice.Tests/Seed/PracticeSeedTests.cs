using System.Reflection;
using System.Text.Json;
using System.Text.RegularExpressions;
using EnglishSchool.Modules.Practice.Domain;
using EnglishSchool.Modules.Practice.Grading;
using EnglishSchool.Modules.Practice.Seed;
using EnglishSchool.Modules.Practice.Tests.TestSupport;

namespace EnglishSchool.Modules.Practice.Tests.Seed;

/// <summary>
/// Mashqlar kontenti ham kod kabi tekshiriladi: har bir maʼlum javob haqiqiy baholash quvuridan oʻtkaziladi
/// va Matching tarjimalari Curriculum'dagi oʻsha kun soʻzlari bilan bir xil boʻlishi shart.
/// </summary>
public sealed partial class PracticeSeedTests
{
    private static readonly SeedPracticeCourse Course = PracticeSeedLoader.LoadEmbedded();
    private static readonly List<Exercise> Exercises = PracticeSeeder.ToEntities(Course);

    [GeneratedRegex(@"nʼt\b|ʼ(?:s|re|ve|ll|d)\b|\bIʼm\b")]
    private static partial Regex EnglishContractionWithModifierApostrophe();

    [GeneratedRegex("[oOgG]'[a-z]")]
    private static partial Regex AsciiOkina();

    [Fact]
    public void Exercises_cover_every_week_one_lesson_day_in_order()
    {
        Assert.Equal([1, 2, 3, 4, 5, 6], Exercises.Select(e => e.DayNumber).Distinct());
        Assert.All(Exercises.GroupBy(e => e.DayNumber), day =>
            Assert.Equal(Enumerable.Range(1, day.Count()), day.Select(e => e.Position)));
        Assert.All(Exercises.GroupBy(e => e.DayNumber), day =>
            Assert.Equal(3, day.Select(e => e.Type).Distinct().Count()));
    }

    [Fact]
    public void Every_exercise_is_well_formed_for_its_type()
    {
        Assert.All(Exercises, exercise =>
        {
            switch (exercise.Type)
            {
                case ExerciseType.GapFill:
                    Assert.Contains("___", exercise.Prompt, StringComparison.Ordinal);
                    Assert.Contains(exercise.Options, o => o.IsCorrect);
                    break;
                case ExerciseType.MultipleChoice:
                    Assert.Single(exercise.Options, o => o.IsCorrect);
                    Assert.InRange(exercise.Options.Count, 3, 4);
                    break;
                case ExerciseType.Matching:
                    Assert.Equal(5, exercise.Options.Count);
                    Assert.Equal(5, exercise.Options.Select(o => o.MatchText).Distinct().Count());
                    break;
            }

            if (exercise.Type != ExerciseType.Matching)
            {
                Assert.False(string.IsNullOrWhiteSpace(exercise.Explanation), exercise.Prompt);
            }
        });
    }

    [Fact]
    public void Every_wrong_option_names_a_known_error_and_explains_it()
    {
        var wrong = Exercises.Where(e => e.Type != ExerciseType.Matching).SelectMany(e => e.Options).Where(o => !o.IsCorrect).ToList();

        Assert.NotEmpty(wrong);
        Assert.All(wrong, option =>
        {
            Assert.True(option.ErrorCode is not null && ErrorCodes.IsKnown(option.ErrorCode), option.Text);
            Assert.False(string.IsNullOrWhiteSpace(option.Feedback), option.Text);
        });
    }

    [Fact]
    public async Task Authored_answers_grade_as_intended_through_the_real_pipeline()
    {
        var pipeline = new GradingPipeline([new RuleBasedTier(), new NormalizationTier(), new AiGatewayTier([])]);

        foreach (var exercise in Exercises.Where(e => e.Type == ExerciseType.GapFill))
        {
            foreach (var option in exercise.Options)
            {
                var outcome = await pipeline.GradeAsync(exercise.Typed(option.Text), CancellationToken.None);

                Assert.True(option.IsCorrect == (outcome.Verdict == Verdict.Correct), $"{exercise.Prompt}: {option.Text}");
                Assert.Equal(option.IsCorrect ? null : option.ErrorCode, outcome.Feedback.SingleOrDefault()?.ErrorCode);
            }
        }
    }

    [Fact]
    public void Matching_pairs_use_the_curriculum_translation_of_that_day()
    {
        var curriculum = LoadCurriculumWords();

        Assert.All(Exercises.Where(e => e.Type == ExerciseType.Matching).SelectMany(e => e.Options.Select(o => (e.DayNumber, o))), x =>
            Assert.Equal(curriculum[(x.DayNumber, x.o.Text)], x.o.MatchText));
    }

    [Fact]
    public void Text_follows_the_apostrophe_conventions()
    {
        var uzbek = Exercises.SelectMany(e => new[] { e.Instruction, e.Explanation }.Concat(e.Options.Select(o => o.Feedback)))
            .Concat(Exercises.Select(e => e.Prompt))
            .OfType<string>()
            .ToList();
        var all = uzbek.Concat(Exercises.SelectMany(e => e.Options).SelectMany(o => new[] { o.Text, o.MatchText }).OfType<string>());

        Assert.DoesNotContain(uzbek, text => AsciiOkina().IsMatch(text));
        Assert.DoesNotContain(all, text => EnglishContractionWithModifierApostrophe().IsMatch(text));
    }

    /// <summary>Curriculum seed'i oʻz assembly'sida: modul ichki kodiga emas, faqat kontentiga tayaniladi.</summary>
    private static Dictionary<(int Day, string En), string> LoadCurriculumWords()
    {
        using var stream = Assembly.Load(new AssemblyName("EnglishSchool.Modules.Curriculum"))
            .GetManifestResourceStream("Curriculum.Seed.curriculum.v1.json")!;
        using var document = JsonDocument.Parse(stream);

        return document.RootElement.GetProperty("days").EnumerateArray()
            .Where(day => day.GetProperty("vocabulary").ValueKind == JsonValueKind.Object)
            .SelectMany(day => day.GetProperty("vocabulary").GetProperty("words").EnumerateArray()
                .Select(word => (Day: day.GetProperty("day").GetInt32(), En: word.GetProperty("en").GetString()!, Uz: word.GetProperty("uz").GetString()!)))
            .ToDictionary(w => (w.Day, w.En), w => w.Uz);
    }
}
