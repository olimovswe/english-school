namespace EnglishSchool.Modules.Retention;

internal sealed class RetentionOptions
{
    public const string Section = "Retention";

    /// <summary>Faqat lokal muhit uchun. Productionda migratsiyalar alohida qadamda bajariladi.</summary>
    public bool ApplyMigrationsOnStartup { get; set; }
}
