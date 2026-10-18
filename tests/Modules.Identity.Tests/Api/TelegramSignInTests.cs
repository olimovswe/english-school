using System.Net;
using EnglishSchool.Modules.Identity.Application;
using EnglishSchool.Modules.Identity.Tests.TestSupport;

namespace EnglishSchool.Modules.Identity.Tests.Api;

[Collection(IdentityApiFactory.CollectionName)]
public sealed class TelegramSignInTests(IdentityApiFactory factory)
{
    [Fact]
    public async Task First_sign_in_creates_an_account_and_later_ones_reuse_it()
    {
        using var client = factory.CreateClient();
        var telegramId = TelegramWidget.NewUserId();

        using var first = await client.SignInWithTelegramAsync(
            TelegramWidget.Sign(IdentityApiFactory.TelegramBotToken, telegramId, DateTimeOffset.UtcNow));
        Assert.Equal(HttpStatusCode.Created, first.StatusCode);
        var created = await first.ReadAsync<AuthResponse>();

        using var second = await client.SignInWithTelegramAsync(
            TelegramWidget.Sign(IdentityApiFactory.TelegramBotToken, telegramId, DateTimeOffset.UtcNow));
        Assert.Equal(HttpStatusCode.OK, second.StatusCode);
        Assert.Equal(created.UserId, (await second.ReadAsync<AuthResponse>()).UserId);

        using var profileResponse = await client.GetProfileAsync(created.AccessToken);
        var profile = await profileResponse.ReadAsync<UserProfileResponse>();
        Assert.Equal(telegramId, profile.TelegramId);
        Assert.Equal("Ali Valiyev", profile.DisplayName);
        Assert.Equal("Asia/Tashkent", profile.TimeZone);
        Assert.Null(profile.Email);
    }

    [Fact]
    public async Task Signed_payload_cannot_be_reused_for_another_telegram_id()
    {
        using var client = factory.CreateClient();
        var signed = TelegramWidget.Sign(IdentityApiFactory.TelegramBotToken, TelegramWidget.NewUserId(), DateTimeOffset.UtcNow);

        using var response = await client.SignInWithTelegramAsync(signed with { Id = signed.Id + 1 });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
