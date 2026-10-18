using EnglishSchool.Modules.Identity.Security;
using EnglishSchool.Modules.Identity.Tests.TestSupport;
using Microsoft.Extensions.Options;

namespace EnglishSchool.Modules.Identity.Tests.Security;

public sealed class TelegramLoginVerifierTests
{
    private const string BotToken = "123456:unit-test-bot";
    private static readonly DateTimeOffset Now = DateTimeOffset.FromUnixTimeSeconds(1_800_000_000);

    [Fact]
    public void Accepts_a_payload_signed_with_the_bot_token()
    {
        var payload = TelegramWidget.Sign(BotToken, 42, Now.AddMinutes(-2));

        Assert.True(CreateVerifier().Verify(payload, Now));
    }

    [Fact]
    public void Rejects_a_payload_signed_by_another_bot()
    {
        var payload = TelegramWidget.Sign("654321:another-bot", 42, Now);

        Assert.False(CreateVerifier().Verify(payload, Now));
    }

    [Fact]
    public void Rejects_a_payload_with_any_field_changed()
    {
        var payload = TelegramWidget.Sign(BotToken, 42, Now);
        var verifier = CreateVerifier();

        Assert.False(verifier.Verify(payload with { Id = 43 }, Now));
        Assert.False(verifier.Verify(payload with { FirstName = "Vali" }, Now));
        Assert.False(verifier.Verify(payload with { PhotoUrl = "https://t.me/i/userpic/320/ali.jpg" }, Now));
    }

    [Theory]
    [InlineData(-11)]
    [InlineData(2)]
    public void Rejects_a_payload_outside_the_accepted_time_window(int minutesFromNow)
    {
        var payload = TelegramWidget.Sign(BotToken, 42, Now.AddMinutes(minutesFromNow));

        Assert.False(CreateVerifier().Verify(payload, Now));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("abc")]
    [InlineData("zzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzz")]
    public void Rejects_a_malformed_hash(string? hash)
    {
        var payload = TelegramWidget.Sign(BotToken, 42, Now) with { Hash = hash };

        Assert.False(CreateVerifier().Verify(payload, Now));
    }

    [Fact]
    public void Is_disabled_when_no_bot_token_is_configured()
    {
        var verifier = CreateVerifier(botToken: string.Empty);

        Assert.False(verifier.IsConfigured);
        Assert.False(verifier.Verify(TelegramWidget.Sign(string.Empty, 42, Now), Now));
    }

    [Fact]
    public void Data_check_string_sorts_fields_and_skips_missing_ones()
    {
        var payload = new TelegramAuthData(42, "Ali", null, "ali_valiyev", null, 1_700_000_000, Hash: "ignored");

        Assert.Equal(
            "auth_date=1700000000\nfirst_name=Ali\nid=42\nusername=ali_valiyev",
            TelegramLoginVerifier.BuildDataCheckString(payload));
    }

    private static TelegramLoginVerifier CreateVerifier(string botToken = BotToken) =>
        new(Options.Create(new TelegramLoginOptions { BotToken = botToken, MaxAuthAge = TimeSpan.FromMinutes(10) }));
}
