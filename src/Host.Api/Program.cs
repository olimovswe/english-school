using EnglishSchool.BuildingBlocks.Caching;
using EnglishSchool.BuildingBlocks.Health;
using EnglishSchool.BuildingBlocks.Hosting;
using EnglishSchool.Host.Api;
using EnglishSchool.Host.Api.Health;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddGracefulShutdown(builder.Configuration);
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<ApiExceptionHandler>();
builder.Services.AddHealthChecks().AddCheck<ShutdownHealthCheck>("shutdown", tags: [HealthCheckTags.Ready]);
builder.Services.AddHybridCaching(builder.Configuration);

// Autentifikatsiya sxemasini (JWT) Identity moduli qoʻshadi; authorization HTTP pipeline'ga tegishli.
builder.Services.AddAuthorization();

foreach (var module in ModuleCatalog.All)
{
    module.AddServices(builder.Services, builder.Configuration);
}

var app = builder.Build();

app.UseExceptionHandler();
app.UseStatusCodePages();
app.UseAuthentication();
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

try
{
    await app.RunAsync();
}
catch (Exception exception) when (exception is not HostAbortedException)
{
    // Masalan, sozlama tekshiruvi (ValidateOnStart) yoki port band: tuzilgan log va 1 kodi bilan chiqish.
    ApiHostLog.Crashed(app.Logger, exception);
    return 1;
}

return 0;
