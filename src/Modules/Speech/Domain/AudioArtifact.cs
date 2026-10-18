namespace EnglishSchool.Modules.Speech.Domain;

internal enum AudioKind
{
    /// <summary>Matndan sintez qilingan namuna talaffuz (TTS). Barcha oʻquvchilar uchun umumiy.</summary>
    Synthesized,

    /// <summary>Oʻquvchining oʻz ovoz yozuvi. Faqat egasiga koʻrinadi.</summary>
    Recording,
}

/// <summary>
/// Saqlangan audio faylning metamaʼlumoti. Faylning oʻzi audio omborida (lokal disk yoki S3) <see cref="StorageKey"/>
/// kaliti ostida turadi; baza faqat u qayerda ekanini va nima ekanini biladi.
/// </summary>
internal sealed class AudioArtifact
{
    private AudioArtifact()
    {
    }

    public Guid Id { get; private init; } = Guid.CreateVersion7();

    public AudioKind Kind { get; private init; }

    /// <summary>Yozuv egasi (Identity foydalanuvchisi, FK yoʻq). Sintez qilingan klip umumiy: <c>null</c>.</summary>
    public Guid? OwnerId { get; private init; }

    /// <summary>Sintez qilingan klipda aytilgan matn. Yozuvda <c>null</c>: uning transkripti bahoda saqlanadi.</summary>
    public string? Text { get; private init; }

    /// <summary>TTS ovozi (masalan, <c>alloy</c>).</summary>
    public string? Voice { get; private init; }

    /// <summary>
    /// TTS kesh kaliti: model, ovoz, tezlik, format va matnning SHA-256 xeshi. Bir xil soʻrov qayta sintez
    /// qilinmaydi va bir matnning klipi barcha oʻquvchilarga bitta.
    /// </summary>
    public string? CacheKey { get; private init; }

    /// <summary>S3 uslubidagi obyekt kaliti: <c>tts/{cacheKey}.mp3</c> yoki <c>recordings/{owner}/{id}.webm</c>.</summary>
    public string StorageKey { get; private init; } = string.Empty;

    public string ContentType { get; private init; } = string.Empty;

    public long SizeBytes { get; private init; }

    /// <summary>Davomiylik, agar maʼlum boʻlsa (yozuvda Whisper qaytaradi).</summary>
    public int? DurationMs { get; private init; }

    /// <summary>Fayl mazmunining SHA-256 xeshi (hex): yaxlitlik tekshiruvi va HTTP ETag.</summary>
    public string Sha256 { get; private init; } = string.Empty;

    public DateTimeOffset CreatedAt { get; private init; }

    public static AudioArtifact Synthesized(
        string text, string voice, string cacheKey, AudioFormat format, long sizeBytes, string sha256, DateTimeOffset now) => new()
    {
        Kind = AudioKind.Synthesized,
        Text = text,
        Voice = voice,
        CacheKey = cacheKey,
        StorageKey = $"tts/{cacheKey}{format.Extension}",
        ContentType = format.ContentType,
        SizeBytes = sizeBytes,
        Sha256 = sha256,
        CreatedAt = now,
    };

    public static AudioArtifact Recording(
        Guid ownerId, AudioFormat format, long sizeBytes, string sha256, int? durationMs, DateTimeOffset now)
    {
        var id = Guid.CreateVersion7(now);
        return new AudioArtifact
        {
            Id = id,
            Kind = AudioKind.Recording,
            OwnerId = ownerId,
            StorageKey = $"recordings/{ownerId:N}/{id:N}{format.Extension}",
            ContentType = format.ContentType,
            SizeBytes = sizeBytes,
            DurationMs = durationMs,
            Sha256 = sha256,
            CreatedAt = now,
        };
    }

    /// <summary>Sintez qilingan klipni har bir oʻquvchi tinglay oladi; yozuvni faqat egasi.</summary>
    public bool IsAvailableTo(Guid userId) => Kind == AudioKind.Synthesized || OwnerId == userId;
}
