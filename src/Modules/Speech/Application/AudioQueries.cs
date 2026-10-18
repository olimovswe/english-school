using EnglishSchool.Modules.Curriculum.Contracts;
using EnglishSchool.Modules.Speech.Contracts;
using EnglishSchool.Modules.Speech.Domain;
using EnglishSchool.Modules.Speech.Infrastructure;
using EnglishSchool.Modules.Speech.Storage;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EnglishSchool.Modules.Speech.Application;

/// <summary>Ochilgan audio fayl: metamaʼlumot va oqim. Oqimni chaqiruvchi yopadi.</summary>
internal sealed record StoredAudio(AudioArtifact Artifact, Stream Content);

internal sealed partial class AudioQueries(
    SpeechDbContext db,
    IAudioStorage storage,
    SpeechClipService clips,
    ICurriculumQueries curriculum,
    ILogger<AudioQueries> logger)
{
    /// <returns>Fayl yoki <c>null</c>: yoʻq, boshqa oʻquvchining yozuvi yoki fayli ombordan topilmadi.</returns>
    public async Task<StoredAudio?> OpenAsync(Guid userId, Guid audioId, CancellationToken cancellationToken)
    {
        var artifact = await db.AudioArtifacts.AsNoTracking().SingleOrDefaultAsync(a => a.Id == audioId, cancellationToken);
        if (artifact is null || !artifact.IsAvailableTo(userId))
        {
            return null;
        }

        var content = await storage.OpenReadAsync(artifact.StorageKey, cancellationToken);
        if (content is null)
        {
            LogMissingFile(artifact.Id, artifact.StorageKey);
            return null;
        }

        return new StoredAudio(artifact, content);
    }

    /// <returns>Kun lugʻati kurs tartibida, tayyor kliplar bilan; kurs kuni yoʻq boʻlsa <c>null</c>.</returns>
    public async Task<IReadOnlyList<VocabularyClipDto>?> GetDayClipsAsync(int dayNumber, CancellationToken cancellationToken)
    {
        if (await curriculum.GetDayAsync(dayNumber, cancellationToken) is null)
        {
            return null;
        }

        var vocabulary = await curriculum.GetVocabularyAsync(dayNumber, cancellationToken);
        var keys = vocabulary.Select(v => clips.CacheKeyFor(v.English)).ToList();
        var ready = await db.AudioArtifacts
            .Where(a => a.CacheKey != null && keys.Contains(a.CacheKey))
            .Select(a => new { CacheKey = a.CacheKey!, a.Id })
            .ToDictionaryAsync(a => a.CacheKey, a => a.Id, StringComparer.Ordinal, cancellationToken);

        return
        [
            .. vocabulary.Select((v, index) => new VocabularyClipDto(
                v.Id, v.English, v.IsPhrase, ready.TryGetValue(keys[index], out var audioId) ? audioId : null)),
        ];
    }

    [LoggerMessage(Level = LogLevel.Error, Message = "Audio {AudioId} bazada bor, lekin fayli omborda yoʻq: {StorageKey}.")]
    private partial void LogMissingFile(Guid audioId, string storageKey);
}
