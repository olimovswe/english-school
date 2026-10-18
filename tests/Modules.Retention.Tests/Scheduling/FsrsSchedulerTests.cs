using EnglishSchool.Modules.Retention.Domain;
using EnglishSchool.Modules.Retention.Scheduling;
using Microsoft.Extensions.Options;

namespace EnglishSchool.Modules.Retention.Tests.Scheduling;

/// <summary>
/// Kutilgan qiymatlar FSRS mualliflarining maʼlumotnoma implementatsiyasidan olingan: py-fsrs 3.1.0
/// (FSRS-5, standart vaznlar, retention 0.9, eng uzun interval 365 kun). Har bir takrorlash oldingisining vaqtida.
/// </summary>
public sealed class FsrsSchedulerTests
{
    private const double Tolerance = 1e-7;
    private static readonly DateTimeOffset T0 = new(2026, 10, 19, 9, 0, 0, TimeSpan.Zero);
    private readonly FsrsScheduler _scheduler = Create();

    [Theory]
    [InlineData(1, "Learning", 0.4072, 7.2102, 1, 0)]
    [InlineData(2, "Learning", 1.1829, 6.5085472239, 5, 0)]
    [InlineData(3, "Learning", 3.1262, 5.3145778296, 10, 0)]
    [InlineData(4, "Review", 15.4722, 3.2828564951, 15 * 24 * 60, 15)]
    public void New_card_starts_according_to_the_first_rating(
        int rating, string phase, double stability, double difficulty, int dueInMinutes, int scheduledDays)
    {
        var schedule = _scheduler.Schedule(CardState.Initial, (Rating)rating, T0);

        AssertSchedule(schedule, Enum.Parse<CardPhase>(phase), stability, difficulty, scheduledDays, lapses: 0);
        Assert.Equal(T0.AddMinutes(dueInMinutes), schedule.DueAt);
        Assert.Null(schedule.Retrievability);
        Assert.False(schedule.IsLapse);
    }

    [Fact]
    public void Good_ratings_graduate_the_card_and_grow_its_intervals_like_the_reference()
    {
        (CardPhase, double, double, int)[] expected =
        [
            (CardPhase.Learning, 3.1262, 5.3145778296, 0),
            (CardPhase.Review, 4.3509794883, 5.2670355503, 4),
            (CardPhase.Review, 14.9487004419, 5.2206057605, 15),
            (CardPhase.Review, 47.7879038382, 5.1752624276, 48),
            (CardPhase.Review, 136.2925229019, 5.1309801288, 136),
            (CardPhase.Review, 351.351183425, 5.0877340358, 351),
        ];

        AssertChain([.. Enumerable.Repeat(Rating.Good, expected.Length)], expected);
    }

    [Fact]
    public void Forgetting_a_review_card_is_a_lapse_followed_by_relearning()
    {
        var schedules = AssertChain(
            [Rating.Easy, Rating.Again, Rating.Good, Rating.Good],
            [
                (CardPhase.Review, 15.4722, 3.2828564951, 15),
                (CardPhase.Relearning, 2.8534191146, 5.3632098151, 0),
                (CardPhase.Review, 3.9713287823, 5.3145295474, 4),
                (CardPhase.Review, 14.5387150235, 5.266988398, 15),
            ]);

        Assert.Equal([false, true, false, false], schedules.Select(s => s.IsLapse));
        Assert.Equal([0, 1, 1, 1], schedules.Select(s => s.State.Lapses));
        Assert.Equal(schedules[0].DueAt.AddMinutes(5), schedules[1].DueAt);
    }

    [Fact]
    public void Learning_steps_use_short_term_stability_and_clamp_difficulty()
    {
        var schedules = AssertChain(
            [Rating.Again, Rating.Again, Rating.Hard, Rating.Good],
            [
                (CardPhase.Learning, 0.4072, 7.2102, 0),
                (CardPhase.Learning, 0.2070762836, 9.198653482, 0),
                (CardPhase.Learning, 0.1742114908, 10, 0),
                (CardPhase.Review, 0.2424638932, 9.842818842, 1),
            ]);

        Assert.Equal([1, 5, 10], schedules.Take(3).Zip(schedules.Prepend(null).Take(3), (s, before) =>
            (int)(s.DueAt - (before?.DueAt ?? T0)).TotalMinutes));
        Assert.All(schedules, s => Assert.False(s.IsLapse));
    }

