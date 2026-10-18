using EnglishSchool.Modules.Speech.Scoring;

namespace EnglishSchool.Modules.Speech.Tests.Scoring;

public sealed class LevenshteinTests
{
    [Theory]
    [InlineData("kitten", "sitting", 3)]
    [InlineData("flaw", "lawn", 2)]
    [InlineData("think", "sink", 2)]
    [InlineData("", "abc", 3)]
    [InlineData("same", "same", 0)]
    public void Counts_single_character_edits_in_either_direction(string source, string target, int distance)
    {
        Assert.Equal((distance, distance), (Levenshtein.Distance(source, target), Levenshtein.Distance(target, source)));
    }

    [Fact]
    public void Long_strings_use_the_heap_and_give_the_same_answer()
    {
        var source = new string('a', 300);
        var target = new string('a', 299) + "b";

        Assert.Equal(1, Levenshtein.Distance(source, target));
    }

    [Theory]
    [InlineData("", "", 1.0)]
    [InlineData("think", "sink", 0.6)]
    [InlineData("abc", "xyz", 0.0)]
    public void Similarity_is_relative_to_the_longer_string(string source, string target, double similarity)
    {
        Assert.Equal(similarity, Levenshtein.Similarity(source, target), 1e-9);
    }
}
