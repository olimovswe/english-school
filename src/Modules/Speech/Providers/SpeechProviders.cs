using EnglishSchool.Modules.Speech.Domain;

namespace EnglishSchool.Modules.Speech.Providers;

/// <summary>Nutqni matnga aylantirish (Whisper).</summary>
internal interface ISpeechTranscriber
{
    /// <summary><c>false</c> — provayder sozlanmagan (API kaliti yoʻq).</summary>
    bool IsAvailable { get; }

    /// <exception cref="SpeechProviderException">Provayder javob bermadi yoki xato qaytardi.</exception>
    Task<Transcription> TranscribeAsync(ReadOnlyMemory<byte> audio, AudioFormat format, string language, CancellationToken cancellationToken);
}

/// <summary>Matndan nutq (TTS): kurs soʻz va iboralarining namuna talaffuzi.</summary>
internal interface ISpeechSynthesizer
{
    bool IsAvailable { get; }

    /// <summary>Ovozni belgilaydigan sozlamalar (model, ovoz, tezlik, format): kesh kalitining bir qismi.</summary>
    string Signature { get; }

    string Voice { get; }

    /// <exception cref="SpeechProviderException">Provayder javob bermadi yoki xato qaytardi.</exception>
    Task<SynthesizedAudio> SynthesizeAsync(string text, CancellationToken cancellationToken);
}

/// <param name="DurationSeconds">Yozuv davomiyligi, provayder bersa.</param>
/// <param name="Words">Soʻzlar vaqt belgilari bilan; provayder bermasa boʻsh.</param>
internal sealed record Transcription(string Text, double? DurationSeconds, IReadOnlyList<TranscribedWord> Words);

/// <param name="Start">Soʻz boshlanishi, soniyada.</param>
/// <param name="End">Soʻz tugashi, soniyada.</param>
internal sealed record TranscribedWord(string Word, double Start, double End);

internal sealed record SynthesizedAudio(byte[] Content, AudioFormat Format);

/// <summary>Nutq provayderi ishlamadi. Xabar loglar uchun; foydalanuvchiga umumiy matn koʻrsatiladi.</summary>
internal sealed class SpeechProviderException : Exception
{
    public SpeechProviderException()
    {
    }

    public SpeechProviderException(string message)
        : base(message)
    {
    }

    public SpeechProviderException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
