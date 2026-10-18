using EnglishSchool.Modules.Speech.Contracts;
using EnglishSchool.Modules.Speech.Domain;
using EnglishSchool.Modules.Speech.Infrastructure;
using EnglishSchool.Modules.Speech.Providers;
using EnglishSchool.Modules.Speech.Scoring;
using EnglishSchool.Modules.Speech.Storage;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EnglishSchool.Modules.Speech.Application;

internal enum AssessmentStatus
{
    Assessed,

    /// <summary>Fayl audio emas yoki Whisper qabul qilmaydigan format.</summary>
    UnsupportedAudio,

    /// <summary>Nutq provayderi sozlanmagan.</summary>
    ProviderUnavailable,

    /// <summary>Provayder xato qaytardi yoki javob bermadi: keyinroq qayta urinish mumkin.</summary>
    ProviderFailed,

    /// <summary>Oʻquvchi 24 soatlik chegaraga yetdi.</summary>
    LimitReached,
}

internal sealed record AssessmentOutcome(AssessmentStatus Status, PronunciationAssessmentDto? Result = null);

/// <summary>
/// Talaffuz bahosi: yozuv Whisper'da matnga aylantiriladi, berilgan matn bilan solishtiriladi, keyin yozuv
/// audio omboriga, baho bazaga saqlanadi. Provayder ishlamasa hech narsa saqlanmaydi.
/// </summary>
internal sealed partial class PronunciationService(
    SpeechDbContext db,
    ISpeechTranscriber transcriber,
    IAudioStorage storage,
    SpeechClipService clips,
    IOptions<SpeechOptions> options,
    TimeProvider clock,
    ILogger<PronunciationService> logger)
{
    /// <summary>Kurs ingliz tilida: Whisper tilni oʻzi aniqlamaydi, aksent tufayli adashmasin.</summary>
    public const string Language = "en";

    public const int MaxTranscriptLength = 2000;

    public async Task<AssessmentOutcome> AssessAsync(
        Guid userId, string targetText, ReadOnlyMemory<byte> audio, CancellationToken cancellationToken)
    {
        var format = AudioFormat.Detect(audio.Span);
        if (format is null)
        {
            return new AssessmentOutcome(AssessmentStatus.UnsupportedAudio);
        }

        if (!transcriber.IsAvailable)
        {
            return new AssessmentOutcome(AssessmentStatus.ProviderUnavailable);
        }

        var now = clock.GetUtcNow();
        var since = now.AddDays(-1);
        var recent = await db.PronunciationAssessments.CountAsync(a => a.UserId == userId && a.CreatedAt > since, cancellationToken);
        if (recent >= options.Value.DailyAssessmentLimit)
        {
            return new AssessmentOutcome(AssessmentStatus.LimitReached);
        }

        Transcription transcription;
        try
        {
            transcription = await transcriber.TranscribeAsync(audio, format, Language, cancellationToken);
        }
        catch (SpeechProviderException exception)
        {
            LogTranscriptionFailed(exception, userId);
            return new AssessmentOutcome(AssessmentStatus.ProviderFailed);
        }

        var text = targetText.Trim();
        var score = PronunciationScorer.Score(text, transcription);
        var recording = AudioArtifact.Recording(
            userId, format, audio.Length, Hashing.Sha256Hex(audio.Span), Milliseconds(transcription.DurationSeconds), now);
        var transcript = transcription.Text.Length > MaxTranscriptLength ? transcription.Text[..MaxTranscriptLength] : transcription.Text;
        var assessment = PronunciationAssessment.Record(userId, recording, text, transcript, Language, score, now);

        await storage.SaveAsync(recording.StorageKey, audio, recording.ContentType, cancellationToken);
        db.AudioArtifacts.Add(recording);
        db.PronunciationAssessments.Add(assessment);
        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch
        {
            // Baho yozilmadi: omborda egasiz yozuv qolmasin.
            await storage.DeleteAsync(recording.StorageKey, CancellationToken.None);
            throw;
        }

        var reference = await clips.FindAsync(text, cancellationToken);
        return new AssessmentOutcome(AssessmentStatus.Assessed, assessment.ToDto(reference?.Id));
    }

    private static int? Milliseconds(double? seconds) => seconds is null ? null : (int)Math.Round(seconds.Value * 1000);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Oʻquvchi {UserId} yozuvini Whisper matnga aylantira olmadi.")]
    private partial void LogTranscriptionFailed(Exception exception, Guid userId);
}
