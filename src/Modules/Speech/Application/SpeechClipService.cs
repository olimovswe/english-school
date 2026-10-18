using EnglishSchool.Modules.Curriculum.Contracts;
using EnglishSchool.Modules.Speech.Domain;
using EnglishSchool.Modules.Speech.Infrastructure;
using EnglishSchool.Modules.Speech.Providers;
using EnglishSchool.Modules.Speech.Storage;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace EnglishSchool.Modules.Speech.Application;

/// <summary>
/// Kurs soʻz va iboralarining namuna talaffuzi (TTS). Klip matn va ovoz sozlamalari boʻyicha keshlanadi:
/// bir matn butun kurs uchun bir marta sintez qilinadi va barcha oʻquvchilarga bitta fayl beriladi.
/// </summary>
internal sealed partial class SpeechClipService(
    SpeechDbContext db,
    ISpeechSynthesizer synthesizer,
    IAudioStorage storage,
    ICurriculumQueries curriculum,
    TimeProvider clock,
    ILogger<SpeechClipService> logger)
{
    /// <summary>Kesh kaliti: ovoz sozlamalari va boʻshliqlari yigʻilgan matnning SHA-256 xeshi.</summary>
    public string CacheKeyFor(string text) => Hashing.Sha256Hex($"{synthesizer.Signature}\n{Normalize(text)}");

    /// <returns>Matnning tayyor klipi yoki <c>null</c> (hali sintez qilinmagan).</returns>
    public Task<AudioArtifact?> FindAsync(string text, CancellationToken cancellationToken)
    {
        var cacheKey = CacheKeyFor(text);
        return db.AudioArtifacts.AsNoTracking().SingleOrDefaultAsync(a => a.CacheKey == cacheKey, cancellationToken);
    }

    /// <summary>
    /// Kun lugʻatining hali sintez qilinmagan elementlarini sintez qiladi. Idempotent: mavjud kliplar
    /// oʻtkazib yuboriladi, shuning uchun qayta yetkazilgan event faqat yetishmaganini toʻldiradi.
    /// </summary>
    /// <returns>Yangi yaratilgan kliplar soni.</returns>
    public async Task<int> SynthesizeDayAsync(int dayNumber, CancellationToken cancellationToken)
    {
        if (dayNumber <= 0)
        {
            return 0;
        }

        if (!synthesizer.IsAvailable)
        {
            LogSynthesisDisabled(dayNumber);
            return 0;
        }

        var texts = (await curriculum.GetVocabularyAsync(dayNumber, cancellationToken))
            .Select(v => Normalize(v.English))
            .Distinct(StringComparer.Ordinal)
            .ToDictionary(CacheKeyFor);
        var keys = texts.Keys.ToList();
        var existing = (await db.AudioArtifacts
                .Where(a => a.CacheKey != null && keys.Contains(a.CacheKey))
                .Select(a => a.CacheKey!)
                .ToListAsync(cancellationToken))
            .ToHashSet(StringComparer.Ordinal);

        var created = 0;
        foreach (var (cacheKey, text) in texts.Where(t => !existing.Contains(t.Key)))
        {
            if (await CreateAsync(text, cacheKey, cancellationToken))
            {
                created++;
            }
        }

        if (created > 0)
        {
            LogDaySynthesized(dayNumber, created);
        }

        return created;
    }

    private async Task<bool> CreateAsync(string text, string cacheKey, CancellationToken cancellationToken)
    {
        var audio = await synthesizer.SynthesizeAsync(text, cancellationToken);
        var clip = AudioArtifact.Synthesized(
            text, synthesizer.Voice, cacheKey, audio.Format, audio.Content.Length, Hashing.Sha256Hex(audio.Content), clock.GetUtcNow());

        // Fayl kaliti kesh kalitidan yasaladi: parallel ish xuddi shu faylni bir xil mazmun bilan yozadi.
        await storage.SaveAsync(clip.StorageKey, audio.Content, clip.ContentType, cancellationToken);
        db.AudioArtifacts.Add(clip);
        try
        {
            await db.SaveChangesAsync(cancellationToken);
            return true;
        }
        catch (DbUpdateException exception) when (exception.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            // Shu matnni boshqa Worker (yoki qayta yetkazilgan event) allaqachon saqladi.
            db.Entry(clip).State = EntityState.Detached;
            return false;
        }
    }

    private static string Normalize(string text) => string.Join(' ', text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));

    [LoggerMessage(Level = LogLevel.Debug, Message = "TTS sozlanmagan (Speech:Provider:ApiKey): {DayNumber}-kun kliplari yaratilmadi.")]
    private partial void LogSynthesisDisabled(int dayNumber);

    [LoggerMessage(Level = LogLevel.Information, Message = "{DayNumber}-kun lugʻati uchun {Count} ta namuna talaffuz klipi yaratildi.")]
    private partial void LogDaySynthesized(int dayNumber, int count);
}
