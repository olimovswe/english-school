using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using EnglishSchool.BuildingBlocks.Abstractions.Events;
using EnglishSchool.BuildingBlocks.Outbox;
using EnglishSchool.Modules.Discipline.Contracts.Events;
using EnglishSchool.Modules.Discipline.Infrastructure;
using EnglishSchool.Modules.Retention.Application;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace EnglishSchool.Modules.Discipline.Tests.Api;

[Collection(DisciplineApiFactory.CollectionName)]
public sealed class DisciplineApiTests(DisciplineApiFactory factory)
{
    [Fact]
    public async Task Gate_keeps_lessons_locked_below_eighty_and_unlocks_at_eighty_percent()
    {
        using var student = await CreateStudentAsync();
        var client = student.Client;
        await SeedCardsAsync(student.UserId, 3);
        var cards = await GetDueCardsAsync(client);

        using var below = await SubmitGateAsync(client, cards.Select((card, index) =>
            new GateAnswer(card.Id, index < 23 ? card.Back : "incorrect")).ToArray());
        Assert.Equal(HttpStatusCode.OK, below.StatusCode);
        using var belowJson = JsonDocument.Parse(await below.Content.ReadAsStringAsync());
        Assert.Equal(76.67m, belowJson.RootElement.GetProperty("score").GetDecimal());
        Assert.False(belowJson.RootElement.GetProperty("passed").GetBoolean());

        using var lockedLesson = await client.GetAsync("/api/v1/curriculum/days/1");
        Assert.Equal(HttpStatusCode.Forbidden, lockedLesson.StatusCode);

        using var atBoundary = await SubmitGateAsync(client, cards.Select((card, index) =>
            new GateAnswer(card.Id, index < 24 ? card.Back : "incorrect")).ToArray());
        Assert.Equal(HttpStatusCode.OK, atBoundary.StatusCode);
        using var boundaryJson = JsonDocument.Parse(await atBoundary.Content.ReadAsStringAsync());
        Assert.Equal(80m, boundaryJson.RootElement.GetProperty("score").GetDecimal());
        Assert.True(boundaryJson.RootElement.GetProperty("passed").GetBoolean());

        using var unlockedLesson = await client.GetAsync("/api/v1/curriculum/days/1");
        Assert.Equal(HttpStatusCode.OK, unlockedLesson.StatusCode);
        Assert.Single(await OutboxEventsAsync<DailyGatePassed>(student.UserId));
    }

    [Fact]
    public async Task Session_gate_focus_completion_and_events_are_persisted_in_postgres()
    {
        using var student = await CreateStudentAsync();
        var client = student.Client;
        await SeedCardsAsync(student.UserId, 3);
        var cards = await GetDueCardsAsync(client);

        using var started = await client.PostAsync("/api/v1/discipline/sessions/start", content: null);
        Assert.Equal(HttpStatusCode.OK, started.StatusCode);
        using var startedJson = JsonDocument.Parse(await started.Content.ReadAsStringAsync());
        var sessionId = startedJson.RootElement.GetProperty("sessionId").GetGuid();
        Assert.Equal("MorningGate", startedJson.RootElement.GetProperty("status").GetString());

        using var earlyCompletion = await client.PostAsJsonAsync("/api/v1/discipline/sessions/complete", new
        {
            sessionId,
            focusSeconds = 1_200,
            distractionCount = 2,
            minimalDay = false,
        });
        Assert.Equal(HttpStatusCode.Conflict, earlyCompletion.StatusCode);

        using var gate = await SubmitGateAsync(client, cards.Select(card => new GateAnswer(card.Id, card.Back)).ToArray());
        Assert.Equal(HttpStatusCode.OK, gate.StatusCode);

        using var completed = await client.PostAsJsonAsync("/api/v1/discipline/sessions/complete", new
        {
            sessionId,
            focusSeconds = 1_200,
            distractionCount = 2,
            minimalDay = false,
        });
        Assert.Equal(HttpStatusCode.OK, completed.StatusCode);
        using var completedJson = JsonDocument.Parse(await completed.Content.ReadAsStringAsync());
        Assert.Equal("Completed", completedJson.RootElement.GetProperty("status").GetString());
        Assert.Equal(1_200, completedJson.RootElement.GetProperty("totalFocusSeconds").GetInt32());

        Assert.Single(await OutboxEventsAsync<BlockCompleted>(student.UserId));
        Assert.Single(await OutboxEventsAsync<SessionEnded>(student.UserId));
    }

    private async Task<Student> CreateStudentAsync()
    {
        var client = factory.CreateClient();
        using var response = await client.PostAsJsonAsync("/api/v1/identity/register", new
        {
            email = $"discipline-{Guid.NewGuid():N}@example.com",
            password = "correct-horse-battery-staple",
            displayName = "Discipline Student",
        });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer", json.RootElement.GetProperty("accessToken").GetString());
        return new Student(client, json.RootElement.GetProperty("userId").GetGuid());
    }

    private async Task SeedCardsAsync(Guid userId, int dayNumber)
    {
        using var scope = factory.Services.CreateScope();
        await scope.ServiceProvider.GetRequiredService<FlashcardEnrollment>()
            .EnrollDayAsync(userId, dayNumber, CancellationToken.None);
    }

    private static async Task<GateCard[]> GetDueCardsAsync(HttpClient client)
    {
        using var response = await client.GetAsync("/api/v1/retention/due?limit=100");
        response.EnsureSuccessStatusCode();
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return json.RootElement.GetProperty("cards").EnumerateArray()
            .Select(card => new GateCard(card.GetProperty("id").GetGuid(), card.GetProperty("back").GetString()!))
            .ToArray();
    }

    private static Task<HttpResponseMessage> SubmitGateAsync(HttpClient client, GateAnswer[] answers) =>
        client.PostAsJsonAsync("/api/v1/discipline/gate/submit", new { answers });

    private async Task<TEvent[]> OutboxEventsAsync<TEvent>(Guid userId) where TEvent : IIntegrationEvent
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DisciplineDbContext>();
        var typeName = OutboxSerializer.TypeName(typeof(TEvent));
        var messages = await db.Set<OutboxMessage>().AsNoTracking().Where(message => message.Type == typeName).ToListAsync();
        return messages
            .Select(message => (TEvent)OutboxSerializer.Deserialize(message.Content, typeof(TEvent)))
            .Where(integrationEvent => integrationEvent switch
            {
                DailyGatePassed gate => gate.UserId == userId,
                BlockCompleted block => block.UserId == userId,
                SessionEnded session => session.UserId == userId,
                _ => false,
            })
            .ToArray();
    }

    private sealed record GateCard(Guid Id, string Back);

    private sealed record GateAnswer(Guid CardId, string Answer);

    private sealed class Student(HttpClient client, Guid userId) : IDisposable
    {
        public HttpClient Client { get; } = client;

        public Guid UserId { get; } = userId;

        public void Dispose() => Client.Dispose();
    }
}