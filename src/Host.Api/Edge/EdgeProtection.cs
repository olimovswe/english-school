using System.Globalization;
using System.Threading.RateLimiting;
using EnglishSchool.BuildingBlocks.Security;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Net.Http.Headers;

namespace EnglishSchool.Host.Api.Edge;

/// <summary>
/// API chegarasidagi himoya: CORS (faqat roʻyxatdagi brauzer ilovalari) va soʻrovlar chegarasi. Ikkalasi ham
/// HTTP'ga xos, shuning uchun modullarda emas, host'da: modullar endpointlari oʻzgarmaydi.
/// </summary>
internal static class EdgeProtection
{
    private static readonly TimeSpan Window = TimeSpan.FromMinutes(1);
    private const int SegmentsPerWindow = 6;

    private static readonly string[] ExposedHeaders =
        [HeaderNames.Location, HeaderNames.RetryAfter, HeaderNames.ETag, HeaderNames.ContentRange, HeaderNames.AcceptRanges];

    public static WebApplicationBuilder AddEdgeProtection(this WebApplicationBuilder builder)
    {
        var origins = AllowedOrigins(builder.Configuration.GetSection("Cors:AllowedOrigins"));
        builder.Services.AddCors(options => options.AddDefaultPolicy(policy =>
        {
            // Roʻyxat boʻsh boʻlsa siyosatda birorta ham manba yoʻq: brauzer boshqa domendan murojaat qila olmaydi.
            if (origins.Length > 0)
            {
                policy.WithOrigins(origins)
                    .AllowAnyHeader()
                    .AllowAnyMethod()
                    .WithExposedHeaders(ExposedHeaders)
                    .SetPreflightMaxAge(TimeSpan.FromMinutes(10));
            }
        }));

        var limits = builder.Configuration.GetSection(RateLimitingSettings.Section).Get<RateLimitingSettings>() ?? new RateLimitingSettings();
        limits.Validate();
        builder.Services.AddSingleton(limits);
        if (limits.Enabled)
        {
            builder.Services.AddRateLimiter(options =>
            {
                options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context => Partition(context, limits));
                options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
                options.OnRejected = RejectAsync;
            });
        }

        return builder;
    }

    /// <summary>Autentifikatsiyadan keyin chaqiriladi: chegara kirgan foydalanuvchi boʻyicha hisoblanadi.</summary>
    public static void UseConfiguredRateLimiter(this WebApplication app)
    {
        if (app.Services.GetRequiredService<RateLimitingSettings>().Enabled)
        {
            app.UseRateLimiter();
        }
    }

    /// <summary>
    /// Har bir chegara alohida hisoblanadi: kirish soʻrovlari IP boʻyicha (parol tanlashga qarshi), qolganlari kirgan
    /// foydalanuvchi boʻyicha (bitta NAT ortidagi sinf bir-biriga xalaqit bermaydi). Health va bosh sahifa cheklanmaydi.
    /// </summary>
    private static RateLimitPartition<string> Partition(HttpContext context, RateLimitingSettings limits)
    {
        var path = context.Request.Path;
        if (!path.StartsWithSegments("/api", StringComparison.OrdinalIgnoreCase))
        {
            return RateLimitPartition.GetNoLimiter("unlimited");
        }

        var ip = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var (policy, permits, client) = path switch
        {
            _ when HttpMethods.IsPost(context.Request.Method) && path.StartsWithSegments("/api/v1/identity", StringComparison.OrdinalIgnoreCase) =>
                ("authentication", limits.AuthenticationPermitsPerMinute, $"ip:{ip}"),
            _ when path.StartsWithSegments("/api/v1/speech/assess", StringComparison.OrdinalIgnoreCase) =>
                ("speech", limits.SpeechPermitsPerMinute, ClientKey(context, ip)),
            _ => ("api", limits.PermitsPerMinute, ClientKey(context, ip)),
        };

        return RateLimitPartition.GetSlidingWindowLimiter($"{policy}:{client}", _ => new SlidingWindowRateLimiterOptions
        {
            PermitLimit = permits,
            Window = Window,
            SegmentsPerWindow = SegmentsPerWindow,
            QueueLimit = 0,
        });
    }

    private static string ClientKey(HttpContext context, string ip) =>
        context.User.TryGetUserId(out var userId) ? $"user:{userId:N}" : $"ip:{ip}";

    private static async ValueTask RejectAsync(OnRejectedContext context, CancellationToken cancellationToken)
    {
        // Sirpanuvchi oyna RetryAfter bermaydi: eng uzoq kutish — bitta segment (10 s), shunda eng eski soʻrov oynadan chiqadi.
        var wait = context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter) ? retryAfter : Window / SegmentsPerWindow;
        var seconds = Math.Max(1, (int)Math.Ceiling(wait.TotalSeconds));
        context.HttpContext.Response.Headers.RetryAfter = seconds.ToString(CultureInfo.InvariantCulture);

        await context.HttpContext.RequestServices.GetRequiredService<IProblemDetailsService>().WriteAsync(new ProblemDetailsContext
        {
            HttpContext = context.HttpContext,
            ProblemDetails =
            {
                Status = StatusCodes.Status429TooManyRequests,
                Title = "Juda koʻp soʻrov",
                Detail = $"Soʻrovlar chegarasiga yetdingiz. {seconds} soniyadan keyin qayta urinib koʻring.",
            },
        });
    }

    /// <summary>
    /// <c>Cors:AllowedOrigins</c>: massiv yoki vergul bilan ajratilgan qator (muhit oʻzgaruvchisi uchun qulay:
    /// <c>Cors__AllowedOrigins__0=https://app.example.uz,https://admin.example.uz</c>). Har biri toʻliq http(s) manba.
    /// </summary>
    private static string[] AllowedOrigins(IConfigurationSection section)
    {
        var origins = (section.Get<string[]>() ?? [])
            .SelectMany(value => value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        foreach (var origin in origins)
        {
            if (!Uri.TryCreate(origin, UriKind.Absolute, out var uri) || uri.Scheme is not ("http" or "https") ||
                uri.PathAndQuery != "/" || origin.EndsWith('/'))
            {
                throw new InvalidOperationException(
                    $"Cors:AllowedOrigins: «{origin}» manba emas. Yoʻlsiz toʻliq manzil yozing (https://app.example.uz); «*» ruxsat etilmaydi.");
            }
        }

        return origins;
    }
}
