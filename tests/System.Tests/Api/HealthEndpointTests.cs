using System.Diagnostics;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using EnglishSchool.SystemTests.TestSupport;
using Microsoft.AspNetCore.Hosting;

namespace EnglishSchool.SystemTests.Api;

[Collection(SystemFixture.CollectionName)]
public sealed class HealthEndpointTests(SystemFixture system)
{
    private static readonly string[] ModuleDatabases =
        ["identity-db", "curriculum-db", "discipline-db", "practice-db", "retention-db", "speech-db", "analytics-db"];

    [Fact]
    public async Task Liveness_answers_without_checking_any_dependency()
    {
        using var client = system.Api.CreateClient();

        var (status, report) = await GetAsync(client, "/health/live");

        Assert.Equal((HttpStatusCode.OK, "Healthy"), (status, report.GetProperty("status").GetString()));
        Assert.Equal(0, report.GetProperty("checks").GetArrayLength());
    }

    [Fact]
    public async Task Readiness_checks_every_module_database_and_its_migrations_valkey_and_shutdown()
    {
        using var client = system.Api.CreateClient();

        var (status, report) = await GetAsync(client, "/health/ready");
        var checks = Checks(report);

        Assert.Equal((HttpStatusCode.OK, "Healthy"), (status, report.GetProperty("status").GetString()));
        Assert.Equal(ModuleDatabases.Append("redis").Append("shutdown").Order(), checks.Keys.Order());
        Assert.All(checks.Values, check => Assert.Equal("Healthy", check.Status));
        Assert.All(ModuleDatabases, name => Assert.Equal("Ulanish bor, barcha migratsiyalar qoʻllangan.", checks[name].Description));
        Assert.StartsWith("Valkey ", checks["redis"].Description, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Readiness_fails_while_a_module_has_pending_migrations_but_liveness_stays_up()
    {
        var database = await system.CreateDatabaseAsync();
        await using var api = system.Api.WithWebHostBuilder(builder =>
        {
            builder.UseSetting("ConnectionStrings:Postgres", database);
            builder.UseSetting("Retention:ApplyMigrationsOnStartup", "false");
        });
        using var client = api.CreateClient();

        var (status, report) = await GetAsync(client, "/health/ready");
        var (liveness, _) = await GetAsync(client, "/health/live");

        var checks = Checks(report);
        var retention = checks["retention-db"];
        Assert.Equal((HttpStatusCode.ServiceUnavailable, "Unhealthy", "Unhealthy"), (status, report.GetProperty("status").GetString(), retention.Status));
        Assert.Equal("1 ta migratsiya qoʻllanmagan.", retention.Description);
        Assert.Equal(["20261018124610_InitialRetention"], retention.Data!.Value.GetProperty("pendingMigrations").EnumerateArray().Select(m => m.GetString()));
        Assert.All(ModuleDatabases.Where(name => name != "retention-db"), name => Assert.Equal("Healthy", checks[name].Status));
        Assert.Equal(HttpStatusCode.OK, liveness);
    }

    [Fact]
    public async Task An_unreachable_valkey_degrades_readiness_while_the_api_keeps_serving_from_memory()
    {
        await using var api = system.Api.WithWebHostBuilder(builder =>
            builder.UseSetting("ConnectionStrings:Redis", "127.0.0.1:1,connectTimeout=500"));
        using var client = api.CreateClient();

        var (status, report) = await GetAsync(client, "/health/ready");
        var stopwatch = Stopwatch.StartNew();
        using var days = await client.GetAsync(new Uri("/api/v1/curriculum/days", UriKind.Relative));
        var elapsed = stopwatch.Elapsed;

        // Degraded hali ham 200: kesh ixtiyoriy, servis trafik qabul qilishda davom etadi va L2'ni kutib qolmaydi.
        var redis = Checks(report)["redis"];
        Assert.Equal((HttpStatusCode.OK, "Degraded", "Degraded"), (status, report.GetProperty("status").GetString(), redis.Status));
        Assert.Equal("Valkey javob bermadi: kesh vaqtincha faqat jarayon xotirasida.", redis.Description);
        Assert.Equal(HttpStatusCode.OK, days.StatusCode);
        Assert.True(elapsed < TimeSpan.FromSeconds(2), $"Kesh uzilganda soʻrov {elapsed} davom etdi.");
    }

    [Fact]
    public async Task The_plain_health_endpoint_still_answers_with_a_single_word()
    {
        using var client = system.Api.CreateClient();

        Assert.Equal("Healthy", await client.GetStringAsync(new Uri("/health", UriKind.Relative)));
    }

    private static async Task<(HttpStatusCode Status, JsonElement Report)> GetAsync(HttpClient client, string path)
    {
        using var response = await client.GetAsync(new Uri(path, UriKind.Relative));
        return (response.StatusCode, await response.Content.ReadFromJsonAsync<JsonElement>());
    }

    private static Dictionary<string, Check> Checks(JsonElement report) =>
        report.GetProperty("checks").EnumerateArray().ToDictionary(
            check => check.GetProperty("name").GetString()!,
            check => new Check(
                check.GetProperty("status").GetString()!,
                check.GetProperty("description").GetString(),
                check.GetProperty("data") is { ValueKind: JsonValueKind.Object } data ? data : null));

    private sealed record Check(string Status, string? Description, JsonElement? Data);
}
