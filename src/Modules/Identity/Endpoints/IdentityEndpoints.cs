using System.Diagnostics;
using System.Security.Claims;
using EnglishSchool.Modules.Identity.Application;
using EnglishSchool.Modules.Identity.Security;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;
using Microsoft.IdentityModel.JsonWebTokens;

namespace EnglishSchool.Modules.Identity.Endpoints;

/// <summary>
/// <c>/api/v1/identity</c>: roʻyxatdan oʻtish, login, Telegram, token yangilash, chiqish va profil.
/// Xatolar ProblemDetails koʻrinishida qaytadi; xabarlar foydalanuvchiga koʻrsatishga yaroqli.
/// </summary>
internal static class IdentityEndpoints
{
    private const string ProfileRoute = "Identity.Profile";

    public static void MapIdentityEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/identity");

        group.MapPost("/register", RegisterAsync);
        group.MapPost("/login", LoginAsync);
        group.MapPost("/telegram", SignInWithTelegramAsync);
        group.MapPost("/refresh", RefreshAsync);
        group.MapPost("/logout", LogoutAsync);
        group.MapGet("/me", GetProfileAsync).RequireAuthorization().WithName(ProfileRoute);
    }

    private static async Task<Results<CreatedAtRoute<AuthResponse>, ValidationProblem, ProblemHttpResult>> RegisterAsync(
        RegisterRequest request, AccountService accounts, CancellationToken cancellationToken)
    {
        if (AuthValidation.Validate(request) is { Count: > 0 } errors)
        {
            return TypedResults.ValidationProblem(errors);
        }

        var result = await accounts.RegisterAsync(
            request.Email!, request.Password!, request.DisplayName!, request.TimeZone, cancellationToken);

        return result.Error is { } error
            ? ToProblem(error)
            : TypedResults.CreatedAtRoute(result.Session, ProfileRoute);
    }

    private static async Task<Results<Ok<AuthResponse>, ValidationProblem, ProblemHttpResult>> LoginAsync(
        LoginRequest request, AccountService accounts, CancellationToken cancellationToken)
    {
        if (AuthValidation.Validate(request) is { Count: > 0 } errors)
        {
            return TypedResults.ValidationProblem(errors);
        }

        var result = await accounts.LoginAsync(request.Email!, request.Password!, cancellationToken);
        return result.Error is { } error ? ToProblem(error) : TypedResults.Ok(result.Session);
    }

    /// <summary>Birinchi kirishda akkaunt yaratiladi (201), keyingilarida mavjud akkauntga kiriladi (200).</summary>
    private static async Task<Results<Ok<AuthResponse>, CreatedAtRoute<AuthResponse>, ProblemHttpResult>> SignInWithTelegramAsync(
        TelegramAuthData data, AccountService accounts, CancellationToken cancellationToken)
    {
        var result = await accounts.SignInWithTelegramAsync(data, cancellationToken);
        if (result.Error is { } error)
        {
            return ToProblem(error);
        }

        return result.IsNewUser
            ? TypedResults.CreatedAtRoute(result.Session, ProfileRoute)
            : TypedResults.Ok(result.Session);
    }

    private static async Task<Results<Ok<AuthResponse>, ValidationProblem, ProblemHttpResult>> RefreshAsync(
        RefreshTokenRequest request, AccountService accounts, CancellationToken cancellationToken)
    {
        if (AuthValidation.Validate(request) is { Count: > 0 } errors)
        {
            return TypedResults.ValidationProblem(errors);
        }

        var result = await accounts.RefreshAsync(request.RefreshToken!, cancellationToken);
        return result.Error is { } error ? ToProblem(error) : TypedResults.Ok(result.Session);
    }

    private static async Task<Results<NoContent, ValidationProblem>> LogoutAsync(
        RefreshTokenRequest request, AccountService accounts, CancellationToken cancellationToken)
    {
        if (AuthValidation.Validate(request) is { Count: > 0 } errors)
        {
            return TypedResults.ValidationProblem(errors);
        }

        await accounts.LogoutAsync(request.RefreshToken!, cancellationToken);
        return TypedResults.NoContent();
    }

    private static async Task<Results<Ok<UserProfileResponse>, UnauthorizedHttpResult, NotFound>> GetProfileAsync(
        ClaimsPrincipal principal, AccountService accounts, CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(principal.FindFirst(JwtRegisteredClaimNames.Sub)?.Value, out var userId))
        {
            return TypedResults.Unauthorized();
        }

        return await accounts.GetProfileAsync(userId, cancellationToken) is { } profile
            ? TypedResults.Ok(profile)
            : TypedResults.NotFound();
    }

    private static ProblemHttpResult ToProblem(AuthError error)
    {
        var (statusCode, detail) = error switch
        {
            AuthError.InvalidCredentials => (StatusCodes.Status401Unauthorized, "Email yoki parol notoʻgʻri."),
            AuthError.LockedOut => (StatusCodes.Status429TooManyRequests, "Juda koʻp xato urinish. Birozdan soʻng qayta urinib koʻring."),
            AuthError.EmailTaken => (StatusCodes.Status409Conflict, "Bu email bilan akkaunt allaqachon mavjud."),
            AuthError.InvalidRefreshToken => (StatusCodes.Status401Unauthorized, "Sessiya yaroqsiz yoki muddati tugagan. Qayta kiring."),
            AuthError.InvalidTelegramData => (StatusCodes.Status401Unauthorized, "Telegram maʼlumotlari tasdiqlanmadi."),
            AuthError.TelegramNotConfigured => (StatusCodes.Status503ServiceUnavailable, "Telegram orqali kirish hozircha sozlanmagan."),
            _ => throw new UnreachableException(),
        };

        return TypedResults.Problem(detail: detail, statusCode: statusCode);
    }
}
