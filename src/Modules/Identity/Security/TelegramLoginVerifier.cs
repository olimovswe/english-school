using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;

namespace EnglishSchool.Modules.Identity.Security;

/// <summary>Telegram Login Widget qaytaradigan maʼlumot: https://core.telegram.org/widgets/login</summary>
internal sealed record TelegramAuthData(
    [property: JsonPropertyName("id")] long Id,
    [property: JsonPropertyName("first_name")] string? FirstName,
    [property: JsonPropertyName("last_name")] string? LastName,
    [property: JsonPropertyName("username")] string? Username,
    [property: JsonPropertyName("photo_url")] string? PhotoUrl,
    [property: JsonPropertyName("auth_date")] long AuthDate,
    [property: JsonPropertyName("hash")] string? Hash);

/// <summary>
/// Widget maʼlumotining haqiqiyligini tekshiradi: hash = HMAC-SHA256(data-check-string, SHA256(bot token)).
/// Imzosiz Telegram ID hech qachon qabul qilinmaydi: aks holda istalgan kishi boshqaning akkauntiga kirardi.
/// </summary>
internal sealed class TelegramLoginVerifier(IOptions<TelegramLoginOptions> options)
{
    private static readonly TimeSpan AllowedClockSkew = TimeSpan.FromMinutes(1);

    public bool IsConfigured => !string.IsNullOrWhiteSpace(options.Value.BotToken);

    public bool Verify(TelegramAuthData data, DateTimeOffset now)
    {
        var settings = options.Value;
        if (!IsConfigured || data.Hash is not { Length: 64 } hash || data.AuthDate <= 0)
        {
            return false;
        }

        var ageSeconds = now.ToUnixTimeSeconds() - data.AuthDate;
        if (ageSeconds > settings.MaxAuthAge.TotalSeconds || ageSeconds < -AllowedClockSkew.TotalSeconds)
        {
            return false;
        }

        byte[] expected;
        try
        {
            expected = Convert.FromHexString(hash);
        }
        catch (FormatException)
        {
            return false;
        }

        var secretKey = SHA256.HashData(Encoding.UTF8.GetBytes(settings.BotToken));
        var actual = HMACSHA256.HashData(secretKey, Encoding.UTF8.GetBytes(BuildDataCheckString(data)));
        return CryptographicOperations.FixedTimeEquals(actual, expected);
    }

    /// <summary><c>hash</c>'dan boshqa barcha kelgan maydonlar, kalit boʻyicha tartiblangan "kalit=qiymat" qatorlari.</summary>
    internal static string BuildDataCheckString(TelegramAuthData data)
    {
        var fields = new SortedDictionary<string, string>(StringComparer.Ordinal)
        {
            ["auth_date"] = data.AuthDate.ToString(CultureInfo.InvariantCulture),
            ["id"] = data.Id.ToString(CultureInfo.InvariantCulture),
        };

        AddIfPresent("first_name", data.FirstName);
        AddIfPresent("last_name", data.LastName);
        AddIfPresent("username", data.Username);
        AddIfPresent("photo_url", data.PhotoUrl);

        return string.Join('\n', fields.Select(field => $"{field.Key}={field.Value}"));

        void AddIfPresent(string key, string? value)
        {
            if (value is not null)
            {
                fields[key] = value;
            }
        }
    }
}
