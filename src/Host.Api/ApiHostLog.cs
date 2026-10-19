namespace EnglishSchool.Host.Api;

internal static partial class ApiHostLog
{
    /// <summary>Host qurilmagan boʻlsa (sozlama xatosi), uning logging'i ham yoʻq: oddiy konsol logger ishlatiladi.</summary>
    public static void Crashed(WebApplication? app, Exception exception)
    {
        if (app is not null)
        {
            Crashed(app.Logger, exception);
            return;
        }

        using var loggerFactory = LoggerFactory.Create(logging => logging.AddConsole());
        var logger = loggerFactory.CreateLogger("EnglishSchool.Host.Api");
        Crashed(logger, exception);
    }

    [LoggerMessage(Level = LogLevel.Critical, Message = "API ishga tushmadi yoki kutilmaganda toʻxtadi.")]
    private static partial void Crashed(ILogger logger, Exception exception);
}
