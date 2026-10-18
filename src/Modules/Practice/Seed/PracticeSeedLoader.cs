using System.Text.Json;

namespace EnglishSchool.Modules.Practice.Seed;

internal sealed record SeedPracticeCourse(string Course, int Version, IReadOnlyList<SeedExercise> Exercises);

/// <param name="Type">gapFill · multipleChoice · matching.</param>
internal sealed record SeedExercise(
    int Day,
    string Type,
    string Instruction,
    string Prompt,
    string? Explanation,
    string? Pattern,
    IReadOnlyList<string>? Accepted,
    IReadOnlyList<SeedMistake>? Mistakes,
    IReadOnlyList<SeedChoice>? Options,
    IReadOnlyList<SeedPair>? Pairs);

internal sealed record SeedMistake(string Text, string ErrorCode, string Feedback);

internal sealed record SeedChoice(string Text, bool Correct, string? ErrorCode, string? Feedback);

internal sealed record SeedPair(string En, string Uz);

/// <summary>Mashqlar assembly ichiga joylangan JSON'dan oʻqiladi (Seed/practice.v1.json).</summary>
internal static class PracticeSeedLoader
{
    public const string ResourceName = "Practice.Seed.practice.v1.json";

    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public static SeedPracticeCourse LoadEmbedded()
    {
        using var stream = typeof(PracticeSeedLoader).Assembly.GetManifestResourceStream(ResourceName)
            ?? throw new InvalidOperationException($"Embedded seed '{ResourceName}' topilmadi.");

        return JsonSerializer.Deserialize<SeedPracticeCourse>(stream, JsonOptions)
            ?? throw new InvalidOperationException("Seed fayli boʻsh.");
    }
}
