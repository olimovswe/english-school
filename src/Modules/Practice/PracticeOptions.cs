namespace EnglishSchool.Modules.Practice;

internal sealed class PracticeOptions
{
    public const string Section = "Practice";

    /// <summary>Qaysi mashqlar versiyasi beriladi (Curriculum:ActiveCourseVersion bilan bir xil boʻlishi kerak).</summary>
    public int ActiveCourseVersion { get; set; } = 1;

    /// <summary>Faqat lokal muhit uchun. Productionda migratsiyalar alohida qadamda bajariladi.</summary>
    public bool ApplyMigrationsOnStartup { get; set; }

    public bool SeedOnStartup { get; set; }
}
