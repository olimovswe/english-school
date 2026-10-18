namespace EnglishSchool.Modules.Speech;

internal sealed class SpeechOptions
{
    public const string Section = "Speech";

    /// <summary>Faqat lokal muhit uchun. Productionda migratsiyalar alohida qadamda bajariladi.</summary>
    public bool ApplyMigrationsOnStartup { get; set; }
}
