using System.Security.Claims;

namespace EnglishSchool.BuildingBlocks.Security;

public static class ClaimsPrincipalExtensions
{
    /// <summary>
    /// Identity moduli chiqargan access tokendagi <c>sub</c> claim. Inbound claim mapping oʻchirilgan,
    /// shuning uchun claim nomi oʻzgarmaydi.
    /// </summary>
    public static bool TryGetUserId(this ClaimsPrincipal principal, out Guid userId)
    {
        ArgumentNullException.ThrowIfNull(principal);
        return Guid.TryParse(principal.FindFirst("sub")?.Value, out userId);
    }
}
