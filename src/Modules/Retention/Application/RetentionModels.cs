namespace EnglishSchool.Modules.Retention.Application;

/// <param name="TotalDue">Hozir navbatdagi barcha kartochkalar soni (<see cref="Cards"/> esa birinchi partiya).</param>
internal sealed record DueCardsResponse(int TotalDue, IReadOnlyList<DueCardDto> Cards);

/// <summary>Kartochka: oldi (inglizcha) koʻrsatiladi, orqasi (oʻzbekcha) javobdan keyin ochiladi.</summary>
internal sealed record DueCardDto(
    Guid Id,
    string Kind,
    string Front,
    string Back,
    string Phase,
    DateTimeOffset DueAt,
    int Reps,
    int Lapses);

/// <param name="Rating">1 Again · 2 Hard · 3 Good · 4 Easy.</param>
internal sealed record ReviewCardRequest(Guid? CardId, int? Rating, int? DurationMs);

internal sealed record ReviewResultResponse(
    Guid CardId,
    string Phase,
    DateTimeOffset DueAt,
    int ScheduledDays,
    double Stability,
    double Difficulty,
    bool Lapsed);

internal enum ReviewStatus
{
    Reviewed,
    CardNotFound,
    Conflict,
}

internal sealed record ReviewOutcome(ReviewStatus Status, ReviewResultResponse? Result = null);
