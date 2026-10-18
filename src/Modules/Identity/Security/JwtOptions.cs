using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace EnglishSchool.Modules.Identity.Security;

internal sealed class JwtOptions
{
    public const string Section = "Identity:Jwt";

    /// <summary>HS256 talabi: kalit kamida 256 bit.</summary>
    public const int MinSigningKeyBytes = 32;

    public string Issuer { get; set; } = "english-school";

    public string Audience { get; set; } = "english-school";

    /// <summary>Repoda saqlanmaydi: lokal muhitda user-secrets, productionda muhit oʻzgaruvchisi yoki secret store.</summary>
    public string SigningKey { get; set; } = string.Empty;

    public TimeSpan AccessTokenLifetime { get; set; } = TimeSpan.FromMinutes(15);

    public TimeSpan RefreshTokenLifetime { get; set; } = TimeSpan.FromDays(30);

    public SymmetricSecurityKey CreateSigningKey() => new(Encoding.UTF8.GetBytes(SigningKey));
}
