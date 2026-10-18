using System.Text;
using EnglishSchool.BuildingBlocks.Modules;
using EnglishSchool.Modules.Identity.Application;
using EnglishSchool.Modules.Identity.Endpoints;
using EnglishSchool.Modules.Identity.Infrastructure;
using EnglishSchool.Modules.Identity.Security;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace EnglishSchool.Modules.Identity;

/// <summary>
/// Identity moduli: roʻyxatdan oʻtish (email + parol yoki Telegram), login, JWT access token va
/// almashinadigan refresh token. Autentifikatsiya sxemasi shu yerda: boshqa modullar endpointlarini
/// faqat <c>RequireAuthorization()</c> bilan himoyalaydi va <c>sub</c> claim'dan UserId oladi.
/// </summary>
public sealed class IdentityModule : IModule
{
    public string Name => "Identity";

    public void AddServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<IdentityModuleOptions>()
            .Bind(configuration.GetSection(IdentityModuleOptions.Section))
            .Validate(o => o.MaxFailedLoginAttempts > 0 && o.LockoutDuration > TimeSpan.Zero,
                "Identity:MaxFailedLoginAttempts va LockoutDuration musbat boʻlishi kerak.")
            .Validate(o => TimeZoneInfo.TryFindSystemTimeZoneById(o.DefaultTimeZone, out _),
                "Identity:DefaultTimeZone tizimda topilmadi (masalan: Asia/Tashkent).");

        services.AddOptions<JwtOptions>()
            .Bind(configuration.GetSection(JwtOptions.Section))
            .Validate(o => Encoding.UTF8.GetByteCount(o.SigningKey) >= JwtOptions.MinSigningKeyBytes,
                "Identity:Jwt:SigningKey kamida 32 bayt boʻlishi kerak (lokal: scripts/dev-secrets.sh).")
            .Validate(o => !string.IsNullOrWhiteSpace(o.Issuer) && !string.IsNullOrWhiteSpace(o.Audience),
                "Identity:Jwt:Issuer va Identity:Jwt:Audience boʻsh boʻlmasligi kerak.")
            .Validate(o => o.AccessTokenLifetime > TimeSpan.Zero && o.RefreshTokenLifetime > o.AccessTokenLifetime,
                "Identity:Jwt: token muddatlari musbat, refresh token esa access tokendan uzoq boʻlishi kerak.");

        services.AddOptions<TelegramLoginOptions>().Bind(configuration.GetSection(TelegramLoginOptions.Section));

        services.AddDbContext<IdentityDbContext>(options => options
            .UseNpgsql(
                configuration.GetConnectionString("Postgres"),
                npgsql => npgsql.MigrationsHistoryTable("__ef_migrations_history", IdentityDbContext.Schema))
            .UseSnakeCaseNamingConvention());

        services.TryAddSingleton(TimeProvider.System);
        services.AddSingleton<TokenIssuer>();
        services.AddSingleton<TelegramLoginVerifier>();
        services.AddScoped<AccountService>();
        services.AddHostedService<IdentityDatabaseMigrator>();
        services.AddHealthChecks().AddDbContextCheck<IdentityDbContext>("identity-db");

        // Authorization (policy, routing'ga bogʻliq) API host'da qoʻshiladi: Worker'da routing yoʻq.
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer();
        services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
            .Configure<IOptions<JwtOptions>>((bearer, jwt) =>
            {
                bearer.MapInboundClaims = false;
                bearer.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidIssuer = jwt.Value.Issuer,
                    ValidAudience = jwt.Value.Audience,
                    IssuerSigningKey = jwt.Value.CreateSigningKey(),
                    ValidAlgorithms = [SecurityAlgorithms.HmacSha256],
                    ClockSkew = TimeSpan.FromSeconds(30),
                    NameClaimType = JwtRegisteredClaimNames.Name,
                };
            });
    }

    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        // Token faqat API'da chiqariladi va tekshiriladi: JWT sozlamalari xato boʻlsa, API ishga tushmaydi.
        // Worker bu kalitni talab qilmaydi.
        _ = endpoints.ServiceProvider.GetRequiredService<IOptions<JwtOptions>>().Value;

        endpoints.MapIdentityEndpoints();
    }
}
