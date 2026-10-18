namespace EnglishSchool.Modules.Identity.Domain;

/// <summary>
/// Refresh token: bazada faqat SHA-256 xeshi saqlanadi. Har safar ishlatilganda almashtiriladi (rotation),
/// bitta login sessiyasidagi tokenlar umumiy <see cref="FamilyId"/> ga ega. Almashtirilgan token qayta
/// kelsa, u oʻgʻirlangan boʻlishi mumkin: butun oila (sessiya) bekor qilinadi.
/// </summary>
internal sealed class RefreshToken
{
    private RefreshToken(
        Guid id,
        Guid userId,
        Guid familyId,
        string tokenHash,
        DateTimeOffset createdAt,
        DateTimeOffset expiresAt,
        DateTimeOffset? revokedAt,
        Guid? replacedByTokenId)
    {
        Id = id;
        UserId = userId;
        FamilyId = familyId;
        TokenHash = tokenHash;
        CreatedAt = createdAt;
        ExpiresAt = expiresAt;
        RevokedAt = revokedAt;
        ReplacedByTokenId = replacedByTokenId;
    }

    public Guid Id { get; private set; }

    public Guid UserId { get; private set; }

    public Guid FamilyId { get; private set; }

    public string TokenHash { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset ExpiresAt { get; private set; }

    public DateTimeOffset? RevokedAt { get; private set; }

    public Guid? ReplacedByTokenId { get; private set; }

    public bool IsRevoked => RevokedAt is not null;

    /// <summary>Token almashtirilgan (bekor qilingan emas, balki allaqachon ishlatilgan).</summary>
    public bool WasRotated => ReplacedByTokenId is not null;

    public bool IsActive(DateTimeOffset now) => RevokedAt is null && ExpiresAt > now;

    public static RefreshToken StartFamily(Guid userId, string tokenHash, DateTimeOffset now, TimeSpan lifetime) =>
        new(Guid.CreateVersion7(), userId, Guid.CreateVersion7(), tokenHash, now, now + lifetime, null, null);

    public RefreshToken Rotate(string nextTokenHash, DateTimeOffset now, TimeSpan lifetime)
    {
        if (!IsActive(now))
        {
            throw new InvalidOperationException("Faqat faol refresh tokenni almashtirish mumkin.");
        }

        var next = new RefreshToken(
            Guid.CreateVersion7(), UserId, FamilyId, nextTokenHash, now, now + lifetime, null, null);

        RevokedAt = now;
        ReplacedByTokenId = next.Id;
        return next;
    }
}
