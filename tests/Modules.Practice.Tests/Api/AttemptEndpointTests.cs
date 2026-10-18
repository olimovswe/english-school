using System.Net;
using EnglishSchool.BuildingBlocks.Outbox;
using EnglishSchool.Modules.Practice.Application;
using EnglishSchool.Modules.Practice.Contracts.Events;
using EnglishSchool.Modules.Practice.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace EnglishSchool.Modules.Practice.Tests.Api;

[Collection(PracticeApiFactory.CollectionName)]
public sealed class AttemptEndpointTests(PracticeApiFactory factory)
{
    private const string StudentGap = "I ___ a student.";

    [Fact]
    public async Task Exact_answer_is_correct_at_tier_0()
    {
        using var student = await factory.CreateStudentAsync();
        var exercise = await student.GetExerciseAsync(2, StudentGap);

        var result = await student.SubmitTextAsync(exercise, "am");

        Assert.Equal("Correct", result.Verdict);
        Assert.Equal(1m, result.Score);
        Assert.Equal(0, result.EvaluationTier);
        Assert.Empty(result.Feedback);
    }

    [Theory]
    [InlineData(1, "— ___! My name is Ali. — Hi, Ali!", "hello")]
    [InlineData(3, "He ___ a doctor. He is a teacher.", "Is Not")]
    [InlineData(3, "He ___ a doctor. He is a teacher.", "isn’t")]
    public async Task Letter_case_and_contraction_variants_are_accepted_at_tier_1(int day, string prompt, string answer)
    {
        using var student = await factory.CreateStudentAsync();
        var exercise = await student.GetExerciseAsync(day, prompt);

        var result = await student.SubmitTextAsync(exercise, answer);

        Assert.Equal("Correct", result.Verdict);
        Assert.Equal(1, result.EvaluationTier);
    }

    [Fact]
    public async Task Misspelling_is_partially_correct_with_spelling_feedback()
    {
        using var student = await factory.CreateStudentAsync();
        var exercise = await student.GetExerciseAsync(6, "two ___ (child)");

        var result = await student.SubmitTextAsync(exercise, "childern");

        Assert.Equal("PartiallyCorrect", result.Verdict);
        Assert.Equal(0.5m, result.Score);
        var feedback = Assert.Single(result.Feedback);
        Assert.Equal("SPELLING", feedback.ErrorCode);
        Assert.Equal("children", feedback.Correction);
    }

    [Fact]
    public async Task Known_mistake_explains_the_rule_and_repeats_are_counted_per_student()
    {
        using var student = await factory.CreateStudentAsync();
        var exercise = await student.GetExerciseAsync(2, StudentGap);

        var first = await student.SubmitTextAsync(exercise, "is");
        await student.SubmitTextAsync(exercise, "are");

        Assert.Equal("Incorrect", first.Verdict);
        var feedback = Assert.Single(first.Feedback);
        Assert.Equal("BE_FORM", feedback.ErrorCode);
        Assert.Equal("am", feedback.Correction);
        Assert.Equal("I bilan faqat am ishlatiladi: I am a student.", feedback.Explanation);

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PracticeDbContext>();
        var error = await db.UserErrors.SingleAsync(e => e.UserId == student.UserId);
        Assert.Equal("BE_FORM", error.ErrorCode);
        Assert.Equal(2, error.Occurrences);
    }

    [Fact]
    public async Task Unrecognised_wrong_answer_shows_the_correct_answer_without_guessing_an_error_type()
    {
        using var student = await factory.CreateStudentAsync();
        var exercise = await student.GetExerciseAsync(4, "I'm ___ engineer.");

        var result = await student.SubmitTextAsync(exercise, "the");

        Assert.Equal("Incorrect", result.Verdict);
        Assert.Equal(1, result.EvaluationTier);
        var feedback = Assert.Single(result.Feedback);
        Assert.Null(feedback.ErrorCode);
        Assert.Equal("an", feedback.Correction);

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PracticeDbContext>();
        Assert.False(await db.UserErrors.AnyAsync(e => e.UserId == student.UserId));
    }

