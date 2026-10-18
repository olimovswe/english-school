namespace EnglishSchool.Modules.Curriculum.Contracts;

/// <summary>
/// Boshqa modullar kurs kontentini faqat shu interfeys orqali oʻqiydi
/// (masalan, Practice mashq uchun qoida varaqasini oladi).
/// </summary>
public interface ICurriculumQueries
{
    Task<IReadOnlyList<DaySummaryDto>> GetDaysAsync(CancellationToken cancellationToken);

    Task<DayDto?> GetDayAsync(int dayNumber, CancellationToken cancellationToken);
}

public sealed record DaySummaryDto(int DayNumber, int WeekNumber, string Kind, string Title);

public sealed record DayDto(
    int DayNumber,
    int WeekNumber,
    string Kind,
    string Title,
    string? Description,
    GrammarTopicDto? Grammar,
    VocabularySetDto? Vocabulary);

public sealed record GrammarTopicDto(
    string Title,
    IReadOnlyList<string> Formula,
    IReadOnlyList<string> Examples,
    IReadOnlyList<string> Notes,
    IReadOnlyList<string> Warnings);

/// <param name="SourceRefs">Faqat havola: "EVU:14", "DEST:36". Kitob matni saqlanmaydi.</param>
/// <param name="PreparesFor">Bu soʻzlar qaysi kungi grammatika uchun tayyorlov ekani.</param>
public sealed record VocabularySetDto(
    string Theme,
    IReadOnlyList<string> SourceRefs,
    string? PreparesFor,
    IReadOnlyList<VocabularyItemDto> Words,
    IReadOnlyList<VocabularyItemDto> Phrases);

public sealed record VocabularyItemDto(string English, string Uzbek);
