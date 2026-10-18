namespace EnglishSchool.Modules.Speech.Scoring;

internal static class Levenshtein
{
    /// <summary>Bir satrni ikkinchisiga aylantirish uchun eng kam belgi qoʻshish, oʻchirish yoki almashtirish soni.</summary>
    public static int Distance(ReadOnlySpan<char> source, ReadOnlySpan<char> target)
    {
        if (source.Length < target.Length)
        {
            var shorter = source;
            source = target;
            target = shorter;
        }

        // Ikki qator yetarli: xotira qisqaroq satr uzunligida.
        Span<int> previous = target.Length < 128 ? stackalloc int[target.Length + 1] : new int[target.Length + 1];
        Span<int> current = target.Length < 128 ? stackalloc int[target.Length + 1] : new int[target.Length + 1];
        for (var j = 0; j <= target.Length; j++)
        {
            previous[j] = j;
        }

        for (var i = 1; i <= source.Length; i++)
        {
            current[0] = i;
            for (var j = 1; j <= target.Length; j++)
            {
                var substitution = previous[j - 1] + (source[i - 1] == target[j - 1] ? 0 : 1);
                current[j] = Math.Min(Math.Min(previous[j] + 1, current[j - 1] + 1), substitution);
            }

            var swap = previous;
            previous = current;
            current = swap;
        }

        return previous[target.Length];
    }

    /// <summary>0 (umuman boshqa) … 1 (bir xil): masofa uzunroq satrga nisbatan.</summary>
    public static double Similarity(string source, string target)
    {
        var longest = Math.Max(source.Length, target.Length);
        return longest == 0 ? 1 : 1 - ((double)Distance(source, target) / longest);
    }
}
