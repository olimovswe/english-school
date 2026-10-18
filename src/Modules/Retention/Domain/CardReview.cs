namespace EnglishSchool.Modules.Retention.Domain;

/// <summary>FSRS bahosi: oʻquvchi soʻzni qanchalik oson esladi.</summary>
internal enum Rating
{
    Again = 1,
    Hard = 2,
    Good = 3,
    Easy = 4,
}

/// <summary>
/// Bitta takrorlash yozuvi (review log). Kartochka holatini qayta tiklash va keyinchalik FSRS parametrlarini
/// oʻquvchilar maʼlumotiga moslab optimallashtirish uchun saqlanadi.
/// </summary>
internal sealed class CardReview
{
    private CardReview()
    {
    }

    public Guid Id { get; private init; } = Guid.CreateVersion7();

    public Guid FlashcardId { get; private init; }

    public Guid UserId { get; private init; }

    public Rating Rating { get; private init; }

    public DateTimeOffset ReviewedAt { get; private init; }

    public CardPhase PhaseBefore { get; private init; }

    /// <summary>Oldingi takrorlashdan beri oʻtgan butun kunlar.</summary>
    public int ElapsedDays { get; private init; }

    /// <summary>Shu baho bilan belgilangan interval (kun).</summary>
    public int ScheduledDays { get; private init; }

    public double Stability { get; private init; }

    public double Difficulty { get; private init; }

    /// <summary>Takrorlash paytidagi eslab qolish ehtimoli; yangi kartochkada yoʻq.</summary>
    public double? Retrievability { get; private init; }

    public int? DurationMs { get; private init; }

    public static CardReview Record(
        Flashcard card,
        Rating rating,
        DateTimeOffset reviewedAt,
        CardPhase phaseBefore,
        int elapsedDays,
        double? retrievability,
        int? durationMs) => new()
    {
        FlashcardId = card.Id,
        UserId = card.UserId,
        Rating = rating,
        ReviewedAt = reviewedAt,
        PhaseBefore = phaseBefore,
        ElapsedDays = elapsedDays,
        ScheduledDays = card.ScheduledDays,
        Stability = card.State.Stability,
        Difficulty = card.State.Difficulty,
        Retrievability = retrievability,
        DurationMs = durationMs,
    };
}
