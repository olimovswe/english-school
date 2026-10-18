using System.Runtime.CompilerServices;

namespace EnglishSchool.SystemTests.TestSupport;

/// <summary>
/// Eventlar Worker orqali asinxron yetadi: natija paydo boʻlguncha (yoki vaqt tugaguncha) qayta soʻraladi.
/// </summary>
internal static class Eventually
{
    private static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan Interval = TimeSpan.FromMilliseconds(200);

    public static async Task<T> GetAsync<T>(
        Func<Task<T>> probe,
        Func<T, bool> done,
        TimeSpan? timeout = null,
        [CallerArgumentExpression(nameof(done))] string condition = "")
    {
        var deadline = DateTimeOffset.UtcNow + (timeout ?? DefaultTimeout);
        while (true)
        {
            var value = await probe();
            if (done(value))
            {
                return value;
            }

            if (DateTimeOffset.UtcNow > deadline)
            {
                throw new TimeoutException($"Kutilgan holat {timeout ?? DefaultTimeout} ichida yuz bermadi: {condition}. Oxirgi qiymat: {value}");
            }

            await Task.Delay(Interval);
        }
    }
}
