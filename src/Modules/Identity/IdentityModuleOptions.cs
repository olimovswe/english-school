namespace EnglishSchool.Modules.Identity;

internal sealed class IdentityModuleOptions
{
    public const string Section = "Identity";

    /// <summary>Faqat lokal muhit uchun. Productionda migratsiyalar alohida qadamda bajariladi.</summary>
    public bool ApplyMigrationsOnStartup { get; set; }

    /// <summary>Vaqt zonasi berilmasa (masalan, Telegram orqali kirishda) shu ishlatiladi.</summary>
    public string DefaultTimeZone { get; set; } = "Asia/Tashkent";

    public int MaxFailedLoginAttempts { get; set; } = 5;

    public TimeSpan LockoutDuration { get; set; } = TimeSpan.FromMinutes(5);
}
