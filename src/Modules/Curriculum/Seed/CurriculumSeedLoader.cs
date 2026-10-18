using System.Text.Json;

namespace EnglishSchool.Modules.Curriculum.Seed;

internal sealed record SeedCourse(string Course, int Version, string Language, IReadOnlyList<SeedDay> Days);

internal sealed record SeedDay(
    int Day,
    int Week,
    string Kind,
    string Title,
    string? Description,
    SeedGrammar? Grammar,
    SeedVocabulary? Vocabulary);

internal sealed record SeedGrammar(
    string Topic,
    IReadOnlyList<string> Formula,
    IReadOnlyList<string> Examples,
    IReadOnlyList<string> Notes,
    IReadOnlyList<string> Warnings);

internal sealed record SeedVocabulary(
    string Theme,
    IReadOnlyList<string> SourceRefs,
    string? PreparesFor,
    IReadOnlyList<SeedTerm> Words,
    IReadOnlyList<SeedTerm> Phrases);

internal sealed record SeedTerm(string En, string Uz);

/// <summary>Kurs kontenti assembly ichiga joylangan JSON'dan oʻqiladi (Seed/curriculum.v1.json).</summary>
internal static class CurriculumSeedLoader
{
    public const string ResourceName = "Curriculum.Seed.curriculum.v1.json";

    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public static SeedCourse LoadEmbedded()
    {
        using var stream = typeof(CurriculumSeedLoader).Assembly.GetManifestResourceStream(ResourceName)
            ?? throw new InvalidOperationException($"Embedded seed '{ResourceName}' topilmadi.");

        return JsonSerializer.Deserialize<SeedCourse>(stream, JsonOptions)
            ?? throw new InvalidOperationException("Seed fayli boʻsh.");
    }
}
