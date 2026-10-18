using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;

namespace EnglishSchool.BuildingBlocks.Health;

/// <summary>
/// Toʻxtash signali kelgach tayyorlik yoʻqoladi: yuklama balanslagich yangi soʻrov yubormay qoʻyadi,
/// joriy soʻrovlar esa <c>Hosting:ShutdownTimeout</c> ichida tugaydi.
/// </summary>
public sealed class ShutdownHealthCheck(IHostApplicationLifetime lifetime) : IHealthCheck
{
    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        return Task.FromResult(lifetime.ApplicationStopping.IsCancellationRequested
            ? new HealthCheckResult(context.Registration.FailureStatus, "Server toʻxtamoqda.")
            : HealthCheckResult.Healthy());
    }
}
