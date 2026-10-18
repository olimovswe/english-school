using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace EnglishSchool.BuildingBlocks.Health;

public static class HealthChecksBuilderExtensions
{
    /// <summary>Bitta tekshiruv oʻlik ulanishda butun tayyorlik javobini ushlab turmasin.</summary>
    private static readonly TimeSpan DatabaseTimeout = TimeSpan.FromSeconds(5);

    /// <summary>
    /// Modul bazasi: ulanish va migratsiyalar. Nomi <c>{modul}-db</c> (masalan, <c>identity-db</c>),
    /// <see cref="HealthCheckTags.Ready"/> tegi bilan. Architecture.Tests har bir modul bazasi uchun buni talab qiladi.
    /// </summary>
    public static IHealthChecksBuilder AddModuleDatabaseCheck<TContext>(this IHealthChecksBuilder builder, string name)
        where TContext : DbContext =>
        builder.AddCheck<ModuleDatabaseHealthCheck<TContext>>(name, HealthStatus.Unhealthy, [HealthCheckTags.Ready], DatabaseTimeout);
}
