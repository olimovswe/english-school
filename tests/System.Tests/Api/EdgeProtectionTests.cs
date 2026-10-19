using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using EnglishSchool.SystemTests.TestSupport;
using Microsoft.AspNetCore.Hosting;

namespace EnglishSchool.SystemTests.Api;

[Collection(SystemFixture.CollectionName)]
public sealed class EdgeProtectionTests(SystemFixture system)
{
    private const string App = "https://app.example.uz";

    [Fact]
    public async Task Cors_lets_only_the_configured_browser_apps_call_the_api()
    {
        await using var api = system.Api.WithWebHostBuilder(builder =>
            builder.UseSetting("Cors:AllowedOrigins:0", $"{App}, https://admin.example.uz"));
        using var client = api.CreateClient();

        using var preflight = await client.SendAsync(Preflight(App));
        using var foreign = await client.SendAsync(Preflight("https://evil.example"));
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/curriculum/days") { Headers = { { "Origin", App } } };
        using var simple = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.NoContent, preflight.StatusCode);
        Assert.Equal(App, preflight.Headers.GetValues("Access-Control-Allow-Origin").Single());
        Assert.Contains("authorization", preflight.Headers.GetValues("Access-Control-Allow-Headers").Single(), StringComparison.OrdinalIgnoreCase);
        Assert.False(foreign.Headers.Contains("Access-Control-Allow-Origin"));
        Assert.Equal(App, simple.Headers.GetValues("Access-Control-Allow-Origin").Single());
        Assert.Contains("Retry-After", simple.Headers.GetValues("Access-Control-Expose-Headers").Single(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task Without_configured_origins_no_browser_app_may_call_the_api()
    {
        using var client = system.Api.CreateClient();

        using var preflight = await client.SendAsync(Preflight(App));

        Assert.False(preflight.Headers.Contains("Access-Control-Allow-Origin"));
    }

    [Theory]
    [InlineData("*")]
    [InlineData("https://app.example.uz/")]
    [InlineData("app.example.uz")]
    public async Task A_wildcard_or_malformed_origin_stops_the_api_with_the_reason_logged(string origin)
    {
        // Host qurilmay qolganda Program sababni konsolga Critical darajada yozadi va 1 kodi bilan chiqadi.
        using var console = new StringWriter();
        var original = Console.Out;
        Console.SetOut(console);
        try
        {
            await using var api = system.Api.WithWebHostBuilder(builder => builder.UseSetting("Cors:AllowedOrigins:0", origin));
            Assert.ThrowsAny<InvalidOperationException>(() => api.CreateClient());
        }
        finally
        {
            Console.SetOut(original);
        }

        var log = console.ToString();
        Assert.Contains("crit: EnglishSchool.Host.Api", log, StringComparison.Ordinal);
        Assert.Contains($"Cors:AllowedOrigins: «{origin}» manba emas.", log, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Sign_in_attempts_are_limited_per_client_with_retry_after_while_health_is_never_limited()
    {
        await using var api = system.Api.WithWebHostBuilder(builder =>
        {
            builder.UseSetting("RateLimiting:Enabled", "true");
            builder.UseSetting("RateLimiting:AuthenticationPermitsPerMinute", "3");
        });
        using var client = api.CreateClient();

        var attempts = new List<HttpStatusCode>();
        for (var i = 0; i < 4; i++)
        {
            using var response = await client.PostAsJsonAsync("/api/v1/identity/login", new { email = "nobody@example.com", password = "wrong-password-123" });
            attempts.Add(response.StatusCode);
        }

        using var limited = await client.PostAsJsonAsync("/api/v1/identity/login", new { email = "nobody@example.com", password = "wrong-password-123" });
        var problem = await limited.Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal([HttpStatusCode.Unauthorized, HttpStatusCode.Unauthorized, HttpStatusCode.Unauthorized, HttpStatusCode.TooManyRequests], attempts);
        Assert.Equal(HttpStatusCode.TooManyRequests, limited.StatusCode);
        Assert.InRange(int.Parse(limited.Headers.GetValues("Retry-After").Single(), CultureInfo.InvariantCulture), 1, 10);
        Assert.StartsWith("Soʻrovlar chegarasiga yetdingiz.", problem.GetProperty("detail").GetString(), StringComparison.Ordinal);
        for (var i = 0; i < 5; i++)
        {
            using var health = await client.GetAsync(new Uri("/health/ready", UriKind.Relative));
            Assert.Equal(HttpStatusCode.OK, health.StatusCode);
        }
    }

    [Fact]
    public async Task The_general_limit_counts_each_student_separately()
    {
        await using var api = system.Api.WithWebHostBuilder(builder =>
        {
            builder.UseSetting("RateLimiting:Enabled", "true");
            builder.UseSetting("RateLimiting:PermitsPerMinute", "4");
        });
        using var busy = await Student.RegisterAsync(api, "Busy Student");
        using var calm = await Student.RegisterAsync(api, "Calm Student");

        var statuses = new List<HttpStatusCode>();
        for (var i = 0; i < 5; i++)
        {
            statuses.Add(await busy.StatusAsync("/api/v1/retention/due"));
        }

        Assert.Equal([.. Enumerable.Repeat(HttpStatusCode.OK, 4), HttpStatusCode.TooManyRequests], statuses);
        Assert.Equal(HttpStatusCode.OK, await calm.StatusAsync("/api/v1/retention/due"));
    }

    private static HttpRequestMessage Preflight(string origin) => new(HttpMethod.Options, "/api/v1/curriculum/days")
    {
        Headers =
        {
            { "Origin", origin },
            { "Access-Control-Request-Method", "GET" },
            { "Access-Control-Request-Headers", "authorization" },
        },
    };
}
