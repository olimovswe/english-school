using System.Text.Json;

namespace EnglishSchool.Architecture.Tests;

/// <summary>
/// Maxfiy qiymatlar (kalitlar, tokenlar, parollar) faqat user-secrets yoki muhit oʻzgaruvchilarida yashaydi.
/// Repozitoriyadagi appsettings fayllarida ularning maydoni boʻsh turadi.
/// </summary>
public sealed class ConfigurationTests
{
    private static readonly string[] SecretKeyEndings = ["ApiKey", "SigningKey", "BotToken", "Password", "Secret"];

    private static readonly string RepositoryRoot = FindRepositoryRoot();

    public static TheoryData<string> SettingsFiles => new(
        Directory.EnumerateFiles(Path.Combine(RepositoryRoot, "src"), "appsettings*.json", SearchOption.AllDirectories)
            .Select(path => Path.GetRelativePath(RepositoryRoot, path))
            .Where(path => !path.Split(Path.DirectorySeparatorChar).Any(part => part is "bin" or "obj"))
            .Order(StringComparer.Ordinal));

    [Fact]
    public void Settings_files_of_both_hosts_are_checked()
    {
        Assert.True(SettingsFiles.Count >= 4, "Host.Api va Host.Worker appsettings fayllari topilmadi.");
    }

    [Theory]
    [MemberData(nameof(SettingsFiles))]
    public void Committed_settings_hold_no_secrets(string file)
    {
        using var json = JsonDocument.Parse(File.ReadAllText(Path.Combine(RepositoryRoot, file)));

        var leaks = Flatten(json.RootElement, prefix: null)
            .Where(setting => setting.Value.Length > 0 && (
                SecretKeyEndings.Any(ending => setting.Key.EndsWith(ending, StringComparison.OrdinalIgnoreCase)) ||
                (setting.Key.StartsWith("ConnectionStrings:", StringComparison.Ordinal) &&
                    setting.Value.Contains("password", StringComparison.OrdinalIgnoreCase))))
            .Select(setting => setting.Key);

        Assert.Empty(leaks);
    }

    private static IEnumerable<(string Key, string Value)> Flatten(JsonElement element, string? prefix) => element.ValueKind switch
    {
        JsonValueKind.Object => element.EnumerateObject().SelectMany(property =>
            Flatten(property.Value, prefix is null ? property.Name : $"{prefix}:{property.Name}")),
        JsonValueKind.String => [(prefix ?? string.Empty, element.GetString() ?? string.Empty)],
        _ => [],
    };

    private static string FindRepositoryRoot()
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
        {
            if (File.Exists(Path.Combine(directory.FullName, "EnglishSchool.slnx")))
            {
                return directory.FullName;
            }
        }

        throw new InvalidOperationException("EnglishSchool.slnx topilmadi: testlar repozitoriya ichidan ishga tushirilishi kerak.");
    }
}
