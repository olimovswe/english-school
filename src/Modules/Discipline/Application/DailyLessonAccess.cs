using EnglishSchool.BuildingBlocks.Security;
using EnglishSchool.Modules.Discipline.Contracts;
using EnglishSchool.Modules.Discipline.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace EnglishSchool.Modules.Discipline.Application;

internal sealed class DailyLessonAccessRequirement : IAuthorizationRequirement;

internal sealed class DailyLessonAccessHandler(DisciplineDbContext db, TimeProvider clock)
    : AuthorizationHandler<DailyLessonAccessRequirement>
{
    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        DailyLessonAccessRequirement requirement)
    {
        if (context.Resource is not HttpContext httpContext ||
            !httpContext.User.TryGetUserId(out var userId) ||
            !int.TryParse(httpContext.Request.RouteValues["dayNumber"]?.ToString(), out var dayNumber))
        {
            return;
        }

        var today = DateOnly.FromDateTime(clock.GetUtcNow().UtcDateTime);
        if (await db.DailyPlans.AnyAsync(
                plan => plan.UserId == userId && plan.DayNumber == dayNumber &&
                    plan.AssignedDay <= today && plan.MorningReviewPassed,
                httpContext.RequestAborted))
        {
            context.Succeed(requirement);
        }
    }
}