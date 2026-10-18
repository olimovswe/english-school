using System.Security.Cryptography;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Testcontainers.PostgreSql;

namespace EnglishSchool.Modules.Analytics.Tests.Api;

public sealed class AnalyticsApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    public const string CollectionName = "analytics-api";

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
        builder.UseSetting("Discipline:ApplyMigrationsOnStartup", "true");
        builder.UseSetting("Retention:ApplyMigrationsOnStartup", "true");
        builder.UseSetting("Analytics:ApplyMigrationsOnStartup", "true");
        builder.UseDefaultServiceProvider(options =>
        {
            options.ValidateScopes = true;
            options.ValidateOnBuild = true;
        });
    }
}

[CollectionDefinition(AnalyticsApiFactory.CollectionName)]
public sealed class AnalyticsApiCollectionDefinition : ICollectionFixture<AnalyticsApiFactory>;