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
}
