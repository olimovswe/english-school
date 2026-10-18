namespace EnglishSchool.Modules.Speech.Storage;

/// <summary>
/// Audio fayllar ombori. Kalitlar S3 obyekt kalitlari kabi (<c>tts/….mp3</c>, <c>recordings/…/….webm</c>),
/// shuning uchun lokal diskdan S3 yoki MinIO'ga oʻtish faqat shu interfeysning yangi amalga oshirilishi.
/// </summary>
internal interface IAudioStorage
{
    /// <summary>Faylni yozadi; shu kalitdagi eski fayl almashtiriladi.</summary>
    Task SaveAsync(string key, ReadOnlyMemory<byte> content, string contentType, CancellationToken cancellationToken);

    /// <returns>Fayl oqimi yoki fayl yoʻq boʻlsa <c>null</c>. Oqimni chaqiruvchi yopadi.</returns>
    Task<Stream?> OpenReadAsync(string key, CancellationToken cancellationToken);

    /// <summary>Faylni oʻchiradi; fayl yoʻq boʻlsa hech narsa qilmaydi.</summary>
    Task DeleteAsync(string key, CancellationToken cancellationToken);
}
