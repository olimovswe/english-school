namespace EnglishSchool.Host.Api.Edge;

/// <summary>Soʻrovlar chegarasi (bir daqiqalik sirpanuvchi oyna). Testlar muhitida (<c>Testing</c>) oʻchiq.</summary>
internal sealed class RateLimitingSettings
{
    public const string Section = "RateLimiting";

    public bool Enabled { get; set; } = true;

    /// <summary>Bitta kirgan foydalanuvchi (kirmaganda — IP) uchun daqiqasiga API soʻrovlari.</summary>
    public int PermitsPerMinute { get; set; } = 300;

    /// <summary>Kirish, roʻyxatdan oʻtish, Telegram va token yangilash: bitta IP uchun daqiqasiga.</summary>
    public int AuthenticationPermitsPerMinute { get; set; } = 20;

    /// <summary>Talaffuz bahosi: bitta oʻquvchi uchun daqiqasiga (har biri pullik Whisper chaqiruvi).</summary>
    public int SpeechPermitsPerMinute { get; set; } = 20;

    public void Validate()
    {
        if (PermitsPerMinute < 1 || AuthenticationPermitsPerMinute < 1 || SpeechPermitsPerMinute < 1)
        {
            throw new InvalidOperationException($"{Section}: har bir chegara daqiqasiga kamida 1 soʻrov boʻlishi kerak.");
        }
    }
}
