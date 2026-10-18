using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using EnglishSchool.BuildingBlocks.Abstractions.Events;
using EnglishSchool.Modules.Discipline.Contracts.Events;
using EnglishSchool.Modules.Retention.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace EnglishSchool.Modules.Retention.Tests.Api;

[Collection(RetentionApiFactory.CollectionName)]
public sealed class EnrollmentTests(RetentionApiFactory factory)
{
    [Fact]
    public async Task Practising_a_day_turns_its_vocabulary_into_due_flashcards_through_the_outbox()
    {
        using var student = await factory.CreateStudentAsync();
        Assert.Equal(0, (await student.GetDueAsync()).TotalDue);

        using var exercises = await student.Client.GetAsync(new Uri("/api/v1/practice/days/2/exercises", UriKind.Relative));
        var gap = (await exercises.Content.ReadFromJsonAsync<JsonElement>()).EnumerateArray().First(e => e.GetProperty("type").GetString() == "GapFill");
        using var attempt = await student.Client.PostAsJsonAsync("/api/v1/practice/attempts", new
        {
            exerciseId = gap.GetProperty("id").GetGuid(),
            answer = "am",
            timeTakenMs = 2_000,
        });
        Assert.Equal(HttpStatusCode.OK, attempt.StatusCode);

        await factory.DispatchOutboxAsync();

        var due = await student.GetDueAsync(limit: 3);
        Assert.Equal(RetentionApi.DayTwoVocabulary, due.TotalDue);
        Assert.Equal(RetentionApi.DayTwoFirstWords, due.Cards.Select(c => c.Front));
        Assert.All(due.Cards, c => Assert.Equal(("Word", "New", 0), (c.Kind, c.Phase, c.Reps)));
    }

    [Fact]
    public async Task Unlocking_a_day_enrolls_its_vocabulary_and_redelivery_creates_no_duplicates()
    {
        using var student = await factory.CreateStudentAsync();
        var unlocked = new DailyGatePassed(student.UserId, 3, 0.9m);

        await ConsumeAsync(unlocked);
        await ConsumeAsync(unlocked);

        var due = await student.GetDueAsync(limit: 100);
        Assert.Equal(RetentionApi.DayThreeVocabulary, due.TotalDue);
        Assert.Equal(25, due.Cards.Count(c => c.Kind == "Word"));
        Assert.Equal(5, due.Cards.Count(c => c.Kind == "Phrase"));
        Assert.Equal(due.Cards.Count, due.Cards.Select(c => c.Front).Distinct().Count());
    }

    [Fact]
    public async Task Enrolling_the_same_day_again_or_a_day_without_vocabulary_adds_nothing()
    {
        using var student = await factory.CreateStudentAsync();

        Assert.Equal(RetentionApi.DayTwoVocabulary, await factory.EnrollAsync(student.UserId, 2));
        Assert.Equal(0, await factory.EnrollAsync(student.UserId, 2));
        Assert.Equal(0, await factory.EnrollAsync(student.UserId, 7));
        Assert.Equal(0, await factory.EnrollAsync(student.UserId, 0));

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<RetentionDbContext>();
        Assert.Equal(RetentionApi.DayTwoVocabulary, await db.Flashcards.CountAsync(f => f.UserId == student.UserId));
    }

    private async Task ConsumeAsync(DailyGatePassed integrationEvent)
    {
        using var scope = factory.Services.CreateScope();
        foreach (var consumer in scope.ServiceProvider.GetServices<IIntegrationEventConsumer<DailyGatePassed>>())
        {
            await consumer.HandleAsync(integrationEvent, CancellationToken.None);
        }
    }
}
