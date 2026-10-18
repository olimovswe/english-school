using EnglishSchool.Modules.Retention.Domain;
using Microsoft.Extensions.Options;

namespace EnglishSchool.Modules.Retention.Scheduling;

/// <summary>
/// FSRS-5 rejalashtiruvchisi: mualliflarning maʼlumotnoma implementatsiyasi (open-spaced-repetition/py-fsrs 3.1.0)
/// bilan bir xil formulalar va standart 19 ta vazn. Testlar oʻsha implementatsiya hisoblagan qiymatlar bilan solishtiradi.
/// Shu kunning ichidagi qadamlar (daqiqalar) qisqa muddatli stabillik bilan, kunlar oʻtgandagilari esa
/// eslab qolish ehtimoli (retrievability) bilan hisoblanadi.
/// </summary>
internal sealed class FsrsScheduler(IOptions<RetentionOptions> options)
{
    public static readonly IReadOnlyList<double> DefaultWeights =
    [
        0.4072, 1.1829, 3.1262, 15.4722, 7.2102, 0.5316, 1.0651, 0.0234, 1.616, 0.1544,
        1.0824, 1.9813, 0.0953, 0.2975, 2.2042, 0.2407, 2.9466, 0.5034, 0.6567,
    ];

    private const double Decay = -0.5;

    /// <summary>R(S, S) = 0.9 boʻlishi uchun: 0.9^(1/DECAY) − 1 = 19/81.</summary>
    private static readonly double Factor = Math.Pow(0.9, 1 / Decay) - 1;

    private static readonly TimeSpan FirstAgainStep = TimeSpan.FromMinutes(1);
    private static readonly TimeSpan AgainStep = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan HardStep = TimeSpan.FromMinutes(10);
    private static readonly TimeSpan FirstHardStep = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan FirstGoodStep = TimeSpan.FromMinutes(10);

    private readonly IReadOnlyList<double> _w = DefaultWeights;

    public CardSchedule Schedule(CardState current, Rating rating, DateTimeOffset now)
    {
        var elapsedDays = current is { Phase: not CardPhase.New, LastReviewedAt: { } last }
            ? Math.Max(0, (now - last).Days)
            : 0;
        double? retrievability = current.Phase == CardPhase.New ? null : Retrievability(elapsedDays, current.Stability);

        var (state, wait, scheduledDays) = current.Phase switch
        {
            CardPhase.New => ScheduleNew(current, rating, now),
            CardPhase.Learning or CardPhase.Relearning => ScheduleShortTerm(current, rating, now),
            _ => ScheduleReview(current, rating, retrievability!.Value, now),
        };

        return new CardSchedule(
            state,
            now + wait,
            scheduledDays,
            elapsedDays,
            retrievability,
            IsLapse: current.Phase == CardPhase.Review && rating == Rating.Again);
    }

    private (CardState State, TimeSpan Wait, int Days) ScheduleNew(CardState current, Rating rating, DateTimeOffset now)
    {
        var next = Next(current, rating == Rating.Easy ? CardPhase.Review : CardPhase.Learning,
            InitialStability(rating), InitialDifficulty(rating), current.Lapses, now);

        return rating switch
        {
            Rating.Again => (next, FirstAgainStep, 0),
            Rating.Hard => (next, FirstHardStep, 0),
            Rating.Good => (next, FirstGoodStep, 0),
            _ => InDays(next, NextInterval(next.Stability)),
        };
    }

    /// <summary>Learning / Relearning: shu kunning ichidagi takrorlashlar qisqa muddatli stabillikni oʻzgartiradi.</summary>
    private (CardState State, TimeSpan Wait, int Days) ScheduleShortTerm(CardState current, Rating rating, DateTimeOffset now)
    {
        var good = NextInterval(ShortTermStability(current.Stability, Rating.Good));
        var easy = Math.Max(NextInterval(ShortTermStability(current.Stability, Rating.Easy)), good + 1);

        var phase = rating is Rating.Again or Rating.Hard ? current.Phase : CardPhase.Review;
        var next = Next(current, phase, ShortTermStability(current.Stability, rating), NextDifficulty(current.Difficulty, rating), current.Lapses, now);

        return rating switch
        {
            Rating.Again => (next, AgainStep, 0),
            Rating.Hard => (next, HardStep, 0),
            Rating.Good => InDays(next, good),
            _ => InDays(next, easy),
        };
    }

