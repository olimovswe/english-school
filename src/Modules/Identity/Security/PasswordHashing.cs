using Microsoft.AspNetCore.Identity;

namespace EnglishSchool.Modules.Identity.Security;

internal enum PasswordCheck
{
    Failed,
    Succeeded,
    SucceededRehashNeeded,
}

/// <summary>
/// ASP.NET Core Identity'ning <see cref="PasswordHasher{TUser}"/> (PBKDF2, tuz bilan, versiyalangan format)
/// ustidagi yupqa qatlam. Modulning qolgan qismi Microsoft.AspNetCore.Identity tiplarini bilmaydi.
/// </summary>
internal static class PasswordHashing
{
    private static readonly object Subject = new();
    private static readonly PasswordHasher<object> Hasher = new();
    private static readonly string DecoyHash = Hasher.HashPassword(Subject, Guid.NewGuid().ToString());

    public static string Hash(string password) => Hasher.HashPassword(Subject, password);

    public static PasswordCheck Verify(string hash, string password) =>
        Hasher.VerifyHashedPassword(Subject, hash, password) switch
        {
            PasswordVerificationResult.Success => PasswordCheck.Succeeded,
            PasswordVerificationResult.SuccessRehashNeeded => PasswordCheck.SucceededRehashNeeded,
            _ => PasswordCheck.Failed,
        };

    /// <summary>
    /// Akkaunt topilmaganda ham bir xil ish bajariladi: javob vaqtidan email roʻyxatdan
    /// oʻtgan-oʻtmaganini bilib boʻlmaydi.
    /// </summary>
    public static void VerifyDecoy(string password) => _ = Hasher.VerifyHashedPassword(Subject, DecoyHash, password);
}
