namespace EnglishSchool.Modules.Retention.Domain;

/// <summary>FSRS bosqichlari: yangi → oʻrganilmoqda → takrorlash; unutilsa — qayta oʻrganish.</summary>
internal enum CardPhase
{
    New,
    Learning,
    Review,
    Relearning,
}

/// <summary>
/// Kartochkaning FSRS xotira holati. <see cref="Stability"/> — eslab qolish ehtimoli 90% ga tushguncha
/// oʻtadigan kunlar; <see cref="Difficulty"/> — 1 (oson) … 10 (qiyin). Har bir baholashda yangi holat bilan almashtiriladi.
/// </summary>
internal sealed record CardState(
    CardPhase Phase,
    double Stability,
    double Difficulty,
    int Reps,
    int Lapses,
    DateTimeOffset? LastReviewedAt)
{
    public static CardState Initial { get; } = new(CardPhase.New, 0, 0, 0, 0, null);
}
