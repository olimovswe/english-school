using EnglishSchool.BuildingBlocks.Abstractions.Events;
using EnglishSchool.Modules.Practice.Contracts.Events;

namespace EnglishSchool.Modules.Practice.Domain;

internal enum Verdict
{
    Correct,
    PartiallyCorrect,
    Incorrect,
}

/// <summary>Javobda aniqlangan, taksonomiyadagi xato.</summary>
internal sealed record DetectedError(string ErrorCode, string Correction);

/// <summary>
/// Oʻquvchining bitta javobi va uning bahosi. Yaratilganda <see cref="AttemptEvaluated"/> va har bir xato uchun
/// <see cref="ErrorDetected"/> chiqaradi: ular urinish bilan bitta tranzaksiyada <c>practice.outbox_messages</c>'ga yoziladi.
/// </summary>
internal sealed class Attempt : IntegrationEventSource
{
    private Attempt()
    {
    }

    public Guid Id { get; private init; } = Guid.CreateVersion7();

    /// <summary>Identity'dagi foydalanuvchi. Boshqa modul jadvaliga FK yoʻq: modullar bazada ham ajratilgan.</summary>
    public Guid UserId { get; private init; }

    public Guid ExerciseId { get; private init; }

    public string Answer { get; private init; } = string.Empty;

    public Verdict Verdict { get; private init; }

    public decimal Score { get; private init; }

    /// <summary>Qarorni qaysi tier berdi: 0 qoidalar · 1 normalizatsiya · 2–3 AiGateway.</summary>
    public int EvaluationTier { get; private init; }

    public int TimeTakenMs { get; private init; }

    public DateTimeOffset SubmittedAt { get; private init; }

    public List<string> DetectedErrorCodes { get; private init; } = [];

    public static Attempt Record(
        Guid userId,
        Guid exerciseId,
        int dayNumber,
        string answer,
        Verdict verdict,
        decimal score,
        int evaluationTier,
        int timeTakenMs,
        IReadOnlyList<DetectedError> errors,
        DateTimeOffset now)
    {
        var attempt = new Attempt
        {
            UserId = userId,
            ExerciseId = exerciseId,
            Answer = answer,
            Verdict = verdict,
            Score = score,
            EvaluationTier = evaluationTier,
            TimeTakenMs = timeTakenMs,
            SubmittedAt = now,
            DetectedErrorCodes = [.. errors.Select(e => e.ErrorCode)],
        };

        attempt.Raise(new AttemptEvaluated(userId, attempt.Id, exerciseId, dayNumber, verdict.ToString(), score, evaluationTier));
        foreach (var error in errors)
        {
            attempt.Raise(new ErrorDetected(userId, attempt.Id, error.ErrorCode, error.Correction));
        }

        return attempt;
    }
}
