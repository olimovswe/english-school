using System.Text.RegularExpressions;

namespace EnglishSchool.Modules.Practice.Grading;

/// <summary>
/// Javob matnini solishtirish uchun tayyorlaydi. <see cref="Simplify"/> faqat shaklni (registr, boʻshliq, egri
/// apostrof, oxirgi tinish belgisi) tekislaydi; <see cref="Canonicalize"/> qisqartmalarni ham ochadi (isn't → is not).
/// </summary>
internal static partial class AnswerText
{
    private static readonly Dictionary<string, string> Contractions = new(StringComparer.Ordinal)
    {
        ["I'M"] = "I AM",
        ["YOU'RE"] = "YOU ARE",
        ["WE'RE"] = "WE ARE",
        ["THEY'RE"] = "THEY ARE",
        ["HE'S"] = "HE IS",
        ["SHE'S"] = "SHE IS",
        ["IT'S"] = "IT IS",
        ["THAT'S"] = "THAT IS",
        ["WHAT'S"] = "WHAT IS",
        ["WHERE'S"] = "WHERE IS",
        ["WHO'S"] = "WHO IS",
        ["THERE'S"] = "THERE IS",
        ["ISN'T"] = "IS NOT",
        ["AREN'T"] = "ARE NOT",
        ["WASN'T"] = "WAS NOT",
        ["WEREN'T"] = "WERE NOT",
        ["DON'T"] = "DO NOT",
        ["DOESN'T"] = "DOES NOT",
        ["DIDN'T"] = "DID NOT",
        ["CAN'T"] = "CANNOT",
        ["WON'T"] = "WILL NOT",
        ["I'VE"] = "I HAVE",
        ["I'LL"] = "I WILL",
        ["LET'S"] = "LET US",
    };

    /// <summary>Bu soʻzlardagi farq — imlo emas, grammatika xatosi (these ↔ those, his ↔ her).</summary>
    private static readonly HashSet<string> FunctionWords = new(StringComparer.Ordinal)
    {
        "A", "AN", "THE", "AM", "IS", "ARE", "WAS", "WERE", "BE", "THIS", "THAT", "THESE", "THOSE",
        "I", "YOU", "HE", "SHE", "IT", "WE", "THEY", "ME", "HIM", "HER", "US", "THEM",
        "MY", "YOUR", "HIS", "ITS", "OUR", "THEIR", "DO", "DOES", "DID", "HAVE", "HAS", "NOT",
        "IN", "ON", "AT", "TO", "OF", "FOR", "AND", "OR", "BUT", "SOME", "ANY", "MUCH", "MANY",
        "WHO", "WHAT", "WHERE", "WHEN", "WHY", "HOW", "WHICH", "THERE", "HERE", "CAN", "WILL",
    };

    public static string Simplify(string text)
    {
        var unified = text.Replace('’', '\'').Replace('‘', '\'').Replace('ʼ', '\'').Replace('`', '\'');
        return Whitespace().Replace(unified, " ").Trim().TrimEnd('.', '!', '?').Trim().ToUpperInvariant();
    }

    public static string Canonicalize(string text)
    {
        var words = Simplify(text).Split(' ').Select(word => Contractions.GetValueOrDefault(word, word));
        return string.Join(' ', words).Replace("CAN NOT", "CANNOT", StringComparison.Ordinal);
    }

    /// <summary>
    /// Ikki <see cref="Simplify"/>langan matn bitta soʻzning imlo xatosi bilan farq qiladimi: tushib qolgan apostrof
    /// yoki 1–2 harflik farq (Damerau-Levenshtein). Qisqa javoblar va grammatik soʻzlar hisobga olinmaydi.
    /// </summary>
    public static bool IsLikelyTypo(string answer, string expected)
    {
        if (string.Equals(answer, expected, StringComparison.Ordinal) || expected.Length < 4)
        {
            return false;
        }

        if (string.Equals(answer.Replace("'", string.Empty, StringComparison.Ordinal), expected.Replace("'", string.Empty, StringComparison.Ordinal), StringComparison.Ordinal))
        {
            return true;
        }

        if (EditDistance(answer, expected) > (expected.Length >= 8 ? 2 : 1))
        {
            return false;
        }

        var answerWords = answer.Split(' ');
        var expectedWords = expected.Split(' ');
        return !answerWords.Except(expectedWords).Concat(expectedWords.Except(answerWords)).Any(FunctionWords.Contains);
    }

    /// <summary>Damerau-Levenshtein (optimal string alignment): qoʻshish, oʻchirish, almashtirish, yonma-yon oʻrin almashish.</summary>
    public static int EditDistance(string a, string b)
    {
        var d = new int[a.Length + 1, b.Length + 1];
        for (var i = 0; i <= a.Length; i++)
        {
            d[i, 0] = i;
        }

        for (var j = 0; j <= b.Length; j++)
        {
            d[0, j] = j;
        }

        for (var i = 1; i <= a.Length; i++)
        {
            for (var j = 1; j <= b.Length; j++)
            {
                var cost = a[i - 1] == b[j - 1] ? 0 : 1;
                d[i, j] = Math.Min(Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1), d[i - 1, j - 1] + cost);
                if (i > 1 && j > 1 && a[i - 1] == b[j - 2] && a[i - 2] == b[j - 1])
                {
                    d[i, j] = Math.Min(d[i, j], d[i - 2, j - 2] + 1);
                }
            }
        }

        return d[a.Length, b.Length];
    }

    [GeneratedRegex(@"\s+")]
    private static partial Regex Whitespace();
}