    private (CardState State, TimeSpan Wait, int Days) ScheduleReview(CardState current, Rating rating, double r, DateTimeOffset now)
    {
        var (d, s) = (current.Difficulty, current.Stability);
        var hard = NextInterval(RecallStability(d, s, r, Rating.Hard));
        var good = NextInterval(RecallStability(d, s, r, Rating.Good));
        hard = Math.Min(hard, good);
        good = Math.Max(good, hard + 1);
        var easy = Math.Max(NextInterval(RecallStability(d, s, r, Rating.Easy)), good + 1);

        if (rating == Rating.Again)
        {
            var lapsed = Next(current, CardPhase.Relearning, ForgetStability(d, s, r), NextDifficulty(d, rating), current.Lapses + 1, now);
            return (lapsed, AgainStep, 0);
        }

        var next = Next(current, CardPhase.Review, RecallStability(d, s, r, rating), NextDifficulty(d, rating), current.Lapses, now);
        return InDays(next, rating switch { Rating.Hard => hard, Rating.Good => good, _ => easy });
    }

    private static CardState Next(CardState current, CardPhase phase, double stability, double difficulty, int lapses, DateTimeOffset now) =>
        new(phase, stability, difficulty, current.Reps + 1, lapses, now);

    /// <summary>
    /// "Easy &gt; Good &gt; Hard" tuzatishidan keyin ham interval eng uzun chegaradan oshmaydi
    /// (maʼlumotnoma bu yerda chegarani 1–2 kunga oshirib yuboradi; boshqa holatlarda natija bir xil).
    /// </summary>
    private (CardState State, TimeSpan Wait, int Days) InDays(CardState state, int days)
    {
        var capped = Math.Min(days, options.Value.MaximumIntervalDays);
        return (state, TimeSpan.FromDays(capped), capped);
    }

    /// <summary>R(t, S) = (1 + FACTOR · t / S)^DECAY.</summary>
    public static double Retrievability(int elapsedDays, double stability) =>
        Math.Pow(1 + (Factor * elapsedDays / stability), Decay);

    /// <summary>
    /// Eslab qolish ehtimoli <see cref="RetentionOptions.DesiredRetention"/> ga tushadigan kun; [1, MaximumIntervalDays] oraligʻida.
    /// Yaxlitlash Python <c>round()</c> kabi (yarmi juftga), maʼlumotnoma bilan bir xil boʻlishi uchun.
    /// </summary>
    public int NextInterval(double stability)
    {
        var settings = options.Value;
        var interval = stability / Factor * (Math.Pow(settings.DesiredRetention, 1 / Decay) - 1);
        return (int)Math.Clamp(Math.Round(interval, MidpointRounding.ToEven), 1, settings.MaximumIntervalDays);
    }

    private double InitialStability(Rating rating) => Math.Max(_w[(int)rating - 1], 0.1);

    private double InitialDifficulty(Rating rating) =>
        Math.Clamp(_w[4] - Math.Exp(_w[5] * ((int)rating - 1)) + 1, 1, 10);

    /// <summary>Qiyinlik baho tomon siljiydi va boshlangʻich "Easy" qiyinligiga qarab biroz qaytadi (mean reversion).</summary>
    private double NextDifficulty(double difficulty, Rating rating)
    {
        var next = difficulty - (_w[6] * ((int)rating - 3));
        return Math.Clamp((_w[7] * InitialDifficulty(Rating.Easy)) + ((1 - _w[7]) * next), 1, 10);
    }

    private double ShortTermStability(double stability, Rating rating) =>
        stability * Math.Exp(_w[17] * ((int)rating - 3 + _w[18]));

    private double RecallStability(double difficulty, double stability, double retrievability, Rating rating)
    {
        var hardPenalty = rating == Rating.Hard ? _w[15] : 1;
        var easyBonus = rating == Rating.Easy ? _w[16] : 1;
        return stability * (1 + (Math.Exp(_w[8])
            * (11 - difficulty)
            * Math.Pow(stability, -_w[9])
            * (Math.Exp((1 - retrievability) * _w[10]) - 1)
            * hardPenalty
            * easyBonus));
    }

    private double ForgetStability(double difficulty, double stability, double retrievability) =>
        _w[11]
        * Math.Pow(difficulty, -_w[12])
        * (Math.Pow(stability + 1, _w[13]) - 1)
        * Math.Exp((1 - retrievability) * _w[14]);
}
