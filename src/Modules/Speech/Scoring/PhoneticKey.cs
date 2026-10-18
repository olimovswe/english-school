using System.Text;

namespace EnglishSchool.Modules.Speech.Scoring;

/// <summary>
/// Soʻzning soddalashtirilgan talaffuz kaliti: undoshlar tovush boʻyicha (kursning 1-kun oʻqish qoidalari:
/// th → θ, sh → ʃ, ch/tch → ʧ, sch → sk, ph → f, ck → k, qu → kw, x → ks, c + e/i/y → s, magic e), unlilar esa bitta
/// <see cref="Vowel"/> belgisiga yigʻiladi. Shunday qilib <c>think</c>/<c>sink</c> bitta tovushga farq qiladi,
/// <c>their</c>/<c>there</c> esa bir xil. Unli sifatini (ship/sheep) transkriptdan ajratib boʻlmaydi.
/// </summary>
internal static class PhoneticKey
{
    public const char Vowel = 'A';

    public static string Encode(string word)
    {
        ArgumentNullException.ThrowIfNull(word);

        var w = new string([.. word.ToLowerInvariant().Where(char.IsAsciiLetterLower)]);
        if (w.Length > 2 && (w.StartsWith("kn", StringComparison.Ordinal) || w.StartsWith("gn", StringComparison.Ordinal) ||
            w.StartsWith("wr", StringComparison.Ordinal) || w.StartsWith("ps", StringComparison.Ordinal)))
        {
            // Soqov bosh harf: know, gnome, write, psychology.
            w = w[1..];
        }

        var key = new StringBuilder(w.Length);
        for (var i = 0; i < w.Length; i++)
        {
            var next = At(w, i + 1);
            switch (w[i])
            {
                case 'a' or 'e' or 'i' or 'o' or 'u':
                    AppendVowel(key);
                    break;
                case 'y':
                    if (i == 0 && IsVowelLetter(next))
                    {
                        Append(key, 'j');
                    }
                    else
                    {
                        AppendVowel(key);
                    }

                    break;
                case 'w':
                    if (next == 'h')
                    {
                        Append(key, 'w');
                        i++;
                    }
                    else if (IsVowelLetter(next) || i == 0)
                    {
                        Append(key, 'w');
                    }
                    else
                    {
                        // now, saw, bowl: w unlining bir qismi.
                        AppendVowel(key);
                    }

                    break;
                case 'h':
                    // Undoshdan keyingi h birikmalarda hisoblangan; yakka h faqat unli oldida eshitiladi (hello, ahead).
                    if (IsVowelLetter(next) && (i == 0 || IsVowelLetter(w[i - 1])))
                    {
                        Append(key, 'h');
                    }

                    break;
                case 'c':
                    if (next == 'h')
                    {
                        Append(key, 'ʧ');
                        i++;
                    }
                    else if (next == 'k')
                    {
                        Append(key, 'k');
                        i++;
                    }
                    else
                    {
                        Append(key, next is 'e' or 'i' or 'y' ? 's' : 'k');
                    }

                    break;
                case 's':
                    if (next == 'h')
                    {
                        Append(key, 'ʃ');
                        i++;
                    }
                    else if (next == 'c' && At(w, i + 2) == 'h')
                    {
                        // school, scheme: sch → sk.
                        Append(key, 's');
                        Append(key, 'k');
                        i += 2;
                    }
                    else
                    {
                        Append(key, 's');
                    }

                    break;
                case 't':
                    if (next == 'c' && At(w, i + 2) == 'h')
                    {
                        Append(key, 'ʧ');
                        i += 2;
                    }
                    else if (next == 'h')
                    {
                        Append(key, 'θ');
                        i++;
                    }
                    else if (next == 'i' && At(w, i + 2) is 'o' or 'a')
                    {
                        // nation, initial: -tio-/-tia- → ʃ; unli keyingi harfda hisoblanadi.
                        Append(key, 'ʃ');
                        i++;
                    }
                    else
                    {
                        Append(key, 't');
                    }

                    break;
                case 'p':
                    if (next == 'h')
                    {
                        Append(key, 'f');
                        i++;
                    }
                    else
                    {
                        Append(key, 'p');
                    }

                    break;
                case 'g':
                    if (next == 'h')
                    {
                        // ghost — g; night, though — soqov.
                        if (i == 0)
                        {
                            Append(key, 'g');
                        }

                        i++;
                    }
                    else
                    {
                        Append(key, 'g');
                    }

                    break;
                case 'd':
                    if (next == 'g' && At(w, i + 2) == 'e')
                    {
                        Append(key, 'ʤ');
                        i++;
                    }
                    else
                    {
                        Append(key, 'd');
                    }

                    break;
                case 'q':
                    Append(key, 'k');
                    if (next == 'u')
                    {
                        Append(key, 'w');
                        i++;
                    }

                    break;
                case 'x':
                    if (i == 0)
                    {
                        Append(key, 'z');
                    }
                    else
                    {
                        Append(key, 'k');
                        Append(key, 's');
                    }

                    break;
                case 'j':
                    Append(key, 'ʤ');
                    break;
                default:
                    Append(key, w[i]);
                    break;
            }
        }

        DropMagicE(w, key);
        return key.ToString();
    }

    private static char At(string word, int index) => index < word.Length ? word[index] : '\0';

    private static bool IsVowelLetter(char c) => c is 'a' or 'e' or 'i' or 'o' or 'u' or 'y';

    private static void AppendVowel(StringBuilder key)
    {
        if (key.Length == 0 || key[^1] != Vowel)
        {
            key.Append(Vowel);
        }
    }

    /// <summary>Qoʻsh undosh bitta tovush: hello, class, coffee.</summary>
    private static void Append(StringBuilder key, char sound)
    {
        if (key.Length == 0 || key[^1] != sound)
        {
            key.Append(sound);
        }
    }

    /// <summary>
    /// Soʻz oxiridagi undoshdan keyingi «e» oʻqilmaydi (make, there, nice), agar soʻzda boshqa unli boʻlsa.
    /// <c>the</c>, <c>she</c> kabi yagona unlili soʻzlarda «e» qoladi.
    /// </summary>
    private static void DropMagicE(string word, StringBuilder key)
    {
        if (word.Length > 2 && word[^1] == 'e' && !IsVowelLetter(word[^2]) &&
            key.Length > 1 && key[^1] == Vowel && key.ToString(0, key.Length - 1).Contains(Vowel, StringComparison.Ordinal))
        {
            key.Length--;
        }
    }
}
