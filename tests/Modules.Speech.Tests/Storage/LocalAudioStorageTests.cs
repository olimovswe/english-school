using EnglishSchool.Modules.Speech.Storage;
using Microsoft.Extensions.Hosting.Internal;
using Microsoft.Extensions.Options;

namespace EnglishSchool.Modules.Speech.Tests.Storage;

public sealed class LocalAudioStorageTests : IDisposable
{
    private readonly string _contentRoot = Directory.CreateTempSubdirectory("speech-storage-").FullName;

    public void Dispose() => Directory.Delete(_contentRoot, recursive: true);

    [Fact]
    public async Task Saves_reads_overwrites_and_deletes_files_under_the_configured_folder()
    {
        var storage = Create("App_Data/audio");
        const string key = "recordings/owner/clip.webm";

        await storage.SaveAsync(key, new byte[] { 1, 2, 3 }, "audio/webm", CancellationToken.None);
        await storage.SaveAsync(key, new byte[] { 4, 5 }, "audio/webm", CancellationToken.None);

        Assert.Equal([4, 5], await File.ReadAllBytesAsync(Path.Combine(_contentRoot, "App_Data", "audio", "recordings", "owner", "clip.webm")));
        Assert.Single(Directory.EnumerateFiles(_contentRoot, "*", SearchOption.AllDirectories));
        await using (var stream = await storage.OpenReadAsync(key, CancellationToken.None))
        {
            using var copy = new MemoryStream();
            await stream!.CopyToAsync(copy);
            Assert.Equal([4, 5], copy.ToArray());
        }

        await storage.DeleteAsync(key, CancellationToken.None);
        await storage.DeleteAsync(key, CancellationToken.None);
        Assert.Null(await storage.OpenReadAsync(key, CancellationToken.None));
    }

    [Theory]
    [InlineData("../outside.mp3")]
    [InlineData("tts/../../outside.mp3")]
    [InlineData("/etc/passwd")]
    public async Task A_key_can_never_reach_outside_the_storage_folder(string key)
    {
        var storage = Create("audio");

        await Assert.ThrowsAsync<ArgumentException>(() => storage.SaveAsync(key, new byte[] { 1 }, "audio/mpeg", CancellationToken.None));
        await Assert.ThrowsAsync<ArgumentException>(() => storage.OpenReadAsync(key, CancellationToken.None));
    }

    private LocalAudioStorage Create(string storagePath) => new(
        Options.Create(new SpeechOptions { StoragePath = storagePath }),
        new HostingEnvironment { ContentRootPath = _contentRoot });
}
