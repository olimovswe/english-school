using System.Net;
using EnglishSchool.Modules.Identity.Application;

namespace EnglishSchool.Modules.Identity.Tests.Api;

[Collection(IdentityApiFactory.CollectionName)]
public sealed class SessionTests(IdentityApiFactory factory)
{
    [Fact]
    public async Task Refresh_rotates_the_refresh_token_and_issues_a_working_access_token()
    {
        using var client = factory.CreateClient();
        var registration = await client.RegisterNewUserAsync();

        var refreshed = await client.RefreshSuccessfullyAsync(registration.RefreshToken);

        Assert.Equal(registration.UserId, refreshed.UserId);
        Assert.NotEqual(registration.RefreshToken, refreshed.RefreshToken);
        using var profile = await client.GetProfileAsync(refreshed.AccessToken);
        Assert.Equal(HttpStatusCode.OK, profile.StatusCode);
    }

    [Fact]
    public async Task Reusing_a_rotated_refresh_token_closes_that_session_but_not_others()
    {
        using var client = factory.CreateClient();
        var email = IdentityApi.UniqueEmail();
        var laptop = await client.RegisterNewUserAsync(email);
        var phone = await client.LoginSuccessfullyAsync(email);

        var rotated = await client.RefreshSuccessfullyAsync(laptop.RefreshToken);
        using var replay = await client.RefreshAsync(laptop.RefreshToken);
        using var afterReplay = await client.RefreshAsync(rotated.RefreshToken);
        using var otherSession = await client.RefreshAsync(phone.RefreshToken);

        Assert.Equal(HttpStatusCode.Unauthorized, replay.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, afterReplay.StatusCode);
        Assert.Equal(HttpStatusCode.OK, otherSession.StatusCode);
    }

    [Fact]
    public async Task Concurrent_refreshes_redeem_a_token_only_once_and_close_the_session()
    {
        using var client = factory.CreateClient();
        var registration = await client.RegisterNewUserAsync();

        var responses = await Task.WhenAll(
            client.RefreshAsync(registration.RefreshToken),
            client.RefreshAsync(registration.RefreshToken));
        try
        {
            var winner = Assert.Single(responses, response => response.StatusCode == HttpStatusCode.OK);
            Assert.Single(responses, response => response.StatusCode == HttpStatusCode.Unauthorized);

            // Ikkinchi soʻrov tokenni qayta ishlatish deb topiladi: gʻolib olgan token ham bekor boʻladi.
            var issued = await winner.ReadAsync<AuthResponse>();
            using var refreshAgain = await client.RefreshAsync(issued.RefreshToken);
            Assert.Equal(HttpStatusCode.Unauthorized, refreshAgain.StatusCode);
        }
        finally
        {
            Array.ForEach(responses, response => response.Dispose());
        }
    }

    [Fact]
    public async Task Logout_closes_the_session()
    {
        using var client = factory.CreateClient();
        var registration = await client.RegisterNewUserAsync();

        using var logout = await client.LogoutAsync(registration.RefreshToken);
        using var refresh = await client.RefreshAsync(registration.RefreshToken);

        Assert.Equal(HttpStatusCode.NoContent, logout.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, refresh.StatusCode);
    }

    [Fact]
    public async Task Unknown_refresh_token_is_rejected_and_logout_stays_idempotent()
    {
        using var client = factory.CreateClient();
        const string neverIssued = "this-token-was-never-issued";

        using var refresh = await client.RefreshAsync(neverIssued);
        using var logout = await client.LogoutAsync(neverIssued);

        Assert.Equal(HttpStatusCode.Unauthorized, refresh.StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, logout.StatusCode);
    }
}
