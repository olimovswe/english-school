using System.Diagnostics;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using EnglishSchool.SystemTests.TestSupport;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Npgsql;

namespace EnglishSchool.SystemTests.Api;

[Collection(SystemFixture.CollectionName)]
public sealed class WorkerHostTests(SystemFixture system)
{
    [Fact]
    public async Task The_production_worker_delivers_outbox_events_and_stops_within_its_shutdown_timeout()
    {
        using var client = system.Api.CreateClient();
        using var registered = await client.PostAsJsonAsync("/api/v1/identity/register", new
        {
            email = $"worker-{Guid.NewGuid():N}@example.com",
            password = "correct-horse-battery-staple",
            displayName = "Worker Student",
        });
        Assert.Equal(HttpStatusCode.Created, registered.StatusCode);
        var userId = (await registered.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("userId").GetGuid();

        using var worker = system.CreateWorker(new Dictionary<string, string?> { ["Hosting:ShutdownTimeout"] = "00:00:05" }).Build();
        await worker.StartAsync();

        // UserRegistered'ning obunachisi yoʻq: Worker uni yetkazilgan deb belgilaydi.
        await Eventually.GetAsync(() => ProcessedAtAsync(userId), processedAt => processedAt is not null);

        var stopwatch = Stopwatch.StartNew();
        await worker.StopAsync();

        Assert.True(stopwatch.Elapsed < TimeSpan.FromSeconds(5), $"Worker {stopwatch.Elapsed} da toʻxtadi.");
        Assert.Equal(TimeSpan.FromSeconds(5), worker.Services.GetRequiredService<IOptions<HostOptions>>().Value.ShutdownTimeout);
    }

    private async Task<DateTimeOffset?> ProcessedAtAsync(Guid userId)
    {
        await using var connection = new NpgsqlConnection(system.PostgresConnectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(
            "SELECT processed_at FROM identity.outbox_messages WHERE content->>'userId' = @userId", connection);
        command.Parameters.AddWithValue("userId", userId.ToString());
        return await command.ExecuteScalarAsync() switch
        {
            // Npgsql timestamptz'ni UTC DateTime sifatida qaytaradi; DBNull — hali yetkazilmagan.
            DateTime utc => new DateTimeOffset(utc),
            _ => null,
        };
    }
}
