using System.Security.Claims;
using EnglishSchool.BuildingBlocks.Security;
using EnglishSchool.Modules.Discipline.Application;
using EnglishSchool.Modules.Retention.Contracts;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;

namespace EnglishSchool.Modules.Discipline.Endpoints;

internal static class DisciplineEndpoints
{
    private const int MaxAnswers = 200;
    private const int MaxAnswerLength = 500;
    private const int MaxFocusSeconds = 24 * 60 * 60;

    public static void MapDisciplineEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/discipline").RequireAuthorization();
        group.MapGet("/today", GetTodayAsync);
        group.MapPost("/gate/submit", SubmitGateAsync);
        group.MapPost("/sessions/start", StartSessionAsync);
        group.MapPost("/sessions/complete", CompleteSessionAsync);
    }

    private static async Task<Results<Ok<TodayPlanResponse>, NotFound, UnauthorizedHttpResult>> GetTodayAsync(
        ClaimsPrincipal principal, DisciplineService discipline, CancellationToken cancellationToken)
    {
        if (!principal.TryGetUserId(out var userId))
        {
            return TypedResults.Unauthorized();
        }

        var response = await discipline.GetTodayAsync(userId, cancellationToken);
        return response is null ? TypedResults.NotFound() : TypedResults.Ok(response);
    }

    private static async Task<Results<Ok<GateSubmissionResponse>, ValidationProblem, NotFound, UnauthorizedHttpResult>> SubmitGateAsync(
        SubmitGateRequest request, ClaimsPrincipal principal, DisciplineService discipline, CancellationToken cancellationToken)
    {
        if (!principal.TryGetUserId(out var userId))
        {
            return TypedResults.Unauthorized();
        }

        if (request.Answers is null || request.Answers.Count > MaxAnswers || request.Answers.Any(answer =>
                answer.CardId == Guid.Empty || answer.Answer is null || answer.Answer.Length > MaxAnswerLength))
        {
            return TypedResults.ValidationProblem(new Dictionary<string, string[]>
            {
                ["answers"] = [$"Answers must contain at most {MaxAnswers} valid card answers, each no longer than {MaxAnswerLength} characters."],
            });
        }

        var response = await discipline.SubmitGateAsync(userId, request.Answers, cancellationToken);
        return response is null ? TypedResults.NotFound() : TypedResults.Ok(response);
    }

    private static async Task<Results<Ok<SessionStartResponse>, Conflict<SessionStartResponse>, NotFound, UnauthorizedHttpResult>> StartSessionAsync(
        ClaimsPrincipal principal, DisciplineService discipline, CancellationToken cancellationToken)
    {
        if (!principal.TryGetUserId(out var userId))
        {
            return TypedResults.Unauthorized();
        }

        var response = await discipline.StartSessionAsync(userId, cancellationToken);
        if (response is null)
        {
            return TypedResults.NotFound();
        }

        return response.SessionId is null ? TypedResults.Conflict(response) : TypedResults.Ok(response);
    }

    private static async Task<Results<Ok<SessionCompletionResponse>, ValidationProblem, NotFound, Conflict<ProblemHttpResult>, UnauthorizedHttpResult>> CompleteSessionAsync(
        CompleteSessionRequest request, ClaimsPrincipal principal, DisciplineService discipline, CancellationToken cancellationToken)
    {
        if (!principal.TryGetUserId(out var userId))
        {
            return TypedResults.Unauthorized();
        }

        var errors = new Dictionary<string, string[]>();
        if (request.SessionId == Guid.Empty)
        {
            errors["sessionId"] = ["A session ID is required."];
        }

        if (request.FocusSeconds is < 0 or > MaxFocusSeconds)
        {
            errors["focusSeconds"] = [$"Focus seconds must be between 0 and {MaxFocusSeconds}."];
        }

        if (request.DistractionCount < 0)
        {
            errors["distractionCount"] = ["Distraction count cannot be negative."];
        }

        if (errors.Count > 0)
        {
            return TypedResults.ValidationProblem(errors);
        }

        try
        {
            var response = await discipline.CompleteSessionAsync(userId, request, cancellationToken);
            return response is null ? TypedResults.NotFound() : TypedResults.Ok(response);
        }
        catch (InvalidOperationException)
        {
            return TypedResults.Conflict(TypedResults.Problem("The session is not in a completable state."));
        }
    }
}

internal sealed record SubmitGateRequest(IReadOnlyList<RetentionGateAnswer>? Answers);