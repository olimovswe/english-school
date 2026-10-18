namespace EnglishSchool.Modules.Identity.Domain;

/// <summary>
/// Oʻquvchi akkaunti. Kirish usuli: email + parol (<see cref="Credentials"/>) yoki tasdiqlangan Telegram ID.
/// Email unikalligi <see cref="NormalizedEmail"/> boʻyicha tekshiriladi (katta-kichik harf farqsiz).
/// </summary>
internal sealed class User
{
    private User(
        Guid id,
        string? email,
        string? normalizedEmail,
        long? telegramId,
        string displayName,
        string timeZone,
        DateTimeOffset createdAt)
    {
        Id = id;
        Email = email;
        NormalizedEmail = normalizedEmail;
        TelegramId = telegramId;
        DisplayName = displayName;
        TimeZone = timeZone;
        CreatedAt = createdAt;
    }

    public Guid Id { get; private set; }

    public string? Email { get; private set; }

    public string? NormalizedEmail { get; private set; }

    public long? TelegramId { get; private set; }

    public string DisplayName { get; private set; }

    /// <summary>IANA vaqt zonasi (masalan, Asia/Tashkent): kunlik reja va eslatmalar shu boʻyicha hisoblanadi.</summary>
    public string TimeZone { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public UserCredentials? Credentials { get; private set; }

    public static User RegisterWithEmail(
        string email,
        string passwordHash,
        string displayName,
        string timeZone,
        DateTimeOffset now)
    {
        var trimmed = email.Trim();
        var user = new User(Guid.CreateVersion7(), trimmed, NormalizeEmail(trimmed), null, displayName, timeZone, now);
        user.Credentials = UserCredentials.Create(user.Id, passwordHash, now);
        return user;
    }

    public static User RegisterWithTelegram(long telegramId, string displayName, string timeZone, DateTimeOffset now) =>
        new(Guid.CreateVersion7(), null, null, telegramId, displayName, timeZone, now);

    public static string NormalizeEmail(string email) => email.Trim().ToUpperInvariant();
}
