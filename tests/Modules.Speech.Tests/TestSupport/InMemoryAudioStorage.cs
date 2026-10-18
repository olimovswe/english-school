using System.Collections.Concurrent;
using EnglishSchool.Modules.Speech.Storage;

namespace EnglishSchool.Modules.Speech.Tests.TestSupport;

/// <summary>Disk oʻrniga xotira: testlar faylni toʻgʻridan-toʻgʻri tekshiradi va hech narsa diskda qolmaydi.</summary>
internal sealed class InMemoryAudioStorage : IAudioStorage
{
    public ConcurrentDictionary<string, (byte[] Content, string ContentType)> Files { get; } = new(StringComparer.Ordinal);

    public Task SaveAsync(string key, ReadOnlyMemory<byte> content, string contentType, CancellationToken cancellationToken)
    {
        Files[key] = (content.ToArray(), contentType);
        return Task.CompletedTask;
    }

    public Task<Stream?> OpenReadAsync(string key, CancellationToken cancellationToken) =>
        Task.FromResult<Stream?>(Files.TryGetValue(key, out var file) ? new MemoryStream(file.Content, writable: false) : null);

    public Task DeleteAsync(string key, CancellationToken cancellationToken)
    {
        Files.TryRemove(key, out _);
        return Task.CompletedTask;
    }
}
