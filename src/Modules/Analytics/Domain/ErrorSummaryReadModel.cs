namespace EnglishSchool.Modules.Analytics.Domain;

internal sealed class ErrorSummaryReadModel
{
    private ErrorSummaryReadModel()
    {
    }

    public Guid UserId { get; private init; }

    public string ErrorCode { get; private init; } = string.Empty;

    public string Correction { get; private set; } = string.Empty;

    public int Occurrences { get; private set; }

    public DateTimeOffset FirstSeenAt { get; private init; }

    public DateTimeOffset LastSeenAt { get; private set; }

    public Guid LastAttemptId { get; private set; }

    public static ErrorSummaryReadModel Create(
        Guid userId,
        string errorCode,
        string correction,
        Guid attemptId,
        DateTimeOffset now) => new()
    {
        UserId = userId,
        ErrorCode = errorCode,
        Correction = correction,
        Occurrences = 1,
        FirstSeenAt = now,
        LastSeenAt = now,
        LastAttemptId = attemptId,
    };

    public void Recur(string correction, Guid attemptId, DateTimeOffset now)
    {
        Occurrences++;
        Correction = correction;
        LastSeenAt = now;
        LastAttemptId = attemptId;
    }
}