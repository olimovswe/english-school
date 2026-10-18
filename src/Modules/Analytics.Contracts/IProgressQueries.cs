namespace EnglishSchool.Modules.Analytics.Contracts;

public interface IProgressQueries
{
    Task<ProgressSummary> GetSummaryAsync(Guid userId, CancellationToken cancellationToken);
}

public sealed record ProgressSummary(
    Guid UserId,
    int CurrentDay,
    int FocusMinutesThisWeek,
    decimal ReviewAccuracy,
    int ActiveVocabulary);
