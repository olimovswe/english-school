using System.Security.Cryptography;
using System.Text;

namespace EnglishSchool.Modules.Speech.Application;

internal static class Hashing
{
    public static string Sha256Hex(ReadOnlySpan<byte> content) => Convert.ToHexStringLower(SHA256.HashData(content));

    public static string Sha256Hex(string text) => Sha256Hex(Encoding.UTF8.GetBytes(text));
}
