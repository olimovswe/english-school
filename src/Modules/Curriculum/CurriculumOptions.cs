namespace EnglishSchool.Modules.Curriculum;

internal sealed class CurriculumOptions
{
    public const string Section = "Curriculum";

    public int ActiveCourseVersion { get; set; } = 1;

    /// <summary>Faqat lokal muhit uchun. Productionda migratsiyalar alohida qadamda bajariladi.</summary>
    public bool ApplyMigrationsOnStartup { get; set; }

    public bool SeedOnStartup { get; set; }
}
