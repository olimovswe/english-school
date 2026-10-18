using System.Net.Mail;

namespace EnglishSchool.Modules.Identity.Application;

/// <summary>Kiruvchi soʻrovlarni tekshiradi. Kalitlar JSON maydon nomlari bilan bir xil (camelCase).</summary>
internal static class AuthValidation
{
    public const int MinPasswordLength = 8;
    public const int MaxPasswordLength = 128;
    public const int MaxEmailLength = 254;
    public const int MaxDisplayNameLength = 100;
    private const int MaxRefreshTokenLength = 256;

    public static Dictionary<string, string[]> Validate(RegisterRequest request)
    {
        var errors = new Dictionary<string, string[]>();

        if (!IsValidEmail(request.Email))
        {
            errors["email"] = ["Email manzili notoʻgʻri."];
        }

        if (request.Password is not { Length: >= MinPasswordLength and <= MaxPasswordLength })
        {
            errors["password"] = [$"Parol {MinPasswordLength}–{MaxPasswordLength} belgidan iborat boʻlishi kerak."];
        }

        if (request.DisplayName?.Trim() is not { Length: > 0 and <= MaxDisplayNameLength })
        {
            errors["displayName"] = [$"Ism boʻsh boʻlmasligi va {MaxDisplayNameLength} belgidan oshmasligi kerak."];
        }

        if (request.TimeZone is { } timeZone && !TimeZoneInfo.TryFindSystemTimeZoneById(timeZone, out _))
        {
            errors["timeZone"] = ["Vaqt zonasi notoʻgʻri (masalan: Asia/Tashkent)."];
        }

        return errors;
    }

    public static Dictionary<string, string[]> Validate(LoginRequest request)
    {
        var errors = new Dictionary<string, string[]>();

        if (!IsValidEmail(request.Email))
        {
            errors["email"] = ["Email manzili notoʻgʻri."];
        }

        if (request.Password is not { Length: > 0 and <= MaxPasswordLength })
        {
            errors["password"] = ["Parol kiritilmagan."];
        }

        return errors;
    }

    public static Dictionary<string, string[]> Validate(RefreshTokenRequest request)
    {
        var errors = new Dictionary<string, string[]>();

        if (request.RefreshToken is not { Length: > 0 and <= MaxRefreshTokenLength })
        {
            errors["refreshToken"] = ["Refresh token kiritilmagan."];
        }

        return errors;
    }

    private static bool IsValidEmail(string? email)
    {
        var trimmed = email?.Trim();
        return trimmed is { Length: > 0 and <= MaxEmailLength }
            && MailAddress.TryCreate(trimmed, out var address)
            && string.Equals(address.Address, trimmed, StringComparison.Ordinal);
    }
}
