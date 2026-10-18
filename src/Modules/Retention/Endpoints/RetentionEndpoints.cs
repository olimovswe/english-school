using System.Security.Claims;
using EnglishSchool.BuildingBlocks.Security;
using EnglishSchool.Modules.Retention.Application;
using EnglishSchool.Modules.Retention.Domain;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;

namespace EnglishSchool.Modules.Retention.Endpoints;

/// <summary><c>/api/v1/retention</c>: navbatdagi kartochkalar va baholash. Hammasi faqat kirgan oʻquvchi uchun.</summary>
internal static class RetentionEndpoints
{
    private const int DefaultLimit = 20;
    private const int MaxLimit = 100;
    private const int MaxDurationMs = 60 * 60 * 1000;

    public static void MapRetentionEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/retention").RequireAuthorization();

        group.MapGet("/due", GetDueAsync);
        group.MapPost("/review", ReviewAsync);
    }

    private static async Task<Results<Ok<DueCardsResponse>, ValidationProblem, UnauthorizedHttpResult>> GetDueAsync(
        int? limit, ClaimsPrincipal principal, DueCardQueries queries, CancellationToken cancellationToken)
    {
        if (!principal.TryGetUserId(out var userId))
        {
            return TypedResults.Unauthorized();
        }

        if (limit is not null and not (>= 1 and <= MaxLimit))
        {
            return TypedResults.ValidationProblem(new Dictionary<string, string[]>
            {
                ["limit"] = [$"limit 1 va {MaxLimit} oraligʻida boʻlishi kerak."],
            });
        }

        return TypedResults.Ok(await queries.GetDueAsync(userId, limit ?? DefaultLimit, cancellationToken));
    }

    private static async Task<Results<Ok<ReviewResultResponse>, ValidationProblem, NotFound, ProblemHttpResult, UnauthorizedHttpResult>> ReviewAsync(
        ReviewCardRequest request, ClaimsPrincipal principal, ReviewService reviews, CancellationToken cancellationToken)
    {
        if (!principal.TryGetUserId(out var userId))
        {
            return TypedResults.Unauthorized();
        }

        if (Validate(request) is { Count: > 0 } errors)
        {
            return TypedResults.ValidationProblem(errors);
        }

        var outcome = await reviews.ReviewAsync(userId, request.CardId!.Value, (Rating)request.Rating!.Value, request.DurationMs, cancellationToken);
        return outcome.Status switch
        {
            ReviewStatus.CardNotFound => TypedResults.NotFound(),
            ReviewStatus.Conflict => TypedResults.Problem(
                detail: "Bu kartochka hozirgina baholandi. Roʻyxatni yangilang.", statusCode: StatusCodes.Status409Conflict),
            _ => TypedResults.Ok(outcome.Result),
        };
    }

    private static Dictionary<string, string[]> Validate(ReviewCardRequest request)
    {
        var errors = new Dictionary<string, string[]>();

        if (request.CardId is null || request.CardId == Guid.Empty)
        {
            errors["cardId"] = ["Kartochka tanlanmagan."];
        }

        if (request.Rating is not (>= (int)Rating.Again and <= (int)Rating.Easy))
        {
            errors["rating"] = ["Baho 1 (Again), 2 (Hard), 3 (Good) yoki 4 (Easy) boʻlishi kerak."];
        }

        if (request.DurationMs is not null and not (>= 0 and <= MaxDurationMs))
        {
            errors["durationMs"] = ["Sarflangan vaqt 0 va 1 soat oraligʻida (millisekundda) boʻlishi kerak."];
        }

        return errors;
    }
}
