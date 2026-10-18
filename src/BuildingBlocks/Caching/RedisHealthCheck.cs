using Microsoft.Extensions.Diagnostics.HealthChecks;
using StackExchange.Redis;

namespace EnglishSchool.BuildingBlocks.Caching;

/// <summary>
/// L2 kesh (Valkey/Redis) javob beryaptimi. Kesh ixtiyoriy qatlam: u ishlamasa servis sekinroq, lekin toʻgʻri
/// ishlaydi, shuning uchun bu tekshiruv <see cref="HealthStatus.Degraded"/> bilan roʻyxatga olinadi.
/// </summary>
internal sealed class RedisHealthCheck(RedisConnection connection) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        try
        {
            var multiplexer = await connection.GetAsync().WaitAsync(cancellationToken);
            var latency = await multiplexer.GetDatabase().PingAsync().WaitAsync(cancellationToken);
            return HealthCheckResult.Healthy($"Valkey {latency.TotalMilliseconds:0} ms'da javob berdi.");
        }
        catch (Exception exception) when (exception is RedisException or TimeoutException)
        {
            return new HealthCheckResult(
                context.Registration.FailureStatus, "Valkey javob bermadi: kesh vaqtincha faqat jarayon xotirasida.", exception);
        }
    }
}
