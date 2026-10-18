using System.Security.Cryptography;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Testcontainers.PostgreSql;

namespace EnglishSchool.Modules.Identity.Tests.Api;

/// <summary>
/// Butun API host haqiqiy PostgreSQL ustida (Testcontainers). Konteyner barcha integratsion testlar uchun
/// bitta: har bir test oʻzining unikal email yoki Telegram ID'sini ishlatadi, shuning uchun tozalash shart emas.
/// "Testing" muhiti appsettings.Development.json va developer'ning user-secrets'ini yuklamaydi.
/// </summary>
public sealed class IdentityApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    public const string CollectionName = "identity-api";

    public const string TelegramBotToken = "123456789:integration-test-bot-token";

    // docker-compose.yml bilan bir xil image: lokal va CI bazasi bir xil.
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("docker.io/pgvector/pgvector:pg18").Build();

    public Task InitializeAsync() => _postgres.StartAsync();

    async Task IAsyncLifetime.DisposeAsync()
    {
        await DisposeAsync();
        await _postgres.DisposeAsync();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("ConnectionStrings:Postgres", _postgres.GetConnectionString());
        builder.UseSetting("ConnectionStrings:Redis", string.Empty);
        builder.UseSetting("Identity:ApplyMigrationsOnStartup", "true");
        builder.UseSetting("Identity:Jwt:SigningKey", Convert.ToBase64String(RandomNumberGenerator.GetBytes(48)));
        builder.UseSetting("Identity:Telegram:BotToken", TelegramBotToken);
        builder.UseDefaultServiceProvider(options =>
        {
            options.ValidateScopes = true;
            options.ValidateOnBuild = true;
        });
    }
}

[CollectionDefinition(IdentityApiFactory.CollectionName)]
public sealed class IdentityApiCollectionDefinition : ICollectionFixture<IdentityApiFactory>;
