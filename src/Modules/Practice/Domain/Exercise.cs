namespace EnglishSchool.Modules.Practice.Domain;

internal enum ExerciseType
{
    GapFill,
    MultipleChoice,
    Matching,
}

/// <summary>
/// Kursning bitta kuniga bogʻlangan mashq. Kontent oʻzgarmas va kurs versiyasiga bogʻlangan (Curriculum kabi).
/// <see cref="Options"/> maʼnosi turga qarab: GapFill — qabul qilinadigan javoblar va maʼlum xatolar,
/// MultipleChoice — tanlovlar, Matching — juftliklar (<see cref="ExerciseOption.Text"/> ↔ <see cref="ExerciseOption.MatchText"/>).
/// </summary>
internal sealed class Exercise
{
    public Guid Id { get; init; } = Guid.CreateVersion7();

    public int CourseVersion { get; init; }

    public int DayNumber { get; init; }

    public int Position { get; init; }

    public ExerciseType Type { get; init; }

    public required string Instruction { get; init; }

    public required string Prompt { get; init; }

    /// <summary>Notoʻgʻri javobda koʻrsatiladigan qoida (variantda oʻz izohi boʻlmasa).</summary>
    public string? Explanation { get; init; }

    /// <summary>GapFill uchun ixtiyoriy: javob toʻliq mos kelishi kerak boʻlgan regex (Tier 0).</summary>
    public string? AnswerPattern { get; init; }

    public List<ExerciseOption> Options { get; init; } = [];
}

internal sealed class ExerciseOption
{
    public Guid Id { get; init; } = Guid.CreateVersion7();

    public Guid ExerciseId { get; init; }

    public int Position { get; init; }

    public required string Text { get; init; }

    public bool IsCorrect { get; init; }

    public string? MatchText { get; init; }

    /// <summary>Notoʻgʻri variant tanlansa yoki yozilsa, qaysi xato (<see cref="ErrorCodes"/>) aniqlanadi.</summary>
    public string? ErrorCode { get; init; }

    public string? Feedback { get; init; }
}
