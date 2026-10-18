using EnglishSchool.BuildingBlocks.Health;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;

namespace EnglishSchool.SystemTests.Hosting;

public sealed class ShutdownHealthCheckTests
{
    [Fact]
    public async Task Readiness_is_withdrawn_as_soon_as_shutdown_begins()
    {
        using var lifetime = new TestLifetime();
        var check = new ShutdownHealthCheck(lifetime);
        var context = new HealthCheckContext { Registration = new HealthCheckRegistration("shutdown", check, HealthStatus.Unhealthy, tags: null) };

        var running = await check.CheckHealthAsync(context);
        lifetime.StopApplication();
        var stopping = await check.CheckHealthAsync(context);

        Assert.Equal(HealthStatus.Healthy, running.Status);
        Assert.Equal((HealthStatus.Unhealthy, "Server toʻxtamoqda."), (stopping.Status, stopping.Description));
    }

    private sealed class TestLifetime : IHostApplicationLifetime, IDisposable
    {
        private readonly CancellationTokenSource _stopping = new();

        public CancellationToken ApplicationStarted => CancellationToken.None;

        public CancellationToken ApplicationStopping => _stopping.Token;

        public CancellationToken ApplicationStopped => CancellationToken.None;

        public void StopApplication() => _stopping.Cancel();

        public void Dispose() => _stopping.Dispose();
    }
}
