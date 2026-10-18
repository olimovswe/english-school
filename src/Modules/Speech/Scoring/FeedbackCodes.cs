namespace EnglishSchool.Modules.Speech.Scoring;

/// <summary>Talaffuz xatolari turlari va ularning oʻzbekcha izohi (oʻzbek tilida soʻzlashuvchilarga xos xatolar).</summary>
internal static class FeedbackCodes
{
    /// <summary>th oʻrniga s, z, t, d yoki f: think → sink, three → tree, this → dis.</summary>
    public const string ThSound = "TH_SOUND";

    /// <summary>w va v almashadi: wine ↔ vine, west ↔ vest.</summary>
    public const string WvSound = "W_V_SOUND";

    /// <summary>Oxirgi undosh tushib qoldi: cats → cat, cold → coal.</summary>
    public const string FinalConsonant = "FINAL_CONSONANT";

    public const string WrongWord = "WRONG_WORD";

    public const string MissingWord = "MISSING_WORD";

    public const string ExtraWord = "EXTRA_WORD";

    private static readonly Dictionary<string, string> Hints = new(StringComparer.Ordinal)
    {
        [ThSound] = "th tovushi: til uchini yuqori tishlar orasiga qoʻyib, havoni chiqaring (think, this). Bu s, z, t yoki d emas.",
        [WvSound] = "w — lablar dumaloq, tishlar labga tegmaydi (wine); v — yuqori tishlar pastki labga tegadi (vine).",
        [FinalConsonant] = "Soʻz oxiridagi undoshni tushirib qoldirmang: uni aniq ayting (cats, cold).",
        [WrongWord] = "Bu soʻz oʻrniga boshqa soʻz eshitildi. Namunani tinglab, qayta urinib koʻring.",
        [MissingWord] = "Bu soʻz eshitilmadi: uni ham aniq ayting.",
        [ExtraWord] = "Matnda yoʻq soʻz aytildi.",
    };

    public static string? Hint(string? code) => code is null ? null : Hints.GetValueOrDefault(code);
}
