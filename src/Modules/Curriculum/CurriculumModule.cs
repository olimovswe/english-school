using EnglishSchool.BuildingBlocks.Modules;
using EnglishSchool.Modules.Discipline.Contracts;
using EnglishSchool.Modules.Curriculum.Application;
using EnglishSchool.Modules.Curriculum.Contracts;
using EnglishSchool.Modules.Curriculum.Infrastructure;
using EnglishSchool.Modules.Curriculum.Seed;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace EnglishSchool.Modules.Curriculum;

/// <summary>
/// Curriculum Engine: kurs → hafta → kun tuzilmasi, grammatika mavzulari, soʻzlar va iboralar.
/// </summary>
public sealed class CurriculumModule : IModule
{
    public string Name => "Curriculum";

    public void AddServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<CurriculumOptions>().Bind(configuration.GetSection(CurriculumOptions.Section));

        services.AddDbContext<CurriculumDbContext>(options => options
            .UseNpgsql(
                configuration.GetConnectionString("Postgres"),
                npgsql => npgsql.MigrationsHistoryTable("__ef_migrations_history", CurriculumDbContext.Schema))
            .UseSnakeCaseNamingConvention());

        services.AddScoped<ICurriculumQueries, CurriculumQueries>();
        services.AddHostedService<CurriculumSeeder>();
        services.AddHealthChecks().AddDbContextCheck<CurriculumDbContext>("curriculum-db");
    }

    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/curriculum");

        group.MapGet("/days", async (ICurriculumQueries queries, CancellationToken ct) =>
            TypedResults.Ok(await queries.GetDaysAsync(ct)));

        group.MapGet("/days/{dayNumber:int}", async Task<Results<Ok<DayDto>, NotFound>> (
            int dayNumber, ICurriculumQueries queries, CancellationToken ct) =>
            await queries.GetDayAsync(dayNumber, ct) is { } day
                ? TypedResults.Ok(day)
                : TypedResults.NotFound())
            .RequireAuthorization(DisciplinePolicies.DailyLessonAccess);
    }
}