    [Fact]
    public async Task Multiple_choice_and_matching_are_graded_at_tier_0()
    {
        using var student = await factory.CreateStudentAsync();
        var choice = await student.GetExerciseAsync(4, "«Men dasturchiman.»");
        var matching = await student.GetExerciseAsync(2, "2-kun soʻzlari");
        string[] translations = ["xursand", "xafa, gʻamgin", "charchagan", "band", "och (qorni och)"];

        var chosen = await student.SelectAsync(choice, "I'm an developer.");
        var matched = await student.SubmitSuccessfullyAsync(new SubmitAttemptRequest(
            matching.Id, null, null, [.. matching.Choices.Zip(translations, (c, uz) => new MatchAnswer(c.Id, uz))], 30_000));

        Assert.Equal(("Incorrect", 0), (chosen.Verdict, chosen.EvaluationTier));
        Assert.Equal("ART_WRONG", Assert.Single(chosen.Feedback).ErrorCode);
        Assert.Equal(("PartiallyCorrect", 0.6m, 0), (matched.Verdict, matched.Score, matched.EvaluationTier));
        Assert.Equal(["hungry — och (qorni och)", "busy — band"], matched.Feedback.Select(f => f.Correction));
    }

    [Fact]
    public async Task Attempt_and_its_events_are_written_in_one_transaction()
    {
        using var student = await factory.CreateStudentAsync();
        var exercise = await student.GetExerciseAsync(6, "___ are my books. (yaqin, koʻp)");

        var result = await student.SubmitTextAsync(exercise, "This");

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PracticeDbContext>();
        var attempt = await db.Attempts.SingleAsync(a => a.Id == result.AttemptId);
        Assert.Equal(["DEMONSTRATIVE"], attempt.DetectedErrorCodes);

        var messages = await db.Set<OutboxMessage>().AsNoTracking().Where(m => m.ProcessedAt == null).ToListAsync();
        var evaluated = messages
            .Where(m => m.Type == OutboxSerializer.TypeName(typeof(AttemptEvaluated)))
            .Select(m => (AttemptEvaluated)OutboxSerializer.Deserialize(m.Content, typeof(AttemptEvaluated)))
            .Single(e => e.AttemptId == result.AttemptId);
        var detected = messages
            .Where(m => m.Type == OutboxSerializer.TypeName(typeof(ErrorDetected)))
            .Select(m => (ErrorDetected)OutboxSerializer.Deserialize(m.Content, typeof(ErrorDetected)))
            .Single(e => e.AttemptId == result.AttemptId);

        Assert.Equal(
            (student.UserId, exercise.Id, 6, "Incorrect", 0),
            (evaluated.UserId, evaluated.ExerciseId, evaluated.DayNumber, evaluated.Verdict, evaluated.EvaluationTier));
        Assert.Equal(("DEMONSTRATIVE", "These"), (detected.ErrorCode, detected.Correction));
    }

    [Fact]
    public async Task Malformed_submissions_are_rejected_before_grading()
    {
        using var student = await factory.CreateStudentAsync();
        var gap = await student.GetExerciseAsync(2, StudentGap);
        var choice = await student.GetExerciseAsync(2, "«U (qiz) charchagan.»");
        var matching = await student.GetExerciseAsync(2, "2-kun soʻzlari");

        using var unknownExercise = await student.SubmitAsync(new SubmitAttemptRequest(Guid.NewGuid(), "am", null, null, 1_000));
        using var noExercise = await student.SubmitAsync(new SubmitAttemptRequest(null, "am", null, null, 1_000));
        using var negativeTime = await student.SubmitAsync(new SubmitAttemptRequest(gap.Id, "am", null, null, -1));
        using var blankAnswer = await student.SubmitAsync(new SubmitAttemptRequest(gap.Id, "   ", null, null, 1_000));
        using var foreignOption = await student.SubmitAsync(
            new SubmitAttemptRequest(choice.Id, null, matching.Choices[0].Id, null, 1_000));
        using var incompleteMatching = await student.SubmitAsync(new SubmitAttemptRequest(
            matching.Id, null, null, [new MatchAnswer(matching.Choices[0].Id, "xursand")], 1_000));

        Assert.Equal(HttpStatusCode.NotFound, unknownExercise.StatusCode);
        Assert.All(
            [noExercise, negativeTime, blankAnswer, foreignOption, incompleteMatching],
            response => Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode));

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PracticeDbContext>();
        Assert.False(await db.Attempts.AnyAsync(a => a.UserId == student.UserId));
    }
}
