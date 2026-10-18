using EnglishSchool.Modules.Speech.Scoring;

namespace EnglishSchool.Modules.Speech.Tests.Scoring;

public sealed class TextNormalizerTests
{
    [Theory]
    [InlineData("I'm fine, thanks.", "i am fine thanks")]
    [InlineData("Don't worry.", "do not worry")]
    [InlineData("It's 30 degrees.", "it is thirty degrees")]
    [InlineData("I'm 25 years old", "i am twenty five years old")]
    [InlineData("can't, cannot, won't", "can not can not will not")]
    [InlineData("Let's go!", "let us go")]
    [InlineData("Iʼm from Toshkent", "i am from toshkent")]
    [InlineData("I’m here", "i am here")]
    [InlineData("café", "cafe")]
    [InlineData("OK, okay", "ok ok")]
    [InlineData("ex-boss", "ex boss")]
    [InlineData("The students' books", "the students books")]
    [InlineData("one … ten (1–10)", "one ten one ten")]
    [InlineData("It's 5 o'clock", "it is five oclock")]
    [InlineData("…?!", "")]
    public void Splits_text_into_comparable_words(string text, string words)
    {
        Assert.Equal(words, string.Join(' ', TextNormalizer.Words(text)));
    }

    [Theory]
    [InlineData(0, "zero")]
    [InlineData(13, "thirteen")]
    [InlineData(40, "forty")]
    [InlineData(99, "ninety nine")]
    [InlineData(100, "one hundred")]
    [InlineData(105, "one hundred five")]
    [InlineData(2026, "two thousand twenty six")]
    [InlineData(1_000_000, "one million")]
    [InlineData(999_999_999, "nine hundred ninety nine million nine hundred ninety nine thousand nine hundred ninety nine")]
    public void Spells_numbers_in_american_english(long number, string words)
    {
        Assert.Equal(words, NumberWords.Spell(number));
    }
}
