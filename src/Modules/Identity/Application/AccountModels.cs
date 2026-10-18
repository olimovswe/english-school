namespace EnglishSchool.Modules.Identity.Application;

internal sealed record RegisterRequest(string? Email, string? Password, string? DisplayName, string? TimeZone);

internal sealed record LoginRequest(string? Email, string? Password);

internal sealed record RefreshTokenRequest(string? RefreshToken);

internal sealed record AuthResponse(
    Guid UserId,
    string AccessToken,
    DateTimeOffset AccessTokenExpiresAt,
    string RefreshToken,
    DateTimeOffset RefreshTokenExpiresAt);

internal sealed record UserProfileResponse(
    Guid Id,
    string? Email,
    string DisplayName,
    string TimeZone,
    long? TelegramId,
    DateTimeOffset CreatedAt);

internal enum AuthError
{
    InvalidCredentials,
    LockedOut,
    EmailTaken,
    InvalidRefreshToken,
    InvalidTelegramData,
    TelegramNotConfigured,
}

/// <summary>Muvaffaqiyatda <see cref="Session"/>, aks holda <see cref="Error"/> toʻldirilgan boʻladi.</summary>
internal sealed record AuthResult(AuthResponse? Session, AuthError? Error, bool IsNewUser)
{
    public static AuthResult Success(AuthResponse session, bool isNewUser = false) => new(session, null, isNewUser);

    public static AuthResult Failure(AuthError error) => new(null, error, false);
}
