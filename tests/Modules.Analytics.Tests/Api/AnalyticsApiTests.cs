using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using EnglishSchool.BuildingBlocks.Abstractions.Events;
using EnglishSchool.Modules.Analytics.Infrastructure;
using EnglishSchool.Modules.Discipline.Contracts.Events;
using EnglishSchool.Modules.Practice.Contracts.Events;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace EnglishSchool.Modules.Analytics.Tests.Api;

[Collection(AnalyticsApiFactory.CollectionName)]
public sealed class AnalyticsApiTests(AnalyticsApiFactory factory)
{
    [Fact]
    public async Task Ingested_events_update_user_progress_weekly_report_and_error_summary_once()
    {
        using var student = await CreateStudentAsync();
        using var beforeAnyEvents = await student.Client.GetAsync("/api/v1/analytics/progress");
        Assert.Equal(HttpStatusCode.OK, beforeAnyEvents.StatusCode);
        using var emptyProgress = JsonDocument.Parse(await beforeAnyEvents.Content.ReadAsStringAsync());
        Assert.Equal(0, emptyProgress.RootElement.GetProperty("activeStreak").GetInt32());

        var now = DateTimeOffset.UtcNow;
        var firstAttempt = new AttemptEvaluated(student.UserId, Guid.NewGuid(), Guid.NewGuid(), 2, "PartiallyCorrect", 0.8m, 0)
        {
            OccurredAt = now,
        };
        var secondAttempt = new AttemptEvaluated(student.UserId, Guid.NewGuid(), Guid.NewGuid(), 2, "PartiallyCorrect", 0.6m, 0)
        {
            OccurredAt = now,
        };
        await DispatchAsync(firstAttempt);
        await DispatchAsync(firstAttempt);
        await DispatchAsync(secondAttempt);
        await DispatchAsync(new ErrorDetected(student.UserId, firstAttempt.AttemptId, "ART_MISSING", "the article")
        {
            OccurredAt = now,
        });
        await DispatchAsync(new ErrorDetected(student.UserId, secondAttempt.AttemptId, "ART_MISSING", "an article")
        {
            OccurredAt = now.AddSeconds(1),
        });
        await DispatchAsync(new DailyGatePassed(student.UserId, 1, 80m)
        {
            OccurredAt = now.AddDays(-1),
        });
        await DispatchAsync(new DailyGatePassed(student.UserId, 2, 80m)
        {
            OccurredAt = now,
        });
        await DispatchAsync(new SessionEnded(student.UserId, Guid.NewGuid(), 2_700, true)
        {
            OccurredAt = now,
        });

        using var progressResponse = await student.Client.GetAsync("/api/v1/analytics/progress");
        Assert.Equal(HttpStatusCode.OK, progressResponse.StatusCode);
        using var progress = JsonDocument.Parse(await progressResponse.Content.ReadAsStringAsync());
        var progressJson = progress.RootElement;
        Assert.Equal(2, progressJson.GetProperty("currentDay").GetInt32());
        Assert.Equal(1, progressJson.GetProperty("completedDays").GetInt32());
        Assert.Equal(2, progressJson.GetProperty("totalPracticeAttempts").GetInt32());
        Assert.Equal(2, progressJson.GetProperty("activeStreak").GetInt32());
        Assert.Equal(0.7m, progressJson.GetProperty("reviewAccuracy").GetDecimal());
        Assert.Equal(45, progressJson.GetProperty("focusMinutesThisWeek").GetInt32());

        using var weeklyResponse = await student.Client.GetAsync("/api/v1/analytics/reports/weekly");
        Assert.Equal(HttpStatusCode.OK, weeklyResponse.StatusCode);
        using var weekly = JsonDocument.Parse(await weeklyResponse.Content.ReadAsStringAsync());
        var weeklyJson = weekly.RootElement;
        Assert.Equal(0.7m, weeklyJson.GetProperty("accuracy").GetDecimal());
        Assert.Equal(45m, weeklyJson.GetProperty("totalFocusMinutes").GetDecimal());
        Assert.Equal(2, weeklyJson.GetProperty("totalPracticeAttempts").GetInt32());
        Assert.True(weeklyJson.GetProperty("wordsLearned").GetInt32() > 0);

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AnalyticsDbContext>();
        var error = await db.ErrorSummaries.SingleAsync(item => item.UserId == student.UserId && item.ErrorCode == "ART_MISSING");
        Assert.Equal((2, "an article", secondAttempt.AttemptId), (error.Occurrences, error.Correction, error.LastAttemptId));
        Assert.Equal(7, await db.ProcessedEvents.CountAsync());
    }

    [Fact]
    public async Task Analytics_endpoints_require_authentication()
    {
        using var anonymous = factory.CreateClient();

        using var progress = await anonymous.GetAsync("/api/v1/analytics/progress");
        using var report = await anonymous.GetAsync("/api/v1/analytics/reports/weekly");

        Assert.Equal(HttpStatusCode.Unauthorized, progress.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, report.StatusCode);
    }

    private async Task<Student> CreateStudentAsync()
    {
        var client = factory.CreateClient();
        using var response = await client.PostAsJsonAsync("/api/v1/identity/register", new
        {
            email = $"analytics-{Guid.NewGuid():N}@example.com",
            password = "correct-horse-battery-staple",
            displayName = "Analytics Student",
        });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer", json.RootElement.GetProperty("accessToken").GetString());
        return new Student(client, json.RootElement.GetProperty("userId").GetGuid());
    }

    private async Task DispatchAsync<TEvent>(TEvent integrationEvent)
        where TEvent : IIntegrationEvent
    {
        using var scope = factory.Services.CreateScope();
        var consumers = scope.ServiceProvider.GetServices<IIntegrationEventConsumer<TEvent>>();
        Assert.NotEmpty(consumers);
        foreach (var consumer in consumers)
        {
            await consumer.HandleAsync(integrationEvent, CancellationToken.None);
        }
    }

    private sealed class Student(HttpClient client, Guid userId) : IDisposable
    {
        public HttpClient Client { get; } = client;

        public Guid UserId { get; } = userId;

        public void Dispose() => Client.Dispose();
    }
}