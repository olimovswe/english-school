using System.Text.Json;
using EnglishSchool.BuildingBlocks.Health;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace EnglishSchool.Host.Api.Health;

internal static class HealthEndpoints
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public static void MapApiHealthChecks(this IEndpointRouteBuilder endpoints)
    {
        // Tiriklik: jarayon soʻrovga javob beryapti. Tashqi tizimlar tekshirilmaydi, aks holda baza uzilganda
        // orkestrator sogʻlom konteynerni bekorga qayta ishga tushirardi.
        endpoints.MapHealthChecks("/health/live", new HealthCheckOptions
        {
            Predicate = _ => false,
            ResponseWriter = WriteAsync,
        });

        // Tayyorlik: har bir modul bazasi va uning migratsiyalari, Valkey, toʻxtash holati. 503 — trafik yubormang.
        endpoints.MapHealthChecks("/health/ready", new HealthCheckOptions
        {
            Predicate = registration => registration.Tags.Contains(HealthCheckTags.Ready),
            ResponseWriter = WriteAsync,
        });

        // Oddiy monitorlar uchun: barcha tekshiruvlar, javob matni Healthy / Degraded / Unhealthy.
        endpoints.MapHealthChecks("/health");
    }

    /// <summary>
    /// Har bir tekshiruvning holati va vaqti. Tekshiruv istisno bilan yiqilsa, uning matni chiqarilmaydi (unda server
    /// nomlari boʻlishi mumkin): u health check loglarida qoladi. Tekshiruvning oʻz tavsifi koʻrsatiladi.
    /// </summary>
    private static Task WriteAsync(HttpContext context, HealthReport report) =>
        context.Response.WriteAsJsonAsync(
            new
            {
                Status = report.Status.ToString(),
                DurationMs = Math.Round(report.TotalDuration.TotalMilliseconds, 1),
                Checks = report.Entries.Select(entry => new
                {
                    Name = entry.Key,
                    Status = entry.Value.Status.ToString(),
                    DurationMs = Math.Round(entry.Value.Duration.TotalMilliseconds, 1),
                    Description = entry.Value.Exception is { } exception && entry.Value.Description == exception.Message
                        ? "Tekshiruv xato bilan tugadi: loglarga qarang."
                        : entry.Value.Description,
                    Data = entry.Value.Data.Count == 0 ? null : entry.Value.Data,
                }),
            },
            Json,
            context.RequestAborted);
}
