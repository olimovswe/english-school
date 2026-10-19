using EnglishSchool.BuildingBlocks.Caching;
using EnglishSchool.BuildingBlocks.Health;
using EnglishSchool.BuildingBlocks.Hosting;
using EnglishSchool.Host.Api.Edge;
using EnglishSchool.Host.Api.Health;

namespace EnglishSchool.Host.Api;

/// <summary>API'ning composition root'i: servislar, HTTP pipeline va endpointlar. <c>Program</c> faqat ishga tushiradi.</summary>
internal static class ApiHost
{
    public static WebApplication Build(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Services.AddGracefulShutdown(builder.Configuration);
        builder.Services.AddProblemDetails();
        builder.Services.AddExceptionHandler<ApiExceptionHandler>();
        builder.Services.AddHealthChecks().AddCheck<ShutdownHealthCheck>("shutdown", tags: [HealthCheckTags.Ready]);
        builder.Services.AddHybridCaching(builder.Configuration);
        builder.AddEdgeProtection();

        // Autentifikatsiya sxemasini (JWT) Identity moduli qoʻshadi; authorization HTTP pipeline'ga tegishli.
        builder.Services.AddAuthorization();

        foreach (var module in ModuleCatalog.All)
        {
            module.AddServices(builder.Services, builder.Configuration);
        }

        var app = builder.Build();

        app.UseExceptionHandler();
        app.UseStatusCodePages();

        // CORS autentifikatsiyadan oldin (preflight soʻrovida token yoʻq), soʻrovlar chegarasi esa undan keyin.
        app.UseCors();
        app.UseAuthentication();
        app.UseConfiguredRateLimiter();
        app.UseAuthorization();

        app.MapApiHealthChecks();
        app.MapGet("/", () => TypedResults.Ok(new
        {
            Name = "English School API",
            Modules = ModuleCatalog.All.Select(m => m.Name),
        }));

        var api = app.MapGroup("/api/v1");
        foreach (var module in ModuleCatalog.All)
        {
            module.MapEndpoints(api);
        }

        return app;
    }
}
