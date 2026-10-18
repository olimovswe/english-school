using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using EnglishSchool.BuildingBlocks.Outbox;
using EnglishSchool.Modules.Retention.Application;
using Microsoft.Extensions.DependencyInjection;

namespace EnglishSchool.Modules.Retention.Tests.Api;

/// <summary>Identity API orqali roʻyxatdan oʻtgan oʻquvchi: soʻrovlari Bearer token bilan ketadi.</summary>
internal sealed class Student(HttpClient client, Guid userId) : IDisposable
{
    public HttpClient Client { get; } = client;

    public Guid UserId { get; } = userId;

    public void Dispose() => Client.Dispose();
}

internal static class RetentionApi
{
    /// <summary>Kurs tartibida: 2-kunning birinchi uchta soʻzi.</summary>
    public static readonly string[] DayTwoFirstWords = ["happy", "sad", "tired"];

    public const int DayTwoVocabulary = 29;
    public const int DayThreeVocabulary = 30;

    public static async Task<Student> CreateStudentAsync(this RetentionApiFactory factory)
    {
        var client = factory.CreateClient();
        using var response = await client.PostAsJsonAsync("/api/v1/identity/register", new
        {
            email = $"retention-{Guid.NewGuid():N}@example.com",
            password = "correct-horse-battery-staple",
            displayName = "Retention Student",
        });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var session = await response.Content.ReadFromJsonAsync<JsonElement>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", session.GetProperty("accessToken").GetString());
        return new Student(client, session.GetProperty("userId").GetGuid());
    }

    /// <summary>Event kutmasdan, toʻgʻridan-toʻgʻri kun lugʻatini kartochkaga aylantiradi.</summary>
    public static async Task<int> EnrollAsync(this RetentionApiFactory factory, Guid userId, int dayNumber)
    {
        using var scope = factory.Services.CreateScope();
        return await scope.ServiceProvider.GetRequiredService<FlashcardEnrollment>().EnrollDayAsync(userId, dayNumber, CancellationToken.None);
    }

    /// <summary>Worker kabi: navbat boʻshaguncha barcha outbox'larni yetkazadi.</summary>
    public static async Task DispatchOutboxAsync(this RetentionApiFactory factory)
    {
        await using var worker = factory.BuildWorker();
        var processor = worker.GetRequiredService<OutboxProcessor>();
        while (await processor.ProcessAsync(CancellationToken.None) > 0)
        {
            // Bitta partiyada yetkazilgan eventlar yangi eventlar chiqargan boʻlishi mumkin.
        }
    }

    public static async Task<DueCardsResponse> GetDueAsync(this Student student, int? limit = null)
    {
        var query = limit is null ? string.Empty : $"?limit={limit}";
        using var response = await student.Client.GetAsync(new Uri($"/api/v1/retention/due{query}", UriKind.Relative));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return await response.Content.ReadFromJsonAsync<DueCardsResponse>() ?? throw new InvalidOperationException("Navbat javobi boʻsh.");
    }

    public static Task<HttpResponseMessage> ReviewAsync(this Student student, Guid? cardId, int? rating) =>
        student.Client.PostAsJsonAsync("/api/v1/retention/review", new ReviewCardRequest(cardId, rating, 3_000));

    public static async Task<ReviewResultResponse> ReviewSuccessfullyAsync(this Student student, Guid cardId, int rating)
    {
        using var response = await student.ReviewAsync(cardId, rating);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return await response.Content.ReadFromJsonAsync<ReviewResultResponse>() ?? throw new InvalidOperationException("Baho javobi boʻsh.");
    }
}
