using System.Security.Cryptography;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Testcontainers.PostgreSql;

namespace EnglishSchool.Modules.Discipline.Tests.Api;

public sealed class DisciplineApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    public const string CollectionName = "discipline-api";

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
        builder.UseSetting("Discipline:ApplyMigrationsOnStartup", "true");
        builder.UseSetting("Retention:ApplyMigrationsOnStartup", "true");
        builder.UseDefaultServiceProvider(options =>
        {
            options.ValidateScopes = true;
            options.ValidateOnBuild = true;
        });
    }
}

[CollectionDefinition(DisciplineApiFactory.CollectionName)]
public sealed class DisciplineApiCollectionDefinition : ICollectionFixture<DisciplineApiFactory>;