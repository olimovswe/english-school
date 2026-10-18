using System.Text;
using EnglishSchool.Modules.Retention.Contracts;
using EnglishSchool.Modules.Retention.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace EnglishSchool.Modules.Retention.Application;

internal sealed class RetentionGateAssessment(RetentionDbContext db, TimeProvider clock) : IRetentionGateAssessment
{
    public async Task<RetentionGateAssessmentResult> AssessAsync(
        Guid userId,
        IReadOnlyList<RetentionGateAnswer> answers,
        CancellationToken cancellationToken)
    {
        var dueCards = await db.Flashcards
            .AsNoTracking()
            .Where(card => card.UserId == userId && card.DueAt <= clock.GetUtcNow())
            .Select(card => new { card.Id, card.Back })
            .ToListAsync(cancellationToken);

        if (answers.Select(answer => answer.CardId).Distinct().Count() != answers.Count)
        {
            return new RetentionGateAssessmentResult(false, 0, 0, dueCards.Count);
        }

        var submitted = answers.ToDictionary(answer => answer.CardId, answer => answer.Answer);
        if (submitted.Count != dueCards.Count ||
            dueCards.Any(card => !submitted.ContainsKey(card.Id)))
        {
            return new RetentionGateAssessmentResult(false, 0, 0, dueCards.Count);
        }

        var correct = dueCards.Count(card => string.Equals(
            Normalize(submitted[card.Id]), Normalize(card.Back), StringComparison.OrdinalIgnoreCase));
        var score = dueCards.Count == 0 ? 100m : Math.Round(correct * 100m / dueCards.Count, 2);

        return new RetentionGateAssessmentResult(true, score, correct, dueCards.Count);
    }

    private static string Normalize(string value) => value.Trim().Normalize(NormalizationForm.FormKC);
}