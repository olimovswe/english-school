namespace EnglishSchool.Modules.Speech.Domain;

/// <summary>Saqlanadigan audio formati: HTTP <c>Content-Type</c> va ombordagi fayl kengaytmasi.</summary>
internal sealed record AudioFormat(string Name, string ContentType, string Extension)
{
    public static readonly AudioFormat Wav = new("wav", "audio/wav", ".wav");
    public static readonly AudioFormat Mp3 = new("mp3", "audio/mpeg", ".mp3");
    public static readonly AudioFormat Ogg = new("ogg", "audio/ogg", ".ogg");
    public static readonly AudioFormat Webm = new("webm", "audio/webm", ".webm");
    public static readonly AudioFormat Flac = new("flac", "audio/flac", ".flac");
    public static readonly AudioFormat Mp4 = new("m4a", "audio/mp4", ".m4a");

    /// <summary>
    /// Formatni faylning birinchi baytlaridan aniqlaydi; mijoz yuborgan <c>Content-Type</c>'ga ishonilmaydi.
    /// Roʻyxat Whisper qabul qiladigan formatlar: brauzer MediaRecorder'i (WebM/Opus, Safari'da MP4), WAV, MP3, OGG, FLAC.
    /// </summary>
    /// <returns>Notanish yoki audio boʻlmagan fayl uchun <c>null</c>.</returns>
    public static AudioFormat? Detect(ReadOnlySpan<byte> header)
    {
        if (header.Length >= 12 && header.StartsWith("RIFF"u8) && header[8..12].SequenceEqual("WAVE"u8))
        {
            return Wav;
        }

        if (header.Length >= 12 && header[4..8].SequenceEqual("ftyp"u8))
        {
            return Mp4;
        }

        if (header.StartsWith("OggS"u8))
        {
            return Ogg;
        }

        if (header.StartsWith("fLaC"u8))
        {
            return Flac;
        }

        // EBML sarlavhasi: WebM (Matroska) konteyneri.
        if (header.StartsWith((ReadOnlySpan<byte>)[0x1A, 0x45, 0xDF, 0xA3]))
        {
            return Webm;
        }

        // ID3 teg yoki MPEG kadr sinxronizatsiyasi (11 bit). Layer bitlari 00 boʻlsa bu MP3 emas, AAC (ADTS).
        if (header.StartsWith("ID3"u8) || (header.Length >= 2 && header[0] == 0xFF && (header[1] & 0xE0) == 0xE0 && (header[1] & 0x06) != 0))
        {
            return Mp3;
        }

        return null;
    }
}
