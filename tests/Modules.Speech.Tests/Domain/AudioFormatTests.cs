using EnglishSchool.Modules.Speech.Domain;
using EnglishSchool.Modules.Speech.Tests.TestSupport;

namespace EnglishSchool.Modules.Speech.Tests.Domain;

public sealed class AudioFormatTests
{
    public static TheoryData<byte[], string> Recordings => new()
    {
        { AudioSamples.Wav(), "wav" },
        { AudioSamples.Webm(), "webm" },
        { AudioSamples.Mp3(), "mp3" },
        { [0xFF, 0xFB, 0x90, 0x64, 0x00], "mp3" },
        { [.. "OggS"u8, 0x00, 0x02], "ogg" },
        { [.. "fLaC"u8, 0x00, 0x00], "flac" },
        { [0x00, 0x00, 0x00, 0x20, .. "ftypM4A "u8], "m4a" },
    };

    public static TheoryData<byte[]> NotRecordings => new()
    {
        AudioSamples.Png(),
        "just some text"u8.ToArray(),
        // AAC (ADTS): kadr sinxronizatsiyasi MP3'ga oʻxshaydi, lekin layer bitlari 00 va Whisper uni qabul qilmaydi.
        (byte[])[0xFF, 0xF1, 0x50, 0x80],
        (byte[])[.. "RIFF"u8, 0x00, 0x00, 0x00, 0x00, .. "AVI "u8],
        (byte[])[],
    };

    [Theory]
    [MemberData(nameof(Recordings))]
    public void Recognizes_formats_whisper_accepts_from_their_first_bytes(byte[] header, string format)
    {
        Assert.Equal(format, AudioFormat.Detect(header)?.Name);
    }

    [Theory]
    [MemberData(nameof(NotRecordings))]
    public void Rejects_files_that_are_not_supported_audio_whatever_their_name(byte[] header)
    {
        Assert.Null(AudioFormat.Detect(header));
    }
}
