namespace EnglishSchool.Modules.Retention;

internal sealed class RetentionOptions
{
    public const string Section = "Retention";

    /// <summary>Faqat lokal muhit uchun. Productionda migratsiyalar alohida qadamda bajariladi.</summary>
    public bool ApplyMigrationsOnStartup { get; set; }

    /// <summary>Kartochka navbatga qaytganda uni eslab qolish ehtimoli (FSRS "request retention").</summary>
    public double DesiredRetention { get; set; } = 0.9;

    /// <summary>Eng uzun interval. 8 haftalik kurs uchun bir yil yetarli (FSRS standarti — 36 500 kun).</summary>
    public int MaximumIntervalDays { get; set; } = 365;
}
