using System.Text.RegularExpressions;
using EnglishSchool.Modules.Curriculum.Domain;
using EnglishSchool.Modules.Curriculum.Seed;

namespace EnglishSchool.Modules.Curriculum.Tests;

/// <summary>
/// Seed fayli metodik rejaning raqamli nusxasi. Bu testlar rejadagi kelishuvlar
/// (56 kun, 1117 soʻz, 235 ibora, takrorlash kunlari) buzilmasligini kafolatlaydi.
/// </summary>
public sealed partial class CurriculumSeedTests
{
    private static readonly SeedCourse Course = CurriculumSeedLoader.LoadEmbedded();

    /// <summary>
    /// Inglizcha qisqartmalar (can't, 's, we're) ichidagi ʼ (U+02BC). Oʻzbekcha tutuq belgisi
    /// (feʼl, taʼm) bu qoliplarga tushmaydi, shuning uchun qoida barcha matn maydonlariga qoʻllanadi.
    /// </summary>
    [GeneratedRegex(@"nʼt\b|ʼ(?:s|re|ve|ll|d)\b|\bIʼm\b")]
    private static partial Regex EnglishContractionWithModifierApostrophe();

    [Fact]
    public void Course_has_56_consecutive_days_in_8_weeks()
    {
        Assert.Equal(Enumerable.Range(1, 56), Course.Days.Select(d => d.Day));
        Assert.All(Course.Days, d => Assert.Equal(((d.Day - 1) / 7) + 1, d.Week));
    }

    [Fact]
    public void Every_seventh_day_is_review_or_exam()
    {
        int[] reviewDays = [7, 14, 21, 35, 42, 49];
        int[] examDays = [28, 56];

        Assert.Equal(reviewDays, Course.Days.Where(d => d.Kind == "review").Select(d => d.Day));
        Assert.Equal(examDays, Course.Days.Where(d => d.Kind == "exam").Select(d => d.Day));
        Assert.Equal(48, Course.Days.Count(d => d.Kind == "lesson"));
    }

    [Fact]
    public void Every_lesson_day_has_grammar_with_formula()
    {
        Assert.All(Course.Days.Where(d => d.Kind == "lesson"), d =>
        {
            Assert.NotNull(d.Grammar);
            Assert.NotEmpty(d.Grammar.Formula);
        });
    }

    [Fact]
    public void Vocabulary_totals_match_the_plan()
    {
        var sets = Course.Days.Where(d => d.Vocabulary is not null).Select(d => d.Vocabulary!).ToList();

        Assert.Equal(47, sets.Count);
        Assert.Equal(1117, sets.Sum(v => v.Words.Count));
        Assert.Equal(235, sets.Sum(v => v.Phrases.Count));
    }

    [Fact]
    public void Words_are_unique_and_translated()
    {
        var words = Course.Days.SelectMany(d => d.Vocabulary?.Words ?? []).ToList();

        var duplicates = words.GroupBy(w => w.En.Trim().ToUpperInvariant())
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToList();

        Assert.Empty(duplicates);
        Assert.All(words, w => Assert.False(string.IsNullOrWhiteSpace(w.Uz), w.En));
    }

    [Fact]
    public void Lesson_days_carry_vocabulary_except_final_preparation_day()
    {
        var withoutVocabulary = Course.Days
            .Where(d => d.Kind == "lesson" && d.Vocabulary is null)
            .Select(d => d.Day);

        Assert.Equal([55], withoutVocabulary);
    }

    [Fact]
    public void Uzbek_text_uses_proper_okina_not_ascii_apostrophe()
    {
        var uzbek = Course.Days
            .SelectMany(d => d.Vocabulary?.Words.Concat(d.Vocabulary.Phrases) ?? [])
            .Select(t => t.Uz);

        Assert.DoesNotContain(uzbek, text => text.Contains('\'', StringComparison.Ordinal));
    }

    [Fact]
    public void English_contractions_use_ascii_apostrophe_in_every_text_field()
    {
        var offending = Course.Days
            .SelectMany(d => AllText(d).Select(text => (d.Day, Text: text)))
            .Where(x => EnglishContractionWithModifierApostrophe().IsMatch(x.Text))
            .Select(x => $"{x.Day}-kun: {x.Text}");

        Assert.Empty(offending);
    }

    [Fact]
    public void Seed_day_maps_to_entity_with_ordered_words_then_phrases()
    {
        var day = CurriculumSeeder.ToEntity(Course.Version, Course.Days[0]);

        Assert.Equal(DayKind.Lesson, day.Kind);
        Assert.NotNull(day.Grammar);
        Assert.Equal(Enumerable.Range(1, day.Vocabulary.Count), day.Vocabulary.Select(v => v.Position));
        Assert.False(day.Vocabulary.First().IsPhrase);
        Assert.True(day.Vocabulary.Last().IsPhrase);
        Assert.All(day.Vocabulary, v => Assert.Equal(day.Id, v.DayId));
    }

    private static IEnumerable<string> AllText(SeedDay day)
    {
        IEnumerable<string?> text = [day.Title, day.Description];

        if (day.Grammar is { } grammar)
        {
            text = text.Append(grammar.Topic)
                .Concat(grammar.Formula).Concat(grammar.Examples).Concat(grammar.Notes).Concat(grammar.Warnings);
        }

        if (day.Vocabulary is { } vocabulary)
        {
            text = text.Append(vocabulary.Theme).Append(vocabulary.PreparesFor)
                .Concat(vocabulary.Words.Concat(vocabulary.Phrases).SelectMany(t => new[] { t.En, t.Uz }));
        }

        return text.OfType<string>();
    }
}
