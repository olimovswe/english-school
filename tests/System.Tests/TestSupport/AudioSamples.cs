namespace EnglishSchool.SystemTests.TestSupport;

internal static class AudioSamples
{
    /// <summary>Haqiqiy sarlavhali 16 kHz, mono, 16-bit PCM WAV (0,1 s jimlik).</summary>
    public static byte[] Wav()
    {
        const int dataBytes = 3_200;
        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream);
        writer.Write("RIFF"u8);
        writer.Write(36 + dataBytes);
        writer.Write("WAVEfmt "u8);
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
}
