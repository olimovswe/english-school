using EnglishSchool.Modules.Analytics.Domain;

namespace EnglishSchool.Modules.Analytics.Tests.Domain;

public sealed class ErrorSummaryReadModelTests
{
    [Fact]
    public void Recurrence_updates_count_correction_and_last_attempt_without_changing_first_seen()
    {
        var userId = Guid.NewGuid();
        var firstAttemptId = Guid.NewGuid();
        var secondAttemptId = Guid.NewGuid();
        var firstSeen = new DateTimeOffset(2026, 10, 17, 8, 0, 0, TimeSpan.Zero);
        var lastSeen = firstSeen.AddDays(1);
        var summary = ErrorSummaryReadModel.Create(userId, "ART_MISSING", "a book", firstAttemptId, firstSeen);

        summary.Recur("the book", secondAttemptId, lastSeen);

        Assert.Equal(userId, summary.UserId);
        Assert.Equal("ART_MISSING", summary.ErrorCode);
        Assert.Equal(2, summary.Occurrences);
        Assert.Equal("the book", summary.Correction);
        Assert.Equal(firstSeen, summary.FirstSeenAt);
        Assert.Equal(lastSeen, summary.LastSeenAt);
        Assert.Equal(secondAttemptId, summary.LastAttemptId);
    }
}