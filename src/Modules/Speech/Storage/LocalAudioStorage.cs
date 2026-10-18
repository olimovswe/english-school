using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace EnglishSchool.Modules.Speech.Storage;

/// <summary>
/// Fayllarni lokal diskda saqlaydi (<c>Speech:StoragePath</c>, host'ning content root'iga nisbatan).
/// Bitta server uchun yetarli; bir nechta API/Worker nusxasida umumiy disk yoki S3 kerak.
/// </summary>
internal sealed class LocalAudioStorage(IOptions<SpeechOptions> options, IHostEnvironment environment) : IAudioStorage
{
    private readonly string _root = Path.GetFullPath(options.Value.StoragePath, environment.ContentRootPath);

    public async Task SaveAsync(string key, ReadOnlyMemory<byte> content, string contentType, CancellationToken cancellationToken)
    {
        var path = Resolve(key);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);

        // Avval vaqtinchalik faylga, keyin bir harakatda almashtirish: oʻquvchi chala yozilgan faylni koʻrmaydi.
        var temporary = $"{path}.{Guid.NewGuid():N}.tmp";
        try
        {
            await File.WriteAllBytesAsync(temporary, content, cancellationToken);
            File.Move(temporary, path, overwrite: true);
        }
        finally
        {
            File.Delete(temporary);
        }
    }

    public Task<Stream?> OpenReadAsync(string key, CancellationToken cancellationToken)
    {
        var path = Resolve(key);
        Stream? stream = File.Exists(path)
            ? new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize: 16 * 1024, useAsync: true)
            : null;
        return Task.FromResult(stream);
    }

    public Task DeleteAsync(string key, CancellationToken cancellationToken)
    {
        File.Delete(Resolve(key));
        return Task.CompletedTask;
    }

    private string Resolve(string key)
    {
        // Kalitlar modul ichida yasaladi, lekin baribir: hech bir kalit ombor ildizidan tashqariga chiqa olmaydi.
        var path = Path.GetFullPath(Path.Combine(_root, key));
        if (!path.StartsWith(_root + Path.DirectorySeparatorChar, StringComparison.Ordinal))
        {
            throw new ArgumentException($"Audio kaliti ombordan tashqariga chiqadi: {key}", nameof(key));
        }

        return path;
    }
}
