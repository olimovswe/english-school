using System.Security.Claims;
using EnglishSchool.BuildingBlocks.Security;
using EnglishSchool.Modules.Curriculum.Contracts;
using EnglishSchool.Modules.Practice.Application;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;

namespace EnglishSchool.Modules.Practice.Endpoints;

/// <summary><c>/api/v1/practice</c>: kun mashqlari va javob yuborish. Hammasi faqat kirgan oʻquvchi uchun.</summary>
internal static class PracticeEndpoints
{
    public static void MapPracticeEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/practice").RequireAuthorization();

        group.MapGet("/days/{dayNumber:int}/exercises", GetExercisesAsync);
        group.MapPost("/attempts", SubmitAttemptAsync);
    }

    /// <summary>Kurs kuni Curriculum'dan (Contracts orqali) tekshiriladi: kursda yoʻq kun — 404, mashqsiz kun — boʻsh roʻyxat.</summary>
    private static async Task<Results<Ok<IReadOnlyList<ExerciseDto>>, NotFound>> GetExercisesAsync(
        int dayNumber, ICurriculumQueries curriculum, ExerciseQueries exercises, CancellationToken cancellationToken)
    {
        if (await curriculum.GetDayAsync(dayNumber, cancellationToken) is null)
        {
            return TypedResults.NotFound();
        }

        return TypedResults.Ok(await exercises.GetForDayAsync(dayNumber, cancellationToken));
    }

    private static async Task<Results<Ok<AttemptResultResponse>, ValidationProblem, NotFound, UnauthorizedHttpResult>> SubmitAttemptAsync(
        SubmitAttemptRequest request, ClaimsPrincipal principal, AttemptService attempts, CancellationToken cancellationToken)
    {
        if (!principal.TryGetUserId(out var userId))
        {
            return TypedResults.Unauthorized();
        }

        if (AttemptValidation.ValidateRequest(request) is { Count: > 0 } errors)
        {
            return TypedResults.ValidationProblem(errors);
        }

        var result = await attempts.SubmitAsync(userId, request, cancellationToken);
        return result.Status switch
        {
            SubmitStatus.ExerciseNotFound => TypedResults.NotFound(),
            SubmitStatus.Invalid => TypedResults.ValidationProblem(result.Errors ?? []),
            _ => TypedResults.Ok(result.Result),
        };
    }
}
