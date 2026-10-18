using System.Reflection;
using EnglishSchool.BuildingBlocks.Caching;
using EnglishSchool.BuildingBlocks.Modules;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using GenericHost = Microsoft.Extensions.Hosting.Host;

namespace EnglishSchool.Architecture.Tests;

/// <summary>
/// Worker — HTTP'siz host. Modulning <see cref="IModule.AddServices"/> qismi unda ham toʻliq resolve boʻlishi
/// kerak: web'ga xos servislar (authorization, routing) faqat API host'da roʻyxatga olinadi.
/// </summary>
public sealed class HostCompositionTests
{
    [Fact]
    public void All_modules_compose_into_a_non_web_host_with_validated_registrations()
    {
        var builder = GenericHost.CreateApplicationBuilder();
        builder.ConfigureContainer(new DefaultServiceProviderFactory(
            new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true }));

        builder.Services.AddHybridCaching(builder.Configuration);
        foreach (var module in SolutionModules.Names.Select(CreateModule))
        {
            module.AddServices(builder.Services, builder.Configuration);
        }

        using var host = builder.Build();

        Assert.NotNull(host.Services.GetService<IHostedService>());
    }

    private static IModule CreateModule(string name)
    {
        var type = Assembly.Load(new AssemblyName($"EnglishSchool.Modules.{name}")).GetExportedTypes()
            .Single(t => typeof(IModule).IsAssignableFrom(t));

        return (IModule)Activator.CreateInstance(type)!;
    }
}
