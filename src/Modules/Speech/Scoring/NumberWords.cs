namespace EnglishSchool.Modules.Speech.Scoring;

/// <summary>Raqamni inglizcha soʻz bilan yozadi: Whisper «25» deb yozishi mumkin, matnda esa «twenty-five».</summary>
internal static class NumberWords
{
    public const long Max = 999_999_999;

    private static readonly string[] Ones =
    [
        "zero", "one", "two", "three", "four", "five", "six", "seven", "eight", "nine", "ten",
        "eleven", "twelve", "thirteen", "fourteen", "fifteen", "sixteen", "seventeen", "eighteen", "nineteen",
    ];

    private static readonly string[] Tens = ["", "", "twenty", "thirty", "forty", "fifty", "sixty", "seventy", "eighty", "ninety"];

    /// <summary>0 … 999 999 999. «and»siz (amerikancha): 105 → «one hundred five».</summary>
    public static string Spell(long number)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(number);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(number, Max);

        return number switch
        {
            < 20 => Ones[number],
            < 100 => Tens[number / 10] + Rest(number % 10),
            < 1_000 => $"{Ones[number / 100]} hundred{Rest(number % 100)}",
            < 1_000_000 => $"{Spell(number / 1_000)} thousand{Rest(number % 1_000)}",
            _ => $"{Spell(number / 1_000_000)} million{Rest(number % 1_000_000)}",
        };
    }

    private static string Rest(long remainder) => remainder == 0 ? string.Empty : " " + Spell(remainder);
}
