using System.Reflection;
using EnglishSchool.BuildingBlocks.Caching;
using EnglishSchool.BuildingBlocks.Modules;
using EnglishSchool.BuildingBlocks.Outbox;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using GenericHost = Microsoft.Extensions.Hosting.Host;

namespace EnglishSchool.Architecture.Tests;

/// <summary>Barcha modullarni Worker kabi HTTP'siz host'ga yigʻadi; DI roʻyxatlari build paytida tekshiriladi.</summary>
internal static class ModuleComposition
{
    public static IEnumerable<Assembly> ImplementationAssemblies =>
        SolutionModules.Names.Select(name => Assembly.Load(new AssemblyName($"EnglishSchool.Modules.{name}")));

    public static IEnumerable<Assembly> ContractAssemblies =>
        SolutionModules.Names.Select(name => Assembly.Load(new AssemblyName($"EnglishSchool.Modules.{name}.Contracts")));

    public static IHost BuildNonWebHost(bool withOutboxDispatcher)
    {
        var builder = GenericHost.CreateApplicationBuilder();
        builder.Configuration["ConnectionStrings:Postgres"] = "Host=localhost;Database=architecture_tests";
        builder.ConfigureContainer(new DefaultServiceProviderFactory(
            new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true }));

        builder.Services.AddHybridCaching(builder.Configuration);
        if (withOutboxDispatcher)
        {
            builder.Services.AddOutboxDispatcher(builder.Configuration);
        }

        foreach (var module in ImplementationAssemblies.Select(CreateModule))
        {
            module.AddServices(builder.Services, builder.Configuration);
        }

        return builder.Build();
    }

    public static bool RunsOutboxDispatcher(IHost host) =>
        host.Services.GetServices<IHostedService>().Any(service => service.GetType().Name == "OutboxDispatcher");

    private static IModule CreateModule(Assembly assembly) =>
        (IModule)Activator.CreateInstance(assembly.GetExportedTypes().Single(t => typeof(IModule).IsAssignableFrom(t)))!;
}
