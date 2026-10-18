using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace EnglishSchool.BuildingBlocks.Health;

/// <summary>
/// Modul bazasining tayyorligi: PostgreSQL'ga ulanish bor va modulning barcha migratsiyalari qoʻllangan.
/// Migratsiyasi qolgan modul birinchi soʻrovdayoq yiqiladi, shuning uchun u ham "tayyor emas".
/// </summary>
internal sealed class ModuleDatabaseHealthCheck<TContext>(TContext db) : IHealthCheck
    where TContext : DbContext
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (!await db.Database.CanConnectAsync(cancellationToken))
        {
            return new HealthCheckResult(context.Registration.FailureStatus, "PostgreSQL'ga ulanib boʻlmadi.");
        }

        var pending = (await db.Database.GetPendingMigrationsAsync(cancellationToken)).ToList();
        if (pending.Count > 0)
        {
            return new HealthCheckResult(
                context.Registration.FailureStatus,
                $"{pending.Count} ta migratsiya qoʻllanmagan.",
                data: new Dictionary<string, object> { ["pendingMigrations"] = pending });
        }

        return HealthCheckResult.Healthy("Ulanish bor, barcha migratsiyalar qoʻllangan.");
    }
}
