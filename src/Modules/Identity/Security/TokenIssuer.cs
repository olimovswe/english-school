using System.Buffers.Text;
using System.Security.Cryptography;
using System.Text;
using EnglishSchool.Modules.Identity.Domain;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace EnglishSchool.Modules.Identity.Security;

internal sealed record AccessToken(string Value, DateTimeOffset ExpiresAt);

/// <summary>
/// Qisqa muddatli JWT access token va tasodifiy refresh token chiqaradi. Refresh tokenning qiymati
/// faqat mijozga beriladi, bazaga uning SHA-256 xeshi yoziladi.
/// </summary>
internal sealed class TokenIssuer(IOptions<JwtOptions> options)
{
    private static readonly JsonWebTokenHandler Handler = new();

    public AccessToken CreateAccessToken(User user, DateTimeOffset now)
    {
        var jwt = options.Value;
        var expiresAt = now + jwt.AccessTokenLifetime;

        var token = Handler.CreateToken(new SecurityTokenDescriptor
        {
            Issuer = jwt.Issuer,
            Audience = jwt.Audience,
            IssuedAt = now.UtcDateTime,
            NotBefore = now.UtcDateTime,
            Expires = expiresAt.UtcDateTime,
            Claims = new Dictionary<string, object>
            {
                [JwtRegisteredClaimNames.Sub] = user.Id.ToString(),
                [JwtRegisteredClaimNames.Name] = user.DisplayName,
                [JwtRegisteredClaimNames.Jti] = Guid.NewGuid().ToString(),
            },
            SigningCredentials = new SigningCredentials(jwt.CreateSigningKey(), SecurityAlgorithms.HmacSha256),
        });

        return new AccessToken(token, expiresAt);
    }

    public (string Value, RefreshToken Token) StartRefreshTokenFamily(Guid userId, DateTimeOffset now)
    {
        var value = NewRefreshTokenValue();
        return (value, RefreshToken.StartFamily(userId, HashRefreshToken(value), now, options.Value.RefreshTokenLifetime));
    }

    public (string Value, RefreshToken Token) RotateRefreshToken(RefreshToken current, DateTimeOffset now)
    {
        var value = NewRefreshTokenValue();
        return (value, current.Rotate(HashRefreshToken(value), now, options.Value.RefreshTokenLifetime));
    }

    public static string HashRefreshToken(string value) =>
        Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(value)));

    private static string NewRefreshTokenValue() => Base64Url.EncodeToString(RandomNumberGenerator.GetBytes(32));
}
