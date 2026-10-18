using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace EnglishSchool.BuildingBlocks.Hosting;

public static class HostingServiceCollectionExtensions
{
    public const string ShutdownTimeoutKey = "Hosting:ShutdownTimeout";

    /// <summary>
    /// Toʻxtash signali (SIGTERM) kelganda joriy HTTP soʻrovlar va outbox'dagi joriy xabar tugashi uchun vaqt.
    /// Konteyner kutish vaqtidan (compose'da <c>stop_grace_period: 30s</c>) qisqaroq boʻlishi kerak, aks holda
    /// jarayon SIGKILL bilan uziladi.
    /// </summary>
    public static IServiceCollection AddGracefulShutdown(this IServiceCollection services, IConfiguration configuration)
    {
        var timeout = configuration.GetValue(ShutdownTimeoutKey, TimeSpan.FromSeconds(25));
        if (timeout <= TimeSpan.Zero)
        {
            throw new InvalidOperationException($"{ShutdownTimeoutKey} musbat boʻlishi kerak (masalan, 00:00:25).");
        }

        services.Configure<HostOptions>(options => options.ShutdownTimeout = timeout);
        return services;
    }
}
