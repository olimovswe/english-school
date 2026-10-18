using System.Net;
using System.Net.Http.Json;
using EnglishSchool.Modules.Practice.Application;

namespace EnglishSchool.Modules.Practice.Tests.Api;

[Collection(PracticeApiFactory.CollectionName)]
public sealed class ExerciseEndpointTests(PracticeApiFactory factory)
{
    [Fact]
    public async Task Day_exercises_come_in_order_without_revealing_answers()
    {
        using var student = await factory.CreateStudentAsync();

        using var response = await student.Client.GetAsync(new Uri("/api/v1/practice/days/2/exercises", UriKind.Relative));
        var json = await response.Content.ReadAsStringAsync();
        var exercises = await response.Content.ReadFromJsonAsync<List<ExerciseDto>>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(exercises);
        Assert.Equal([1, 2, 3, 4], exercises.Select(e => e.Position));
        Assert.Equal(["GapFill", "GapFill", "MultipleChoice", "Matching"], exercises.Select(e => e.Type));
        Assert.All(exercises.Where(e => e.Type == "GapFill"), e => Assert.Empty(e.Choices));

        var choice = exercises.Single(e => e.Type == "MultipleChoice");
        Assert.Equal(choice.Choices.Select(c => c.Text).Order(StringComparer.Ordinal), choice.Choices.Select(c => c.Text));

        var matching = exercises.Single(e => e.Type == "Matching");
        Assert.Contains("xursand", matching.MatchTargets);
        Assert.Equal(matching.MatchTargets.Order(StringComparer.Ordinal), matching.MatchTargets);

        foreach (var secret in new[] { "isCorrect", "errorCode", "feedback", "BE_FORM", "explanation" })
        {
            Assert.DoesNotContain(secret, json, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public async Task Days_outside_the_course_are_not_found_and_days_without_exercises_are_empty()
    {
        using var student = await factory.CreateStudentAsync();

        using var beyondCourse = await student.Client.GetAsync(new Uri("/api/v1/practice/days/99/exercises", UriKind.Relative));

        Assert.Equal(HttpStatusCode.NotFound, beyondCourse.StatusCode);
        Assert.Empty(await student.GetExercisesAsync(7));
    }

    [Fact]
    public async Task Practice_requires_a_signed_in_student()
    {
        using var anonymous = factory.CreateClient();

        using var list = await anonymous.GetAsync(new Uri("/api/v1/practice/days/2/exercises", UriKind.Relative));
        using var submit = await anonymous.PostAsJsonAsync(
            "/api/v1/practice/attempts", new SubmitAttemptRequest(Guid.NewGuid(), "am", null, null, 1_000));

        Assert.Equal(HttpStatusCode.Unauthorized, list.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, submit.StatusCode);
    }
}
