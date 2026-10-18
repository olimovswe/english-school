using EnglishSchool.BuildingBlocks.Modules;
using EnglishSchool.Modules.Identity;
using EnglishSchool.Modules.Curriculum;
using EnglishSchool.Modules.Discipline;
using EnglishSchool.Modules.Practice;
using EnglishSchool.Modules.Retention;
using EnglishSchool.Modules.AiGateway;
using EnglishSchool.Modules.Speech;
using EnglishSchool.Modules.Analytics;

namespace EnglishSchool.Host.Api;

/// <summary>Composition root: shu host qaysi modullarni yuklashini belgilaydi.</summary>
internal static class ModuleCatalog
{
    public static IReadOnlyList<IModule> All { get; } =
    [
        new IdentityModule(),
        new CurriculumModule(),
        new DisciplineModule(),
        new PracticeModule(),
        new RetentionModule(),
        new AiGatewayModule(),
        new SpeechModule(),
        new AnalyticsModule(),
    ];
}
