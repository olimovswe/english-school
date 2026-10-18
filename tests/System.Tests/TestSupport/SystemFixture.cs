extern alias worker;

using System.Security.Cryptography;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Npgsql;
using Testcontainers.PostgreSql;
using Testcontainers.Redis;
using WorkerHost = worker::EnglishSchool.Host.Worker.WorkerHost;

namespace EnglishSchool.SystemTests.TestSupport;

/// <summary>
/// Butun tizim uchun bitta muhit: PostgreSQL (pgvector) va Valkey konteynerlari hamda barcha modullari migratsiya va
/// seed qilingan API host. Worker kerak boʻlgan test uni <see cref="CreateWorker"/> bilan oʻzi ishga tushiradi.
/// Testlar bir-biridan unikal foydalanuvchilar bilan ajraladi.
/// </summary>
public sealed class SystemFixture : IAsyncLifetime
{
    public const string CollectionName = "system";

    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("docker.io/pgvector/pgvector:pg18").Build();
    private readonly RedisContainer _valkey = new RedisBuilder("docker.io/valkey/valkey:8-alpine").Build();

    public string PostgresConnectionString => _postgres.GetConnectionString();

    public string ValkeyConnectionString => _valkey.GetConnectionString();

    /// <summary>Bitta kalit: WithWebHostBuilder bilan yasalgan API hostlari ham shu tokenlarni qabul qiladi.</summary>
    public string SigningKey { get; } = Convert.ToBase64String(RandomNumberGenerator.GetBytes(48));

    public ApiFactory Api { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        await Task.WhenAll(_postgres.StartAsync(), _valkey.StartAsync());

        // Host birinchi murojaatda ishga tushadi va barcha modullarni migratsiya hamda seed qiladi.
        Api = new ApiFactory(this);
        _ = Api.Services;
    }

    public async Task DisposeAsync()
    {
        await Api.DisposeAsync();
        await _valkey.DisposeAsync();
        await _postgres.DisposeAsync();
    }

    /// <summary>Productiondagi Worker yigʻishi (<see cref="WorkerHost"/>), shu konteynerlarga ulangan.</summary>
    public HostApplicationBuilder CreateWorker(IReadOnlyDictionary<string, string?>? overrides = null)
    {
        var settings = new Dictionary<string, string?>
        {
            ["ConnectionStrings:Postgres"] = PostgresConnectionString,
            ["ConnectionStrings:Redis"] = ValkeyConnectionString,
            ["Outbox:PollingInterval"] = "00:00:00.100",
            ["Outbox:BatchSize"] = "1000",
        };
        foreach (var (key, value) in overrides ?? new Dictionary<string, string?>())
        {
            settings[key] = value;
        }

        return WorkerHost.CreateBuilder(
            new HostApplicationBuilderSettings { EnvironmentName = "Testing", ContentRootPath = AppContext.BaseDirectory },
            settings);
    }

    /// <summary>Shu konteynerda yangi, boʻsh baza: migratsiya qilinmagan holatlarni tekshirish uchun.</summary>
    public async Task<string> CreateDatabaseAsync()
    {
        var name = $"system_{Guid.NewGuid():N}";
        var result = await _postgres.ExecScriptAsync($"CREATE DATABASE {name};");
        Assert.True(result.ExitCode == 0, result.Stderr);
        return new NpgsqlConnectionStringBuilder(PostgresConnectionString) { Database = name }.ConnectionString;
    }
}

[CollectionDefinition(SystemFixture.CollectionName)]
public sealed class SystemCollectionDefinition : ICollectionFixture<SystemFixture>;
