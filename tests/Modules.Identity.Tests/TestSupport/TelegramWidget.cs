using System.Security.Cryptography;
using System.Text;
using EnglishSchool.Modules.Identity.Security;

namespace EnglishSchool.Modules.Identity.Tests.TestSupport;

/// <summary>Telegram Login Widget kabi maʼlumotni bot tokeni bilan imzolaydi (hujjatdagi algoritm boʻyicha).</summary>
internal static class TelegramWidget
{
    public static long NewUserId() => 5_000_000_000L + RandomNumberGenerator.GetInt32(int.MaxValue);

    public static TelegramAuthData Sign(
        string botToken,
        long userId,
        DateTimeOffset authenticatedAt,
        string firstName = "Ali",
        string? lastName = "Valiyev",
        string? username = "ali_valiyev")
    {
        var unsigned = new TelegramAuthData(userId, firstName, lastName, username, null, authenticatedAt.ToUnixTimeSeconds(), null);

        var secretKey = SHA256.HashData(Encoding.UTF8.GetBytes(botToken));
        var hash = HMACSHA256.HashData(secretKey, Encoding.UTF8.GetBytes(TelegramLoginVerifier.BuildDataCheckString(unsigned)));

        return unsigned with { Hash = Convert.ToHexStringLower(hash) };
    }
}
