using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace EnglishSchool.SystemTests.TestSupport;

/// <summary>Identity orqali roʻyxatdan oʻtgan oʻquvchi: barcha soʻrovlari Bearer token bilan, faqat HTTP orqali.</summary>
internal sealed class Student(HttpClient client, Guid userId) : IDisposable
{
    public HttpClient Client { get; } = client;

    public Guid UserId { get; } = userId;

    public static async Task<Student> RegisterAsync(WebApplicationFactory<Program> api, string displayName)
    {
        var client = api.CreateClient();
        using var response = await client.PostAsJsonAsync("/api/v1/identity/register", new
        {
            email = $"system-{Guid.NewGuid():N}@example.com",
            password = "correct-horse-battery-staple",
            displayName,
        });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var session = await response.Content.ReadFromJsonAsync<JsonElement>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", session.GetProperty("accessToken").GetString());
        return new Student(client, session.GetProperty("userId").GetGuid());
    }

    public async Task<JsonElement> GetAsync(string path)
    {
        using var response = await Client.GetAsync(new Uri(path, UriKind.Relative));
        return await ReadAsync(response, $"GET {path}");
    }

    public async Task<JsonElement> PostAsync(string path, object? body)
    {
        using var response = await Client.PostAsJsonAsync(path, body);
        return await ReadAsync(response, $"POST {path}");
    }

    public async Task<HttpStatusCode> StatusAsync(string path)
    {
        using var response = await Client.GetAsync(new Uri(path, UriKind.Relative));
        return response.StatusCode;
    }

    public async Task<byte[]> GetBytesAsync(string path)
    {
        using var response = await Client.GetAsync(new Uri(path, UriKind.Relative));
        Assert.True(response.StatusCode == HttpStatusCode.OK, $"GET {path}: {(int)response.StatusCode}");
        return await response.Content.ReadAsByteArrayAsync();
    }

    /// <summary>Talaffuz bahosi: multipart <c>audio</c> + <c>targetText</c>, brauzer yozuvi kabi.</summary>
    public async Task<JsonElement> AssessAsync(byte[] audio, string targetText)
    {
        using var form = new MultipartFormDataContent();
        var file = new ByteArrayContent(audio);
        file.Headers.ContentType = new MediaTypeHeaderValue("audio/wav");
        form.Add(file, "audio", "recording.wav");
        form.Add(new StringContent(targetText), "targetText");

        using var response = await Client.PostAsync(new Uri("/api/v1/speech/assess", UriKind.Relative), form);
        return await ReadAsync(response, "POST /api/v1/speech/assess");
    }

    public void Dispose() => Client.Dispose();

    private static async Task<JsonElement> ReadAsync(HttpResponseMessage response, string request)
    {
        var body = await response.Content.ReadAsStringAsync();
        Assert.True(response.StatusCode == HttpStatusCode.OK, $"{request}: {(int)response.StatusCode} {body}");
        return JsonDocument.Parse(body).RootElement.Clone();
    }
}
