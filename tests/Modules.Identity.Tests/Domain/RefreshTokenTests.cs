using EnglishSchool.Modules.Identity.Domain;

namespace EnglishSchool.Modules.Identity.Tests.Domain;

public sealed class RefreshTokenTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 18, 9, 0, 0, TimeSpan.Zero);
    private static readonly TimeSpan Lifetime = TimeSpan.FromDays(30);

    [Fact]
    public void Rotation_revokes_the_current_token_and_continues_the_same_family()
    {
        var current = RefreshToken.StartFamily(Guid.NewGuid(), "hash-1", Now, Lifetime);
        var rotatedAt = Now.AddDays(1);

        var next = current.Rotate("hash-2", rotatedAt, Lifetime);

        Assert.True(current.WasRotated);
        Assert.False(current.IsActive(rotatedAt));
        Assert.Equal(next.Id, current.ReplacedByTokenId);
        Assert.Equal(current.FamilyId, next.FamilyId);
        Assert.Equal(current.UserId, next.UserId);
        Assert.True(next.IsActive(rotatedAt));
        Assert.Equal(rotatedAt + Lifetime, next.ExpiresAt);
    }

    [Fact]
    public void Token_stops_being_active_when_its_lifetime_ends()
    {
        var token = RefreshToken.StartFamily(Guid.NewGuid(), "hash", Now, Lifetime);

        Assert.True(token.IsActive(Now + Lifetime - TimeSpan.FromSeconds(1)));
        Assert.False(token.IsActive(Now + Lifetime));
    }

    [Fact]
    public void Only_an_active_token_can_be_rotated()
    {
        var rotated = RefreshToken.StartFamily(Guid.NewGuid(), "hash-1", Now, Lifetime);
        rotated.Rotate("hash-2", Now, Lifetime);
        var expired = RefreshToken.StartFamily(Guid.NewGuid(), "hash-3", Now, Lifetime);

        Assert.Throws<InvalidOperationException>(() => rotated.Rotate("hash-4", Now, Lifetime));
        Assert.Throws<InvalidOperationException>(() => expired.Rotate("hash-5", Now + Lifetime, Lifetime));
    }
}
