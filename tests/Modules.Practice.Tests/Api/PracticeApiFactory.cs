using System.Security.Cryptography;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Testcontainers.PostgreSql;

namespace EnglishSchool.Modules.Practice.Tests.Api;

/// <summary>
/// Butun API host haqiqiy PostgreSQL ustida. Practice kurs kunlarini Curriculum'dan oladi va token'ni
/// Identity tekshiradi, shuning uchun uchala modul ham migratsiya va seed qilinadi.
/// </summary>
public sealed class PracticeApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    public const string CollectionName = "practice-api";

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
        builder.UseSetting("Curriculum:ApplyMigrationsOnStartup", "true");
        builder.UseSetting("Curriculum:SeedOnStartup", "true");
        builder.UseSetting("Practice:ApplyMigrationsOnStartup", "true");
        builder.UseSetting("Practice:SeedOnStartup", "true");
        builder.UseDefaultServiceProvider(options =>
        {
            options.ValidateScopes = true;
            options.ValidateOnBuild = true;
        });
    }
}

[CollectionDefinition(PracticeApiFactory.CollectionName)]
public sealed class PracticeApiCollectionDefinition : ICollectionFixture<PracticeApiFactory>;
