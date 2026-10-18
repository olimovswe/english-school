using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography;
using EnglishSchool.Modules.Identity.Application;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace EnglishSchool.Modules.Identity.Tests.Api;

[Collection(IdentityApiFactory.CollectionName)]
public sealed class RegistrationAndLoginTests(IdentityApiFactory factory)
{
    [Fact]
    public async Task Register_creates_account_and_its_access_token_opens_the_profile()
    {
        using var client = factory.CreateClient();
        var email = IdentityApi.UniqueEmail();

        using var response = await client.RegisterAsync(email, timeZone: "Asia/Samarkand");

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.EndsWith("/api/v1/identity/me", response.Headers.Location?.ToString(), StringComparison.Ordinal);
        var session = await response.ReadAsync<AuthResponse>();

        using var profileResponse = await client.GetProfileAsync(session.AccessToken);
        Assert.Equal(HttpStatusCode.OK, profileResponse.StatusCode);
        var profile = await profileResponse.ReadAsync<UserProfileResponse>();
        Assert.Equal(session.UserId, profile.Id);
        Assert.Equal(email, profile.Email);
        Assert.Equal("Ali Valiyev", profile.DisplayName);
        Assert.Equal("Asia/Samarkand", profile.TimeZone);
        Assert.Null(profile.TelegramId);
    }

    [Fact]
    public async Task Register_rejects_an_email_that_differs_only_in_letter_case()
    {
        using var client = factory.CreateClient();
        var email = IdentityApi.UniqueEmail();
        await client.RegisterNewUserAsync(email);

        using var response = await client.RegisterAsync(email.ToUpperInvariant());

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Register_reports_every_invalid_field()
    {
        using var client = factory.CreateClient();

        using var response = await client.PostAsJsonAsync(
            "/api/v1/identity/register", new RegisterRequest("not-an-email", "short", "   ", "Mars/Olympus_Mons"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.ReadAsync<HttpValidationProblemDetails>();
        Assert.Equal(["displayName", "email", "password", "timeZone"], problem.Errors.Keys.Order(StringComparer.Ordinal));
    }

    [Fact]
    public async Task Login_with_the_right_password_opens_a_new_session()
    {
        using var client = factory.CreateClient();
        var email = IdentityApi.UniqueEmail();
        var registration = await client.RegisterNewUserAsync(email);

        using var response = await client.LoginAsync(email.ToUpperInvariant(), IdentityApi.Password);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var session = await response.ReadAsync<AuthResponse>();
        Assert.Equal(registration.UserId, session.UserId);
        Assert.NotEqual(registration.RefreshToken, session.RefreshToken);
    }

    [Fact]
    public async Task Login_answers_alike_for_a_wrong_password_and_an_unknown_email()
    {
        using var client = factory.CreateClient();
        var email = IdentityApi.UniqueEmail();
        await client.RegisterNewUserAsync(email);

        using var wrongPassword = await client.LoginAsync(email, "wrong-password");
        using var unknownEmail = await client.LoginAsync(IdentityApi.UniqueEmail(), IdentityApi.Password);

        Assert.Equal(HttpStatusCode.Unauthorized, wrongPassword.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, unknownEmail.StatusCode);
        var first = await wrongPassword.ReadAsync<ProblemDetails>();
        var second = await unknownEmail.ReadAsync<ProblemDetails>();
        Assert.Equal(first.Detail, second.Detail);
    }

    [Fact]
    public async Task Fifth_wrong_password_locks_the_account_even_for_the_right_password()
    {
        using var client = factory.CreateClient();
        var email = IdentityApi.UniqueEmail();
        await client.RegisterNewUserAsync(email);

        for (var attempt = 1; attempt <= 5; attempt++)
        {
            using var failed = await client.LoginAsync(email, $"wrong-password-{attempt}");
            Assert.Equal(HttpStatusCode.Unauthorized, failed.StatusCode);
        }

        using var locked = await client.LoginAsync(email, IdentityApi.Password);
        Assert.Equal(HttpStatusCode.TooManyRequests, locked.StatusCode);
    }

    [Fact]
    public async Task Parallel_wrong_passwords_are_all_counted()
    {
        using var client = factory.CreateClient();
        var email = IdentityApi.UniqueEmail();
        await client.RegisterNewUserAsync(email);

        var responses = await Task.WhenAll(
            Enumerable.Range(1, 5).Select(attempt => client.LoginAsync(email, $"wrong-password-{attempt}")));
        try
        {
            Assert.All(responses, response => Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode));
        }
        finally
        {
            Array.ForEach(responses, response => response.Dispose());
        }

        using var locked = await client.LoginAsync(email, IdentityApi.Password);
        Assert.Equal(HttpStatusCode.TooManyRequests, locked.StatusCode);
    }

    [Fact]
    public async Task Profile_requires_an_access_token_signed_by_this_api()
    {
        using var client = factory.CreateClient();

        using var anonymous = await client.GetProfileAsync(accessToken: null);
        using var malformed = await client.GetProfileAsync("not-a-jwt");
        using var forged = await client.GetProfileAsync(ForgeAccessToken());

        Assert.Equal(HttpStatusCode.Unauthorized, anonymous.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, malformed.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, forged.StatusCode);
    }

    /// <summary>Issuer va audience toʻgʻri, lekin token boshqa kalit bilan imzolangan.</summary>
    private static string ForgeAccessToken() => new JsonWebTokenHandler().CreateToken(new SecurityTokenDescriptor
    {
        Issuer = "english-school",
        Audience = "english-school",
        Expires = DateTime.UtcNow.AddMinutes(5),
        Claims = new Dictionary<string, object> { [JwtRegisteredClaimNames.Sub] = Guid.NewGuid().ToString() },
        SigningCredentials = new SigningCredentials(
            new SymmetricSecurityKey(RandomNumberGenerator.GetBytes(32)), SecurityAlgorithms.HmacSha256),
    });
}
