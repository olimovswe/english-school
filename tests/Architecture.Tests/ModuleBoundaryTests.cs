using System.Reflection;
using ArchUnitNET.Domain;
using ArchUnitNET.Loader;
using ArchUnitNET.xUnit;
using static ArchUnitNET.Fluent.ArchRuleDefinition;
using ArchAssembly = System.Reflection.Assembly;

namespace EnglishSchool.Architecture.Tests;

/// <summary>
/// Modular monolith chegaralari kod bilan majburlanadi:
/// modul boshqa modulning ichki kodiga emas, faqat uning Contracts loyihasiga tayanadi.
/// </summary>
public sealed class ModuleBoundaryTests
{
    private static readonly string[] Modules = SolutionModules.Names;

    public static TheoryData<string> ModuleNames => new(Modules);

    private static readonly ArchAssembly BuildingBlocks = Load("EnglishSchool.BuildingBlocks");
    private static readonly ArchAssembly Abstractions = Load("EnglishSchool.BuildingBlocks.Abstractions");

    private static readonly Dictionary<string, ArchAssembly> Implementations =
        Modules.ToDictionary(m => m, m => Load($"EnglishSchool.Modules.{m}"));

    private static readonly Dictionary<string, ArchAssembly> Contracts =
        Modules.ToDictionary(m => m, m => Load($"EnglishSchool.Modules.{m}.Contracts"));

    private static readonly ArchUnitNET.Domain.Architecture Architecture = new ArchLoader()
        .LoadAssemblies([BuildingBlocks, Abstractions, .. Implementations.Values, .. Contracts.Values])
        .Build();

    [Theory]
    [MemberData(nameof(ModuleNames))]
    public void Module_does_not_depend_on_other_module_implementations(string module)
    {
        var others = Implementations.Where(kv => kv.Key != module).Select(kv => kv.Value).ToArray();

        Types().That().ResideInAssembly(Implementations[module])
            .Should().NotDependOnAny(Types().That().ResideInAssembly(others[0], others[1..]))
            .Because("modullar bir-biri bilan faqat Contracts orqali gaplashadi")
            .Check(Architecture);
    }

    [Theory]
    [MemberData(nameof(ModuleNames))]
    public void Contracts_do_not_depend_on_any_module_implementation(string module)
    {
        var implementations = Implementations.Values.ToArray();

        Types().That().ResideInAssembly(Contracts[module])
            .Should().NotDependOnAny(Types().That().ResideInAssembly(implementations[0], implementations[1..]))
            .Because("Contracts — modulning ommaviy API'si, u ichki kodni oshkor qilmaydi")
            .Check(Architecture);
    }

    [Fact]
    public void Building_blocks_do_not_depend_on_modules()
    {
        var modules = Implementations.Values.Concat(Contracts.Values).ToArray();

        Types().That().ResideInAssembly(BuildingBlocks, Abstractions)
            .Should().NotDependOnAny(Types().That().ResideInAssembly(modules[0], modules[1..]))
            .Because("umumiy infratuzilma hech qaysi biznes modulni bilmaydi")
            .Check(Architecture);
    }

    [Theory]
    [MemberData(nameof(ModuleNames))]
    public void Contracts_do_not_reference_web_or_data_frameworks(string module)
    {
        string[] forbidden = ["Microsoft.EntityFrameworkCore", "Microsoft.AspNetCore", "Npgsql"];

        var references = Contracts[module].GetReferencedAssemblies()
            .Select(a => a.Name ?? string.Empty)
            .Where(name => forbidden.Any(prefix => name.StartsWith(prefix, StringComparison.Ordinal)))
            .ToList();

        Assert.Empty(references);
    }

    [Theory]
    [MemberData(nameof(ModuleNames))]
    public void Only_module_entry_point_is_public(string module)
    {
        // EF Core migratsiyalari generatsiya qilingan kod sifatida public boʻladi, ular istisno.
        var publicTypes = Implementations[module].GetExportedTypes()
            .Where(t => t.Namespace?.EndsWith(".Migrations", StringComparison.Ordinal) != true)
            .Select(t => t.Name)
            .ToList();

        Assert.Equal([$"{module}Module"], publicTypes);
    }

    private static ArchAssembly Load(string name) => ArchAssembly.Load(new AssemblyName(name));
}
