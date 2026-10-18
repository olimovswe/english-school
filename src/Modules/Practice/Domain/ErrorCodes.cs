namespace EnglishSchool.Modules.Practice.Domain;

/// <summary>
/// Xatolar taksonomiyasi. Kodlar barqaror: ular <c>ErrorDetected</c> eventida boshqa modullarga ketadi va
/// <see cref="UserError"/> takrorlanishlarini sanaydi. Yangi kod faqat shu roʻyxatga qoʻshilgandan keyin ishlatiladi.
/// </summary>
internal static class ErrorCodes
{
    public const string ThirdPersonS = "3SG_S";
    public const string ArticleMissing = "ART_MISSING";
    public const string ArticleWrong = "ART_WRONG";
    public const string BeMissing = "BE_MISSING";
    public const string BeForm = "BE_FORM";
    public const string PossessivePronoun = "PRONOUN_POSSESSIVE";
    public const string Demonstrative = "DEMONSTRATIVE";
    public const string PluralForm = "PLURAL_FORM";
    public const string WordOrder = "WORD_ORDER";
    public const string Vocabulary = "VOCABULARY";
    public const string Spelling = "SPELLING";
    public const string Phonics = "PHONICS";

    public static IReadOnlyDictionary<string, string> Descriptions { get; } = new Dictionary<string, string>(StringComparer.Ordinal)
    {
        [ThirdPersonS] = "3-shaxs birlikda feʼlga -s qoʻshilmagan (he goes).",
        [ArticleMissing] = "Artikl tushib qolgan (a developer).",
        [ArticleWrong] = "a / an notoʻgʻri tanlangan (an engineer).",
        [BeMissing] = "to be tushib qolgan (I am a student).",
        [BeForm] = "to be shakli yoki joyi notoʻgʻri (he is, you are).",
        [PossessivePronoun] = "Egalik olmoshi notoʻgʻri (his / her, your).",
        [Demonstrative] = "this / that / these / those notoʻgʻri.",
        [PluralForm] = "Koʻplik shakli notoʻgʻri (books, children).",
        [WordOrder] = "Soʻz tartibi notoʻgʻri (Are you…?).",
        [Vocabulary] = "Soʻz maʼnosi notoʻgʻri.",
        [Spelling] = "Imlo xatosi.",
        [Phonics] = "Oʻqish qoidasi (harf va tovush) notoʻgʻri.",
    };

    public static bool IsKnown(string code) => Descriptions.ContainsKey(code);
}
