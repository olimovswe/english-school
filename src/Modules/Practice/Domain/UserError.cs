namespace EnglishSchool.Modules.Practice.Domain;

/// <summary>
/// Oʻquvchining takrorlanayotgan xatosi: (foydalanuvchi, xato kodi) boʻyicha bitta qator. Keyinchalik shaxsiy
/// mashqlar va Analytics shu hisoblagichdan foydalanadi.
/// </summary>
internal sealed class UserError
{
    private UserError()
    {
    }

    public Guid Id { get; private init; } = Guid.CreateVersion7();

    public Guid UserId { get; private init; }

    public string ErrorCode { get; private init; } = string.Empty;

    public int Occurrences { get; private set; }

    public DateTimeOffset FirstSeenAt { get; private init; }

    public DateTimeOffset LastSeenAt { get; private set; }

    public Guid LastAttemptId { get; private set; }

    public static UserError FirstSeen(Guid userId, string errorCode, Guid attemptId, DateTimeOffset now) => new()
    {
        UserId = userId,
        ErrorCode = errorCode,
        Occurrences = 1,
        FirstSeenAt = now,
        LastSeenAt = now,
        LastAttemptId = attemptId,
    };

    public void Recur(Guid attemptId, DateTimeOffset now)
    {
        Occurrences++;
        LastSeenAt = now;
        LastAttemptId = attemptId;
    }
}
