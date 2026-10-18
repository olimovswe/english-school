using EnglishSchool.Modules.Identity.Domain;
using EnglishSchool.Modules.Identity.Infrastructure;
using EnglishSchool.Modules.Identity.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Npgsql;

namespace EnglishSchool.Modules.Identity.Application;

/// <summary>
/// Roʻyxatdan oʻtish, login, token yangilash va chiqish. Har bir muvaffaqiyatli kirish yangi sessiya
/// (refresh token oilasi) ochadi. Access token qisqa muddatli va bazaga yozilmaydi.
/// </summary>
internal sealed partial class AccountService(
    IdentityDbContext db,
    TokenIssuer tokens,
    TelegramLoginVerifier telegram,
    IOptions<IdentityModuleOptions> options,
    TimeProvider clock,
    ILogger<AccountService> logger)
{
    public async Task<AuthResult> RegisterAsync(
        string email,
        string password,
        string displayName,
        string? timeZone,
        CancellationToken cancellationToken)
    {
        var normalizedEmail = User.NormalizeEmail(email);
        if (await db.Users.AnyAsync(u => u.NormalizedEmail == normalizedEmail, cancellationToken))
        {
            return AuthResult.Failure(AuthError.EmailTaken);
        }

        var now = clock.GetUtcNow();
        var user = User.RegisterWithEmail(
            email, PasswordHashing.Hash(password), displayName.Trim(), timeZone ?? options.Value.DefaultTimeZone, now);
        db.Users.Add(user);
        var session = StartSession(user, now);

        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (IsUniqueViolation(exception))
        {
            // Bir xil email bilan parallel soʻrov tekshiruvdan oʻtib ketgan: unikal indeks ikkinchisini toʻxtatdi.
            return AuthResult.Failure(AuthError.EmailTaken);
        }

        LogRegistered(logger, user.Id, "email");
        return AuthResult.Success(session, isNewUser: true);
    }

    public async Task<AuthResult> LoginAsync(string email, string password, CancellationToken cancellationToken)
    {
        var normalizedEmail = User.NormalizeEmail(email);
        var user = await db.Users.SingleOrDefaultAsync(u => u.NormalizedEmail == normalizedEmail, cancellationToken);
        if (user is null)
        {
            PasswordHashing.VerifyDecoy(password);
            return AuthResult.Failure(AuthError.InvalidCredentials);
        }

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        var credentials = await db.LockCredentialsAsync(user.Id, cancellationToken);
        if (credentials is null)
        {
            PasswordHashing.VerifyDecoy(password);
            return AuthResult.Failure(AuthError.InvalidCredentials);
        }

        var now = clock.GetUtcNow();
        if (credentials.IsLockedOut(now))
        {
            return AuthResult.Failure(AuthError.LockedOut);
        }

        var check = PasswordHashing.Verify(credentials.PasswordHash, password);
        if (check == PasswordCheck.Failed)
        {
            var settings = options.Value;
            credentials.RecordFailedAttempt(now, settings.MaxFailedLoginAttempts, settings.LockoutDuration);
            await db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            if (credentials.IsLockedOut(now))
            {
                LogLockedOut(logger, user.Id);
            }

            return AuthResult.Failure(AuthError.InvalidCredentials);
        }

        if (check == PasswordCheck.SucceededRehashNeeded)
        {
            credentials.ReplaceHash(PasswordHashing.Hash(password), now);
        }

        credentials.RecordSuccessfulLogin();
        var session = StartSession(user, now);
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return AuthResult.Success(session);
    }

    public async Task<AuthResult> RefreshAsync(string refreshToken, CancellationToken cancellationToken)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        var current = await db.LockRefreshTokenAsync(TokenIssuer.HashRefreshToken(refreshToken), cancellationToken);
        if (current is null)
        {
            return AuthResult.Failure(AuthError.InvalidRefreshToken);
        }

        var now = clock.GetUtcNow();
        if (current.WasRotated)
        {
            // Allaqachon almashtirilgan token qayta keldi: u oʻgʻirlangan boʻlishi mumkin, sessiya yopiladi.
            await RevokeFamilyAsync(current.FamilyId, now, cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            LogRefreshTokenReuse(logger, current.UserId, current.FamilyId);
            return AuthResult.Failure(AuthError.InvalidRefreshToken);
        }

        if (!current.IsActive(now))
        {
            return AuthResult.Failure(AuthError.InvalidRefreshToken);
        }

        var user = await db.Users.SingleAsync(u => u.Id == current.UserId, cancellationToken);
        var (value, next) = tokens.RotateRefreshToken(current, now);
        db.RefreshTokens.Add(next);
        var session = CreateResponse(user, value, next, now);

        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return AuthResult.Success(session);
    }

    /// <summary>Sessiyani (token oilasini) yopadi. Nomaʼlum token xato emas: chiqish idempotent.</summary>
    public async Task LogoutAsync(string refreshToken, CancellationToken cancellationToken)
    {
        var tokenHash = TokenIssuer.HashRefreshToken(refreshToken);
        var familyId = await db.RefreshTokens
            .Where(t => t.TokenHash == tokenHash)
            .Select(t => (Guid?)t.FamilyId)
            .SingleOrDefaultAsync(cancellationToken);

        if (familyId is { } family)
        {
            await RevokeFamilyAsync(family, clock.GetUtcNow(), cancellationToken);
        }
    }

    public async Task<AuthResult> SignInWithTelegramAsync(TelegramAuthData data, CancellationToken cancellationToken)
    {
        if (!telegram.IsConfigured)
        {
            return AuthResult.Failure(AuthError.TelegramNotConfigured);
        }

        var now = clock.GetUtcNow();
        if (!telegram.Verify(data, now))
        {
            return AuthResult.Failure(AuthError.InvalidTelegramData);
        }

        var user = await db.Users.SingleOrDefaultAsync(u => u.TelegramId == data.Id, cancellationToken);
        var isNewUser = user is null;
        if (user is null)
        {
            user = User.RegisterWithTelegram(data.Id, TelegramDisplayName(data), options.Value.DefaultTimeZone, now);
            db.Users.Add(user);
        }

        var session = StartSession(user, now);

        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (isNewUser && IsUniqueViolation(exception))
        {
            // Birinchi kirish ikki marta bosilgan: akkauntni parallel soʻrov yaratdi, endi u bilan kiramiz.
            db.ChangeTracker.Clear();
            return await SignInWithTelegramAsync(data, cancellationToken);
        }

        if (isNewUser)
        {
            LogRegistered(logger, user.Id, "telegram");
        }

        return AuthResult.Success(session, isNewUser);
    }

    public Task<UserProfileResponse?> GetProfileAsync(Guid userId, CancellationToken cancellationToken) =>
        db.Users
            .AsNoTracking()
            .Where(u => u.Id == userId)
            .Select(u => new UserProfileResponse(u.Id, u.Email, u.DisplayName, u.TimeZone, u.TelegramId, u.CreatedAt))
            .SingleOrDefaultAsync(cancellationToken);

    private AuthResponse StartSession(User user, DateTimeOffset now)
    {
        var (value, refreshToken) = tokens.StartRefreshTokenFamily(user.Id, now);
        db.RefreshTokens.Add(refreshToken);
        return CreateResponse(user, value, refreshToken, now);
    }

    private AuthResponse CreateResponse(User user, string refreshTokenValue, RefreshToken refreshToken, DateTimeOffset now)
    {
        var accessToken = tokens.CreateAccessToken(user, now);
        return new AuthResponse(user.Id, accessToken.Value, accessToken.ExpiresAt, refreshTokenValue, refreshToken.ExpiresAt);
    }

    private Task<int> RevokeFamilyAsync(Guid familyId, DateTimeOffset now, CancellationToken cancellationToken) =>
        db.RefreshTokens
            .Where(t => t.FamilyId == familyId && t.RevokedAt == null)
            .ExecuteUpdateAsync(setters => setters.SetProperty(t => t.RevokedAt, now), cancellationToken);

    private static string TelegramDisplayName(TelegramAuthData data)
    {
        var name = string.Join(' ', new[] { data.FirstName, data.LastName }.Where(part => !string.IsNullOrWhiteSpace(part))).Trim();
        if (name.Length == 0)
        {
            name = data.Username ?? "Telegram";
        }

        return name.Length <= AuthValidation.MaxDisplayNameLength ? name : name[..AuthValidation.MaxDisplayNameLength];
    }

    private static bool IsUniqueViolation(DbUpdateException exception) =>
        exception.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation };

    [LoggerMessage(Level = LogLevel.Information, Message = "Yangi foydalanuvchi {UserId} roʻyxatdan oʻtdi ({Method}).")]
    private static partial void LogRegistered(ILogger logger, Guid userId, string method);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Foydalanuvchi {UserId} ketma-ket xato parollar sababli vaqtincha bloklandi.")]
    private static partial void LogLockedOut(ILogger logger, Guid userId);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Almashtirilgan refresh token qayta ishlatildi: foydalanuvchi {UserId}, sessiya {FamilyId} yopildi.")]
    private static partial void LogRefreshTokenReuse(ILogger logger, Guid userId, Guid familyId);
}
