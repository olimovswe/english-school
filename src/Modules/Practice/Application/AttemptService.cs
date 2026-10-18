using EnglishSchool.Modules.Practice.Domain;
using EnglishSchool.Modules.Practice.Grading;
using EnglishSchool.Modules.Practice.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace EnglishSchool.Modules.Practice.Application;

/// <summary>
/// Javobni qabul qiladi, baholash quvuridan oʻtkazadi va natijani saqlaydi. Urinish, takrorlanuvchi xatolar
/// hisobi va eventlar (<c>AttemptEvaluated</c>, <c>ErrorDetected</c>) bitta tranzaksiyada yoziladi.
/// </summary>
internal sealed class AttemptService(PracticeDbContext db, GradingPipeline grading, TimeProvider clock)
{
    private const int MaxStoredAnswerLength = 500;

    public async Task<SubmitAttemptResult> SubmitAsync(Guid userId, SubmitAttemptRequest request, CancellationToken cancellationToken)
    {
        var exercise = await db.Exercises
            .AsNoTracking()
            .Include(e => e.Options)
            .SingleOrDefaultAsync(e => e.Id == request.ExerciseId, cancellationToken);

        if (exercise is null)
        {
            return new SubmitAttemptResult(SubmitStatus.ExerciseNotFound);
        }

        if (AttemptValidation.ValidateAnswer(exercise, request) is { Count: > 0 } errors)
        {
            return new SubmitAttemptResult(SubmitStatus.Invalid, Errors: errors);
        }

        var answer = new SubmittedAnswer(
            request.Answer,
            request.SelectedOptionId,
            [.. (request.Matches ?? []).Select(m => new SubmittedMatch(m.OptionId, m.MatchText ?? string.Empty))]);
        var outcome = await grading.GradeAsync(new GradingContext(userId, exercise, answer), cancellationToken);

        var result = await RecordAsync(userId, exercise, request, outcome, retryOnConflict: true, cancellationToken);
        return new SubmitAttemptResult(SubmitStatus.Evaluated, result);
    }

    private async Task<AttemptResultResponse> RecordAsync(
        Guid userId,
        Exercise exercise,
        SubmitAttemptRequest request,
        GradingOutcome outcome,
        bool retryOnConflict,
        CancellationToken cancellationToken)
    {
        var now = clock.GetUtcNow();
        var detected = outcome.Feedback
            .Where(f => f.ErrorCode is not null && ErrorCodes.IsKnown(f.ErrorCode))
            .Select(f => new DetectedError(f.ErrorCode!, f.Correction))
            .DistinctBy(e => e.ErrorCode)
            .ToList();

        var attempt = Attempt.Record(
            userId, exercise.Id, exercise.DayNumber, DescribeAnswer(exercise, request), outcome.Verdict, outcome.Score, outcome.Tier,
            request.TimeTakenMs ?? 0, detected, now);
        db.Attempts.Add(attempt);
        await TrackErrorsAsync(userId, attempt.Id, detected, now, cancellationToken);

        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (retryOnConflict && IsUniqueViolation(exception))
        {
            // Bir oʻquvchining ikki javobi bir vaqtda yangi xato kodini yaratmoqchi boʻldi: qayta oʻqib, hisoblagichni oshiramiz.
            db.ChangeTracker.Clear();
            return await RecordAsync(userId, exercise, request, outcome, retryOnConflict: false, cancellationToken);
        }

        return new AttemptResultResponse(
            attempt.Id,
            outcome.Verdict.ToString(),
            outcome.Score,
            outcome.Tier,
            [.. outcome.Feedback.Select(f => new FeedbackDto(f.ErrorCode, f.Correction, f.Explanation))]);
    }

    private async Task TrackErrorsAsync(
        Guid userId, Guid attemptId, List<DetectedError> detected, DateTimeOffset now, CancellationToken cancellationToken)
    {
        if (detected.Count == 0)
        {
            return;
        }

        var codes = detected.Select(e => e.ErrorCode).ToList();
        var known = await db.UserErrors
            .Where(e => e.UserId == userId && codes.Contains(e.ErrorCode))
            .ToDictionaryAsync(e => e.ErrorCode, StringComparer.Ordinal, cancellationToken);

        foreach (var code in codes)
        {
            if (known.TryGetValue(code, out var existing))
            {
                existing.Recur(attemptId, now);
            }
            else
            {
                db.UserErrors.Add(UserError.FirstSeen(userId, code, attemptId, now));
            }
        }
    }

    /// <summary>Tahlil uchun javobning oʻqiladigan koʻrinishi.</summary>
    private static string DescribeAnswer(Exercise exercise, SubmitAttemptRequest request)
    {
        var text = exercise.Type switch
        {
            ExerciseType.MultipleChoice => exercise.Options.Single(o => o.Id == request.SelectedOptionId).Text,
            ExerciseType.Matching => string.Join("; ", (request.Matches ?? [])
                .Select(m => $"{exercise.Options.Single(o => o.Id == m.OptionId).Text} — {m.MatchText?.Trim()}")),
            _ => request.Answer?.Trim() ?? string.Empty,
        };

        return text.Length <= MaxStoredAnswerLength ? text : text[..MaxStoredAnswerLength];
    }

    private static bool IsUniqueViolation(DbUpdateException exception) =>
        exception.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation };
}
