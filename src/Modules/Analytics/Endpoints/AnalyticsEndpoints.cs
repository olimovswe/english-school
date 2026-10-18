using System.Security.Claims;
using EnglishSchool.BuildingBlocks.Security;
using EnglishSchool.Modules.Analytics.Contracts;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;

namespace EnglishSchool.Modules.Analytics.Endpoints;

internal static class AnalyticsEndpoints
{
    public static void MapAnalyticsEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/analytics").RequireAuthorization();
        group.MapGet("/progress", GetProgressAsync);
        group.MapGet("/reports/weekly", GetWeeklyReportAsync);
    }

    private static async Task<Results<Ok<ProgressSummary>, UnauthorizedHttpResult>> GetProgressAsync(
        ClaimsPrincipal principal, IProgressQueries queries, CancellationToken cancellationToken)
    {
        if (!principal.TryGetUserId(out var userId))
        {
            return TypedResults.Unauthorized();
        }

        return TypedResults.Ok(await queries.GetSummaryAsync(userId, cancellationToken));
    }

    private static async Task<Results<Ok<WeeklyPerformanceReport>, UnauthorizedHttpResult>> GetWeeklyReportAsync(
        ClaimsPrincipal principal, IProgressQueries queries, CancellationToken cancellationToken)
    {
        if (!principal.TryGetUserId(out var userId))
        {
            return TypedResults.Unauthorized();
        }

        return TypedResults.Ok(await queries.GetWeeklyReportAsync(userId, cancellationToken));
    }
}