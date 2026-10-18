using EnglishSchool.Modules.Identity.Domain;

namespace EnglishSchool.Modules.Identity.Tests.Domain;

public sealed class UserCredentialsTests
{
    private const int MaxAttempts = 5;
    private static readonly DateTimeOffset Now = new(2026, 10, 18, 9, 0, 0, TimeSpan.Zero);
    private static readonly TimeSpan Lockout = TimeSpan.FromMinutes(5);

    [Fact]
    public void Locks_after_max_failed_attempts_until_the_lockout_ends()
    {
        var credentials = UserCredentials.Create(Guid.NewGuid(), "hash", Now);

        FailTimes(credentials, MaxAttempts - 1);
        Assert.False(credentials.IsLockedOut(Now));

        FailTimes(credentials, 1);
        Assert.True(credentials.IsLockedOut(Now));
        Assert.True(credentials.IsLockedOut(Now + Lockout - TimeSpan.FromSeconds(1)));
        Assert.False(credentials.IsLockedOut(Now + Lockout));
        Assert.Equal(0, credentials.FailedAttempts);
    }

    [Fact]
    public void Successful_login_resets_the_failure_count()
    {
        var credentials = UserCredentials.Create(Guid.NewGuid(), "hash", Now);

        FailTimes(credentials, MaxAttempts - 1);
        credentials.RecordSuccessfulLogin();
        FailTimes(credentials, MaxAttempts - 1);

        Assert.False(credentials.IsLockedOut(Now));
    }

    private static void FailTimes(UserCredentials credentials, int count)
    {
        for (var i = 0; i < count; i++)
        {
            credentials.RecordFailedAttempt(Now, MaxAttempts, Lockout);
        }
    }
}
