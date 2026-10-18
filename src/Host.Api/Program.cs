using EnglishSchool.BuildingBlocks.Caching;
using EnglishSchool.Host.Api;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddProblemDetails();
builder.Services.AddHealthChecks();
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

app.MapHealthChecks("/health");
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

await app.RunAsync();
