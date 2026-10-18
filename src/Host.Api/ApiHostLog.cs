namespace EnglishSchool.Host.Api;

internal static partial class ApiHostLog
{
    [LoggerMessage(Level = LogLevel.Critical, Message = "API ishga tushmadi yoki kutilmaganda toʻxtadi.")]
    public static partial void Crashed(ILogger logger, Exception exception);
}
