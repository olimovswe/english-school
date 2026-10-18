namespace EnglishSchool.Host.Worker;

internal static partial class WorkerHostLog
{
    [LoggerMessage(Level = LogLevel.Critical, Message = "Worker ishga tushmadi yoki kutilmaganda toʻxtadi.")]
    public static partial void Crashed(ILogger logger, Exception exception);
}
