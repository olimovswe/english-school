namespace EnglishSchool.Modules.Speech.Tests.TestSupport;

internal static class AudioSamples
{
    /// <summary>Haqiqiy sarlavhali 16 kHz, mono, 16-bit PCM WAV (jimlik).</summary>
    public static byte[] Wav(int samples = 1600)
    {
        var dataBytes = samples * 2;
        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream);
        writer.Write("RIFF"u8);
        writer.Write(36 + dataBytes);
        writer.Write("WAVE"u8);
        writer.Write("fmt "u8);
        writer.Write(16);
        writer.Write((short)1);
        writer.Write((short)1);
        writer.Write(16_000);
        writer.Write(32_000);
        writer.Write((short)2);
        writer.Write((short)16);
        writer.Write("data"u8);
        writer.Write(dataBytes);
        writer.Write(new byte[dataBytes]);
        writer.Flush();
        return stream.ToArray();
    }

    /// <summary>Brauzer MediaRecorder'i yozadigan WebM (EBML sarlavhasi).</summary>
    public static byte[] Webm() => [0x1A, 0x45, 0xDF, 0xA3, 0x9F, 0x42, 0x86, 0x81, 0x01, .. new byte[256]];

    /// <summary>ID3 tegli MP3: soxta TTS provayderining javobi.</summary>
    public static byte[] Mp3() => [.. "ID3"u8, 0x04, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0xFF, 0xFB, 0x90, 0x64, .. new byte[128]];

    public static byte[] Png() => [0x89, .. "PNG"u8, 0x0D, 0x0A, 0x1A, 0x0A, .. new byte[64]];
}
