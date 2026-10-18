namespace EnglishSchool.Modules.Curriculum.Domain;

internal enum DayKind
{
    Lesson,
    Review,
    Exam,
}

/// <summary>Kursning bitta kuni (1..56). Kontent oʻzgarmas, yangi tahrir yangi kurs versiyasi boʻladi.</summary>
internal sealed class CourseDay
{
    public Guid Id { get; init; } = Guid.CreateVersion7();

    public int CourseVersion { get; init; }

    public int DayNumber { get; init; }

    public int WeekNumber { get; init; }

    public DayKind Kind { get; init; }

    public required string Title { get; init; }

    public string? Description { get; init; }

    public string? VocabularyTheme { get; init; }

    public List<string> SourceRefs { get; init; } = [];

    public string? PreparesFor { get; init; }

    public GrammarTopic? Grammar { get; init; }

    public List<VocabularyItem> Vocabulary { get; init; } = [];
}

internal sealed class GrammarTopic
{
    public Guid Id { get; init; } = Guid.CreateVersion7();

    public Guid DayId { get; init; }

    public required string Title { get; init; }

    public List<string> Formula { get; init; } = [];

    public List<string> Examples { get; init; } = [];

    public List<string> Notes { get; init; } = [];

    /// <summary>"Diqqat" bandlari. Keyinchalik Tier 0 qoidalari va AI qoida varaqasi shulardan quriladi.</summary>
    public List<string> Warnings { get; init; } = [];
}

internal sealed class VocabularyItem
{
    public Guid Id { get; init; } = Guid.CreateVersion7();

    public Guid DayId { get; init; }

    public int Position { get; init; }

    public required string English { get; init; }

    public required string Uzbek { get; init; }

    public bool IsPhrase { get; init; }
}
