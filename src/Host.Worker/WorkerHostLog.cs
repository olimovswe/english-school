namespace EnglishSchool.Host.Worker;

internal static partial class WorkerHostLog
{
    /// <summary>Host qurilmagan (yoki allaqachon yopilgan) boʻlsa, oddiy konsol logger ishlatiladi.</summary>
    public static void Crashed(IHost? host, Exception exception)
    {
        if (host is not null)
        {
            var hostLogger = host.Services.GetRequiredService<ILoggerFactory>().CreateLogger("EnglishSchool.Host.Worker");
            Crashed(hostLogger, exception);
            return;
        }

        using var loggerFactory = LoggerFactory.Create(logging => logging.AddConsole());
        var logger = loggerFactory.CreateLogger("EnglishSchool.Host.Worker");
        Crashed(logger, exception);
    }

    [LoggerMessage(Level = LogLevel.Critical, Message = "Worker ishga tushmadi yoki kutilmaganda toʻxtadi.")]
    private static partial void Crashed(ILogger logger, Exception exception);
}
