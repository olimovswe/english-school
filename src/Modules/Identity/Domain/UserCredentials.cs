namespace EnglishSchool.Modules.Identity.Domain;

/// <summary>
/// Parol xeshi va brute-force himoyasi. Faqat email bilan roʻyxatdan oʻtganlarda boʻladi.
/// Ketma-ket xato urinishlar chegaraga yetsa, akkaunt vaqtincha bloklanadi va hisoblagich nolga qaytadi.
/// </summary>
internal sealed class UserCredentials
{
    private UserCredentials(
        Guid userId,
        string passwordHash,
        int failedAttempts,
        DateTimeOffset? lockedUntil,
        DateTimeOffset updatedAt)
    {
        UserId = userId;
        PasswordHash = passwordHash;
        FailedAttempts = failedAttempts;
        LockedUntil = lockedUntil;
        UpdatedAt = updatedAt;
    }

    public Guid UserId { get; private set; }

    public string PasswordHash { get; private set; }

    public int FailedAttempts { get; private set; }

    public DateTimeOffset? LockedUntil { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; }

    public static UserCredentials Create(Guid userId, string passwordHash, DateTimeOffset now) =>
        new(userId, passwordHash, 0, null, now);

    public bool IsLockedOut(DateTimeOffset now) => LockedUntil > now;

    public void RecordFailedAttempt(DateTimeOffset now, int maxAttempts, TimeSpan lockoutDuration)
    {
        FailedAttempts++;
        if (FailedAttempts < maxAttempts)
        {
            return;
        }

        FailedAttempts = 0;
        LockedUntil = now + lockoutDuration;
    }

    public void RecordSuccessfulLogin()
    {
        FailedAttempts = 0;
        LockedUntil = null;
    }

    /// <summary>Xesh formati yoki iteratsiyalar soni eskirganda, toʻgʻri paroldan yangi xesh yoziladi.</summary>
    public void ReplaceHash(string passwordHash, DateTimeOffset now)
    {
        PasswordHash = passwordHash;
        UpdatedAt = now;
    }
}
