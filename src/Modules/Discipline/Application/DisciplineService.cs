using EnglishSchool.Modules.Curriculum.Contracts;
using EnglishSchool.Modules.Discipline.Domain;
using EnglishSchool.Modules.Discipline.Infrastructure;
using EnglishSchool.Modules.Retention.Contracts;
using Microsoft.EntityFrameworkCore;

namespace EnglishSchool.Modules.Discipline.Application;

internal sealed class DisciplineService(
    DisciplineDbContext db,
    ICurriculumQueries curriculum,
    IRetentionGateAssessment retentionGate,
    TimeProvider clock)
{
    public async Task<TodayPlanResponse?> GetTodayAsync(Guid userId, CancellationToken cancellationToken)
    {
        var plan = await GetOrCreateTodayPlanAsync(userId, cancellationToken);
        if (plan is null)
        {
            return null;
        }

        var sessionStatus = await db.StudySessions
            .AsNoTracking()
            .Where(session => session.DailyPlanId == plan.Id)
            .OrderByDescending(session => session.StartedAt)
            .Select(session => (StudySessionStatus?)session.Status)
            .FirstOrDefaultAsync(cancellationToken);

        return new TodayPlanResponse(
            plan.DayNumber,
            plan.AssignedDay,
            plan.MorningReviewPassed,
            plan.GatePassPercentage,
            plan.MorningReviewPassed,
            plan.CurriculumCompleted,
            plan.PracticeCompleted,
            plan.FocusCompleted,
            plan.DayCompleted,
            sessionStatus?.ToString());
    }

    public async Task<GateSubmissionResponse?> SubmitGateAsync(
        Guid userId,
        IReadOnlyList<RetentionGateAnswer> answers,
        CancellationToken cancellationToken)
    {
        var plan = await GetOrCreateTodayPlanAsync(userId, cancellationToken);
        if (plan is null)
        {
            return null;
        }

        var assessment = await retentionGate.AssessAsync(userId, answers, cancellationToken);
        if (!assessment.IsValid)
        {
            return new GateSubmissionResponse(plan.DayNumber, false, assessment.Score, assessment.CorrectAnswers, assessment.TotalCards, plan.MorningReviewPassed);
        }

        var passed = plan.RecordMorningReview(assessment.Score);
        if (passed)
        {
            var waitingSession = await db.StudySessions.FirstOrDefaultAsync(
                session => session.DailyPlanId == plan.Id && session.Status == StudySessionStatus.MorningGate,
                cancellationToken);
            waitingSession?.PassMorningGate();
        }

        await db.SaveChangesAsync(cancellationToken);
        return new GateSubmissionResponse(plan.DayNumber, true, assessment.Score, assessment.CorrectAnswers, assessment.TotalCards, passed);
    }

    public async Task<SessionStartResponse?> StartSessionAsync(Guid userId, CancellationToken cancellationToken)
    {
        var plan = await GetOrCreateTodayPlanAsync(userId, cancellationToken);
        if (plan is null)
        {
            return null;
        }

        var hasOpenSession = await db.StudySessions.AnyAsync(
            session => session.DailyPlanId == plan.Id &&
                session.Status != StudySessionStatus.Completed && session.Status != StudySessionStatus.Abandoned,
            cancellationToken);
        if (hasOpenSession)
        {
            return new SessionStartResponse(null, "Conflict");
        }

        var now = clock.GetUtcNow();
        var session = StudySession.Create(userId, plan.Id, now);
        session.BeginMorningGate();
        if (plan.MorningReviewPassed)
        {
            session.PassMorningGate();
        }

        db.StudySessions.Add(session);
        await db.SaveChangesAsync(cancellationToken);
        return new SessionStartResponse(session.Id, session.Status.ToString());
    }

    public async Task<SessionCompletionResponse?> CompleteSessionAsync(
        Guid userId,
        CompleteSessionRequest request,
        CancellationToken cancellationToken)
    {
        var session = await db.StudySessions.SingleOrDefaultAsync(
            item => item.Id == request.SessionId && item.UserId == userId,
            cancellationToken);
        if (session is null)
        {
            return null;
        }

        var plan = await db.DailyPlans.SingleAsync(plan => plan.Id == session.DailyPlanId, cancellationToken);
        var now = clock.GetUtcNow();
        var block = session.StartFocusBlock("Daily", now.AddSeconds(-request.FocusSeconds));
        block.Complete(request.FocusSeconds, request.DistractionCount, now);
        session.CompleteFocusBlock(block, request.MinimalDay, now);
        plan.MarkFocusCompleted();
        db.FocusBlocks.Add(block);
        await db.SaveChangesAsync(cancellationToken);

        return new SessionCompletionResponse(session.Id, session.Status.ToString(), session.TotalFocusSeconds, session.MinimalDay);
    }

    private async Task<DailyPlan?> GetOrCreateTodayPlanAsync(Guid userId, CancellationToken cancellationToken)
    {
        var today = DateOnly.FromDateTime(clock.GetUtcNow().UtcDateTime);
        var existing = await db.DailyPlans.SingleOrDefaultAsync(
            plan => plan.UserId == userId && plan.AssignedDay == today,
            cancellationToken);
        if (existing is not null)
        {
            return existing;
        }

        var days = await curriculum.GetDaysAsync(cancellationToken);
        if (days.Count == 0)
        {
            return null;
        }

        var lastPlan = await db.DailyPlans
            .Where(plan => plan.UserId == userId && plan.AssignedDay < today)
            .OrderByDescending(plan => plan.AssignedDay)
            .Select(plan => new { plan.DayNumber, plan.DayCompleted })
            .FirstOrDefaultAsync(cancellationToken);
        var nextDayNumber = lastPlan is null || lastPlan.DayCompleted ? (lastPlan?.DayNumber ?? 0) + 1 : lastPlan.DayNumber;
        var nextDay = days.FirstOrDefault(day => day.DayNumber == nextDayNumber);
        if (nextDay is null)
        {
            return null;
        }

        var plan = DailyPlan.Create(userId, nextDay.DayNumber, today, clock.GetUtcNow());
        db.DailyPlans.Add(plan);
        await db.SaveChangesAsync(cancellationToken);
        return plan;
    }
}

internal sealed record TodayPlanResponse(
    int DayNumber,
    DateOnly AssignedDay,
    bool MorningReviewPassed,
    decimal? GatePassPercentage,
    bool LessonsUnlocked,
    bool CurriculumCompleted,
    bool PracticeCompleted,
    bool FocusCompleted,
    bool DayCompleted,
    string? CurrentSessionState);

internal sealed record GateSubmissionResponse(
    int DayNumber,
    bool AssessmentValid,
    decimal Score,
    int CorrectAnswers,
    int TotalCards,
    bool Passed);

internal sealed record SessionStartResponse(Guid? SessionId, string Status);

internal sealed record CompleteSessionRequest(
    Guid SessionId,
    int FocusSeconds,
    int DistractionCount,
    bool MinimalDay);

internal sealed record SessionCompletionResponse(Guid SessionId, string Status, int TotalFocusSeconds, bool MinimalDay);