using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using EnglishSchool.Modules.Practice.Application;

namespace EnglishSchool.Modules.Practice.Tests.Api;

/// <summary>Identity API orqali roʻyxatdan oʻtgan oʻquvchi: soʻrovlari Bearer token bilan ketadi.</summary>
internal sealed class Student(HttpClient client, Guid userId) : IDisposable
{
    public HttpClient Client { get; } = client;

    public Guid UserId { get; } = userId;

    public void Dispose() => Client.Dispose();
}

internal static class PracticeApi
{
    public static async Task<Student> CreateStudentAsync(this PracticeApiFactory factory)
    {
        var client = factory.CreateClient();
        using var response = await client.PostAsJsonAsync("/api/v1/identity/register", new
        {
            email = $"practice-{Guid.NewGuid():N}@example.com",
            password = "correct-horse-battery-staple",
            displayName = "Practice Student",
        });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var session = await response.Content.ReadFromJsonAsync<JsonElement>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", session.GetProperty("accessToken").GetString());
        return new Student(client, session.GetProperty("userId").GetGuid());
    }

    public static async Task<IReadOnlyList<ExerciseDto>> GetExercisesAsync(this Student student, int dayNumber)
    {
        using var response = await student.Client.GetAsync(new Uri($"/api/v1/practice/days/{dayNumber}/exercises", UriKind.Relative));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return await response.Content.ReadFromJsonAsync<List<ExerciseDto>>() ?? [];
    }

    public static async Task<ExerciseDto> GetExerciseAsync(this Student student, int dayNumber, string prompt) =>
        Assert.Single(await student.GetExercisesAsync(dayNumber), e => e.Prompt == prompt);

    public static Task<HttpResponseMessage> SubmitAsync(this Student student, SubmitAttemptRequest request) =>
        student.Client.PostAsJsonAsync("/api/v1/practice/attempts", request);

    public static async Task<AttemptResultResponse> SubmitTextAsync(this Student student, ExerciseDto exercise, string answer) =>
        await student.SubmitSuccessfullyAsync(new SubmitAttemptRequest(exercise.Id, answer, null, null, 4_000));

    public static async Task<AttemptResultResponse> SelectAsync(this Student student, ExerciseDto exercise, string choice) =>
        await student.SubmitSuccessfullyAsync(new SubmitAttemptRequest(
            exercise.Id, null, Assert.Single(exercise.Choices, c => c.Text == choice).Id, null, 4_000));

    public static async Task<AttemptResultResponse> SubmitSuccessfullyAsync(this Student student, SubmitAttemptRequest request)
    {
        using var response = await student.SubmitAsync(request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return await response.Content.ReadFromJsonAsync<AttemptResultResponse>()
            ?? throw new InvalidOperationException("Baholash javobi boʻsh.");
    }
}
