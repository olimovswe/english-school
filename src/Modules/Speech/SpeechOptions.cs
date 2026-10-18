namespace EnglishSchool.Modules.Speech;

internal sealed class SpeechOptions
{
    public const string Section = "Speech";

    /// <summary>Faqat lokal muhit uchun. Productionda migratsiyalar alohida qadamda bajariladi.</summary>
    public bool ApplyMigrationsOnStartup { get; set; }

    /// <summary>Lokal audio ombori papkasi, host'ning content root'iga nisbatan (yoki absolyut yoʻl).</summary>
    public string StoragePath { get; set; } = "App_Data/audio";

    /// <summary>Bitta yozuvning eng katta hajmi. 30 soniyalik WAV ham 10 MB'dan kichik; Whisper chegarasi 25 MB.</summary>
    public int MaxRecordingBytes { get; set; } = 10 * 1024 * 1024;

    /// <summary>Bitta oʻquvchining 24 soat ichidagi baholari: Whisper pullik, xarajat chegaralanadi.</summary>
    public int DailyAssessmentLimit { get; set; } = 200;

    public SpeechProviderOptions Provider { get; set; } = new();
}

/// <summary>
/// OpenAI Audio API bilan mos provayder: OpenAI'ning oʻzi yoki shu API'ni beradigan oʻz serverimiz
/// (masalan, faster-whisper asosidagi). Ikkala yoʻnalish ham bitta kalit bilan ishlaydi.
/// </summary>
internal sealed class SpeechProviderOptions
{
    public Uri BaseUrl { get; set; } = new("https://api.openai.com/v1/");

    /// <summary>Maxfiy. Boʻsh boʻlsa nutq xizmatlari oʻchiq: baholash 503, kliplar yaratilmaydi.</summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>Soʻz vaqtlari (<c>verbose_json</c>) kerak, shuning uchun <c>whisper-1</c>.</summary>
    public string TranscriptionModel { get; set; } = "whisper-1";

    public string SynthesisModel { get; set; } = "tts-1";

    public string Voice { get; set; } = "alloy";

    /// <summary>Nutq tezligi (0,25–4). Boshlovchilar uchun biroz sekinroq.</summary>
    public double Speed { get; set; } = 0.9;

    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(30);

    public bool IsConfigured => !string.IsNullOrWhiteSpace(ApiKey);
}
