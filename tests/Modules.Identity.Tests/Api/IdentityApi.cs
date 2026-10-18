using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using EnglishSchool.Modules.Identity.Application;
using EnglishSchool.Modules.Identity.Security;

namespace EnglishSchool.Modules.Identity.Tests.Api;

/// <summary>Identity endpointlari uchun HTTP yordamchilari.</summary>
internal static class IdentityApi
{
    public const string Password = "correct-horse-battery-staple";

    public static string UniqueEmail() => $"student-{Guid.NewGuid():N}@example.com";

    public static Task<HttpResponseMessage> RegisterAsync(
        this HttpClient client, string email, string password = Password, string displayName = "Ali Valiyev", string? timeZone = null) =>
        client.PostAsJsonAsync("/api/v1/identity/register", new RegisterRequest(email, password, displayName, timeZone));

    public static Task<HttpResponseMessage> LoginAsync(this HttpClient client, string email, string password) =>
        client.PostAsJsonAsync("/api/v1/identity/login", new LoginRequest(email, password));

    public static Task<HttpResponseMessage> SignInWithTelegramAsync(this HttpClient client, TelegramAuthData data) =>
        client.PostAsJsonAsync("/api/v1/identity/telegram", data);

    public static Task<HttpResponseMessage> RefreshAsync(this HttpClient client, string refreshToken) =>
        client.PostAsJsonAsync("/api/v1/identity/refresh", new RefreshTokenRequest(refreshToken));

    public static Task<HttpResponseMessage> LogoutAsync(this HttpClient client, string refreshToken) =>
        client.PostAsJsonAsync("/api/v1/identity/logout", new RefreshTokenRequest(refreshToken));

    public static async Task<HttpResponseMessage> GetProfileAsync(this HttpClient client, string? accessToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/identity/me");
        if (accessToken is not null)
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        }

        return await client.SendAsync(request);
    }

    public static async Task<AuthResponse> RegisterNewUserAsync(this HttpClient client, string? email = null)
    {
        using var response = await client.RegisterAsync(email ?? UniqueEmail());
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return await response.ReadAsync<AuthResponse>();
    }

    public static async Task<AuthResponse> LoginSuccessfullyAsync(this HttpClient client, string email)
    {
        using var response = await client.LoginAsync(email, Password);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return await response.ReadAsync<AuthResponse>();
    }

    public static async Task<AuthResponse> RefreshSuccessfullyAsync(this HttpClient client, string refreshToken)
    {
        using var response = await client.RefreshAsync(refreshToken);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return await response.ReadAsync<AuthResponse>();
    }

    public static async Task<T> ReadAsync<T>(this HttpResponseMessage response) =>
        await response.Content.ReadFromJsonAsync<T>()
        ?? throw new InvalidOperationException($"{typeof(T).Name} javobi boʻsh.");
}
