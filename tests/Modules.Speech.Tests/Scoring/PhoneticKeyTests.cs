using EnglishSchool.Modules.Speech.Scoring;

namespace EnglishSchool.Modules.Speech.Tests.Scoring;

/// <summary>Kalit kursning 1-kun oʻqish qoidalariga mos: digraflar bitta tovush, unlilar bitta belgi (A).</summary>
public sealed class PhoneticKeyTests
{
    [Theory]
    [InlineData("think", "θAnk")]
    [InlineData("this", "θAs")]
    [InlineData("ship", "ʃAp")]
    [InlineData("chair", "ʧAr")]
    [InlineData("catch", "kAʧ")]
    [InlineData("school", "skAl")]
    [InlineData("phone", "fAn")]
    [InlineData("back", "bAk")]
    [InlineData("queen", "kwAn")]
    [InlineData("box", "bAks")]
    [InlineData("city", "sAtA")]
    [InlineData("cat", "kAt")]
    [InlineData("job", "ʤAb")]
    [InlineData("bridge", "brAʤ")]
    [InlineData("nation", "nAʃAn")]
    [InlineData("what", "wAt")]
    [InlineData("yes", "jAs")]
    [InlineData("my", "mA")]
    [InlineData("hello", "hAlA")]
    [InlineData("coffee", "kAfA")]
    [InlineData("night", "nAt")]
    [InlineData("write", "rAt")]
    [InlineData("know", "nA")]
    [InlineData("make", "mAk")]
    [InlineData("nice", "nAs")]
    [InlineData("the", "θA")]
    public void Encodes_spelling_rules_as_sounds(string word, string key)
    {
        Assert.Equal(key, PhoneticKey.Encode(word));
    }

    [Theory]
    [InlineData("their", "there")]
    [InlineData("I", "eye")]
    [InlineData("know", "no")]
    [InlineData("write", "right")]
    [InlineData("see", "sea")]
    [InlineData("ship", "sheep")]
    public void Words_that_sound_alike_share_a_key(string first, string second)
    {
        Assert.Equal(PhoneticKey.Encode(first), PhoneticKey.Encode(second));
    }

    [Theory]
    [InlineData("think", "sink")]
    [InlineData("wine", "vine")]
    [InlineData("ship", "chip")]
    [InlineData("fan", "van")]
    public void Different_consonants_give_different_keys(string first, string second)
    {
        Assert.NotEqual(PhoneticKey.Encode(first), PhoneticKey.Encode(second));
    }
}