    [Theory]
    [InlineData(1, "Relearning", 2.8534191146, 5.3632098151, 0, 1)]
    [InlineData(2, "Review", 26.0058967872, 4.3230331551, 26, 0)]
    [InlineData(3, "Review", 59.234961891, 3.2828564951, 59, 0)]
    [InlineData(4, "Review", 144.4235541881, 2.2426798351, 144, 0)]
    public void Review_card_rated_on_its_due_date_matches_the_reference(
        int rating, string phase, double stability, double difficulty, int scheduledDays, int lapses)
    {
        var review = _scheduler.Schedule(CardState.Initial, Rating.Easy, T0);

        var schedule = _scheduler.Schedule(review.State, (Rating)rating, review.DueAt);

        AssertSchedule(schedule, Enum.Parse<CardPhase>(phase), stability, difficulty, scheduledDays, lapses);
        Assert.Equal(15, schedule.ElapsedDays);
        Assert.Equal(0.9026207995718719, schedule.Retrievability!.Value, Tolerance);
        Assert.Equal((Rating)rating == Rating.Again, schedule.IsLapse);
    }

    [Fact]
    public void Reviewing_early_on_the_same_day_does_not_grow_stability()
    {
        var review = _scheduler.Schedule(CardState.Initial, Rating.Easy, T0);

        var schedule = _scheduler.Schedule(review.State, Rating.Good, T0.AddHours(3));

        AssertSchedule(schedule, CardPhase.Review, 15.4722, 3.2828564951, 16, lapses: 0);
        Assert.Equal((0, 1.0), (schedule.ElapsedDays, schedule.Retrievability));
    }

    [Theory]
    [InlineData(10.0, 10)]
    [InlineData(2.5, 2)]
    [InlineData(3.5, 4)]
    [InlineData(0.3, 1)]
    [InlineData(5000.0, 365)]
    public void At_90_percent_retention_the_interval_is_the_stability_rounded_half_to_even_and_clamped(double stability, int days) =>
        Assert.Equal(days, _scheduler.NextInterval(stability));

    [Fact]
    public void Retrievability_is_90_percent_after_stability_days_and_falls_with_time()
    {
        Assert.Equal(0.9, FsrsScheduler.Retrievability(20, 20), Tolerance);
        Assert.Equal(1.0, FsrsScheduler.Retrievability(0, 20), Tolerance);
        Assert.True(FsrsScheduler.Retrievability(40, 20) < FsrsScheduler.Retrievability(30, 20));
    }

    [Fact]
    public void Lower_desired_retention_gives_longer_intervals_and_the_maximum_caps_them()
    {
        var relaxed = Create(desiredRetention: 0.8);
        var capped = Create(maximumIntervalDays: 30);

        Assert.True(relaxed.NextInterval(10) > _scheduler.NextInterval(10));
        Assert.Equal(30, capped.Schedule(CardState.Initial, Rating.Easy, T0) is var first
            ? capped.Schedule(first.State, Rating.Easy, first.DueAt).ScheduledDays
            : 0);
    }

    [Theory]
    [InlineData("New")]
    [InlineData("Learning")]
    [InlineData("Relearning")]
    public void Again_is_a_lapse_only_for_a_card_in_review(string phaseName)
    {
        var phase = Enum.Parse<CardPhase>(phaseName);
        var state = new CardState(phase, 3, 5, 2, 0, phase == CardPhase.New ? null : T0);

        Assert.False(_scheduler.Schedule(state, Rating.Again, T0.AddMinutes(10)).IsLapse);
    }

    private List<CardSchedule> AssertChain(Rating[] ratings, (CardPhase Phase, double Stability, double Difficulty, int Days)[] expected)
    {
        var schedules = new List<CardSchedule>();
        var (state, now) = (CardState.Initial, T0);

        for (var i = 0; i < ratings.Length; i++)
        {
            var schedule = _scheduler.Schedule(state, ratings[i], now);
            var (phase, stability, difficulty, days) = expected[i];
            AssertSchedule(schedule, phase, stability, difficulty, days, schedule.State.Lapses);
            Assert.Equal(i + 1, schedule.State.Reps);

            schedules.Add(schedule);
            (state, now) = (schedule.State, schedule.DueAt);
        }

        return schedules;
    }

    private static void AssertSchedule(CardSchedule schedule, CardPhase phase, double stability, double difficulty, int scheduledDays, int lapses)
    {
        Assert.Equal(phase, schedule.State.Phase);
        Assert.Equal(stability, schedule.State.Stability, Tolerance);
        Assert.Equal(difficulty, schedule.State.Difficulty, Tolerance);
        Assert.Equal(scheduledDays, schedule.ScheduledDays);
        Assert.Equal(lapses, schedule.State.Lapses);
    }

    private static FsrsScheduler Create(double desiredRetention = 0.9, int maximumIntervalDays = 365) =>
        new(Options.Create(new RetentionOptions { DesiredRetention = desiredRetention, MaximumIntervalDays = maximumIntervalDays }));
}
