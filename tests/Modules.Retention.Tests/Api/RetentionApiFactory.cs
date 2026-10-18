using System.Security.Cryptography;
using EnglishSchool.BuildingBlocks.Caching;
using EnglishSchool.BuildingBlocks.Modules;
using EnglishSchool.BuildingBlocks.Outbox;
using EnglishSchool.Modules.Curriculum;
using EnglishSchool.Modules.Practice;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;

namespace EnglishSchool.Modules.Retention.Tests.Api;

/// <summary>
/// Butun API host haqiqiy PostgreSQL ustida. Kartochkalar Curriculum lugʻatidan, Practice eventlari orqali
/// ochiladi, shuning uchun Identity, Curriculum, Practice va Retention migratsiya va seed qilinadi.
/// </summary>
public sealed class RetentionApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    public const string CollectionName = "retention-api";

    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("docker.io/pgvector/pgvector:pg18").Build();

    public Task InitializeAsync() => _postgres.StartAsync();

    async Task IAsyncLifetime.DisposeAsync()
    {
        await DisposeAsync();
        await _postgres.DisposeAsync();
    }

    /// <summary>
    /// Worker kabi yigʻilgan DI: outbox dispatcher va eventni chiqaradigan (Practice) hamda qabul qiladigan
    /// (Retention, Curriculum lugʻati bilan) modullar. Boshqa testlarning eventlari ham bitta partiyada olinadi.
    /// </summary>
    internal ServiceProvider BuildWorker()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Postgres"] = _postgres.GetConnectionString(),
                ["Outbox:BatchSize"] = "1000",
            })
            .Build();

        var services = new ServiceCollection().AddLogging();
        services.AddHybridCaching(configuration);
        foreach (IModule module in new IModule[] { new CurriculumModule(), new PracticeModule(), new RetentionModule() })
        {
            module.AddServices(services, configuration);
        }

        services.AddOutboxDispatcher(configuration);
        return services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true, ValidateOnBuild = true });
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
        builder.UseSetting("Retention:ApplyMigrationsOnStartup", "true");
        builder.UseDefaultServiceProvider(options =>
        {
            options.ValidateScopes = true;
            options.ValidateOnBuild = true;
        });
    }
}

[CollectionDefinition(RetentionApiFactory.CollectionName)]
public sealed class RetentionApiCollectionDefinition : ICollectionFixture<RetentionApiFactory>;
