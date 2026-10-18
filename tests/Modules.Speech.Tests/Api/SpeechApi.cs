using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using EnglishSchool.BuildingBlocks.Abstractions.Events;
using EnglishSchool.Modules.Discipline.Contracts.Events;
using EnglishSchool.Modules.Speech.Contracts;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace EnglishSchool.Modules.Speech.Tests.Api;

/// <summary>Identity API orqali roʻyxatdan oʻtgan oʻquvchi: soʻrovlari Bearer token bilan ketadi.</summary>
internal sealed class Student(HttpClient client, Guid userId) : IDisposable
{
    public HttpClient Client { get; } = client;

    public Guid UserId { get; } = userId;

    public void Dispose() => Client.Dispose();
}

internal static class SpeechApi
{
    public static async Task<Student> CreateStudentAsync(this WebApplicationFactory<Program> factory)
    {
        var client = factory.CreateClient();
        using var response = await client.PostAsJsonAsync("/api/v1/identity/register", new
        {
            email = $"speech-{Guid.NewGuid():N}@example.com",
            password = "correct-horse-battery-staple",
            displayName = "Speech Student",
        });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var session = await response.Content.ReadFromJsonAsync<JsonElement>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", session.GetProperty("accessToken").GetString());
        return new Student(client, session.GetProperty("userId").GetGuid());
    }

    /// <summary>
    /// Worker kabi: kun ochilgani haqidagi eventni Speech consumer'iga yetkazadi. Retention ham shu eventni tinglaydi,
    /// lekin bu host uning jadvallarini yaratmaydi.
    /// </summary>
    public static async Task UnlockDayAsync(this WebApplicationFactory<Program> factory, Guid userId, int dayNumber)
    {
        using var scope = factory.Services.CreateScope();
        foreach (var consumer in scope.ServiceProvider.GetServices<IIntegrationEventConsumer<DailyGatePassed>>()
                     .Where(c => c.GetType().Assembly == typeof(SpeechModule).Assembly))
        {
            await consumer.HandleAsync(new DailyGatePassed(userId, dayNumber, 0.9m), CancellationToken.None);
        }
    }

    public static async Task<HttpResponseMessage> AssessAsync(
        this HttpClient client, byte[]? audio, string? targetText, string fileName = "recording.wav", string contentType = "audio/wav")
    {
        using var form = new MultipartFormDataContent();
        if (audio is not null)
        {
            var file = new ByteArrayContent(audio);
            file.Headers.ContentType = new MediaTypeHeaderValue(contentType);
            form.Add(file, "audio", fileName);
        }

        if (targetText is not null)
        {
            form.Add(new StringContent(targetText), "targetText");
        }

        return await client.PostAsync(new Uri("/api/v1/speech/assess", UriKind.Relative), form);
    }

    public static async Task<PronunciationAssessmentDto> AssessSuccessfullyAsync(this Student student, byte[] audio, string targetText)
    {
        using var response = await student.Client.AssessAsync(audio, targetText);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return await response.Content.ReadFromJsonAsync<PronunciationAssessmentDto>() ?? throw new InvalidOperationException("Baho javobi boʻsh.");
    }

    public static async Task<IReadOnlyList<VocabularyClipDto>> GetDayClipsAsync(this Student student, int dayNumber)
    {
        using var response = await student.Client.GetAsync(new Uri($"/api/v1/speech/days/{dayNumber}/audio", UriKind.Relative));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return await response.Content.ReadFromJsonAsync<List<VocabularyClipDto>>() ?? throw new InvalidOperationException("Kliplar javobi boʻsh.");
    }

    public static Task<HttpResponseMessage> GetAudioAsync(this HttpClient client, Guid audioId) =>
        client.GetAsync(new Uri($"/api/v1/speech/audio/{audioId}", UriKind.Relative));

    public static async Task<JsonElement> ProblemAsync(this HttpResponseMessage response) =>
        await response.Content.ReadFromJsonAsync<JsonElement>();
}
