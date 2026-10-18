using EnglishSchool.BuildingBlocks.Health;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace EnglishSchool.Architecture.Tests;

/// <summary>
/// Worker — HTTP'siz host. Modulning <c>AddServices</c> qismi unda ham toʻliq resolve boʻlishi kerak:
/// web'ga xos servislar (authorization, routing) faqat API host'da roʻyxatga olinadi.
/// </summary>
public sealed class HostCompositionTests
{
    [Fact]
    public void All_modules_compose_into_the_worker_host_with_validated_registrations()
    {
        using var host = ModuleComposition.BuildNonWebHost(withOutboxDispatcher: true);

        Assert.True(ModuleComposition.RunsOutboxDispatcher(host));
    }

    [Fact]
    public void Modules_never_start_the_outbox_dispatcher_themselves()
    {
        using var host = ModuleComposition.BuildNonWebHost(withOutboxDispatcher: false);

        Assert.False(ModuleComposition.RunsOutboxDispatcher(host), "Eventlarni faqat Worker yetkazadi: AddOutboxDispatcher host'da chaqiriladi.");
    }

    [Fact]
    public void Every_module_database_and_its_migrations_are_covered_by_the_readiness_endpoint()
    {
        using var host = ModuleComposition.BuildNonWebHost(withOutboxDispatcher: false);
        // Tayyorlik (/health/ready) faqat "ready" tegli tekshiruvlarni bajaradi.
        var checks = host.Services.GetRequiredService<IOptions<HealthCheckServiceOptions>>().Value.Registrations
            .Where(registration => registration.Tags.Contains(HealthCheckTags.Ready))
            .Select(registration => registration.Name)
            .ToHashSet(StringComparer.Ordinal);

        var expected = ModuleComposition.ImplementationAssemblies
            .Where(assembly => assembly.GetTypes().Any(type => typeof(DbContext).IsAssignableFrom(type) && !type.IsAbstract))
            .Select(assembly => $"{assembly.GetName().Name!["EnglishSchool.Modules.".Length..].ToLowerInvariant()}-db")
            .ToList();

        Assert.NotEmpty(expected);
        Assert.All(expected, name => Assert.True(checks.Contains(name), $"{name}: AddModuleDatabaseCheck chaqirilmagan, /health/ready bu bazani va uning migratsiyalarini tekshirmaydi."));
    }
}
