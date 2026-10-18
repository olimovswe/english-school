using System.Globalization;
using System.Text;

namespace EnglishSchool.Modules.Speech.Scoring;

/// <summary>
/// Matn va transkriptni bir xil soʻzlar ketma-ketligiga keltiradi: talaffuzga taʼsir qilmaydigan farqlar
/// (harf kattaligi, tinish belgilari, apostrof turi, qisqartma, raqam, aksent) bahoga tushmaydi.
/// </summary>
internal static class TextNormalizer
{
    private static readonly Dictionary<string, string[]> Irregular = new(StringComparer.Ordinal)
    {
        ["can't"] = ["can", "not"],
        ["cannot"] = ["can", "not"],
        ["won't"] = ["will", "not"],
        ["shan't"] = ["shall", "not"],
        ["let's"] = ["let", "us"],
    };

    private static readonly (string Suffix, string Word)[] Contractions =
    [
        ("n't", "not"), ("'re", "are"), ("'ve", "have"), ("'ll", "will"), ("'d", "would"), ("'m", "am"), ("'s", "is"),
    ];

    /// <summary>Bir soʻzning yozuvdagi variantlari (Whisper amerikacha yozadi, kurs ham en-US).</summary>
    private static readonly Dictionary<string, string> Spellings = new(StringComparer.Ordinal)
    {
        ["okay"] = "ok",
        ["mum"] = "mom",
        ["colour"] = "color",
        ["favourite"] = "favorite",
        ["centre"] = "center",
        ["theatre"] = "theater",
        ["grey"] = "gray",
    };

    public static IReadOnlyList<string> Words(string text)
    {
        ArgumentNullException.ThrowIfNull(text);

        var words = new List<string>();
        var token = new StringBuilder();
        foreach (var c in Fold(text).Append(' '))
        {
            if (char.IsLetterOrDigit(c) || c == '\'')
            {
                token.Append(c);
                continue;
            }

            // Tinish belgisi, defis yoki boʻshliq: soʻz tugadi. Chetdagi apostroflar (students', 'cause) tashlanadi.
            var word = token.ToString().Trim('\'');
            token.Clear();
            if (word.Length > 0)
            {
                Expand(word, words);
            }
        }

        return words;
    }

    /// <summary>Kichik harf, aksentsiz harflar (café → cafe), barcha apostroflar ASCII <c>'</c>.</summary>
    private static string Fold(string text)
    {
        var decomposed = text.Normalize(NormalizationForm.FormD);
        var folded = new StringBuilder(decomposed.Length);
        foreach (var c in decomposed)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) == UnicodeCategory.NonSpacingMark)
            {
                continue;
            }

            folded.Append(c is '\u2019' or '\u2018' or '\u02BC' or '\u02BB' or '`' or '\u00B4' ? '\'' : char.ToLowerInvariant(c));
        }

        return folded.ToString();
    }

    private static void Expand(string token, List<string> words)
    {
        if (token.All(char.IsAsciiDigit))
        {
            if (token.Length <= 9)
            {
                words.AddRange(NumberWords.Spell(long.Parse(token, CultureInfo.InvariantCulture)).Split(' '));
                return;
            }

            words.Add(token);
            return;
        }

        if (Irregular.TryGetValue(token, out var irregular))
        {
            words.AddRange(irregular);
            return;
        }

        foreach (var (suffix, word) in Contractions)
        {
            if (token.Length > suffix.Length && token.EndsWith(suffix, StringComparison.Ordinal))
            {
                words.Add(Canonical(token[..^suffix.Length]));
                words.Add(word);
                return;
            }
        }

        words.Add(Canonical(token));
    }

    private static string Canonical(string word)
    {
        var plain = word.Replace("'", string.Empty, StringComparison.Ordinal);
        return Spellings.GetValueOrDefault(plain, plain);
    }
}
