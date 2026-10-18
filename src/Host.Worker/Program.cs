using EnglishSchool.Host.Worker;

// Worker modullarning fon ishlarini bajaradi: outbox orqali eventlarni yetkazadi, keyinroq tungi job'lar va Batch API.
using var host = WorkerHost.CreateBuilder(args).Build();

try
{
    await host.RunAsync();
}
catch (Exception exception) when (exception is not HostAbortedException)
{
    var logger = host.Services.GetRequiredService<ILoggerFactory>().CreateLogger("EnglishSchool.Host.Worker");
    WorkerHostLog.Crashed(logger, exception);
    return 1;
}

return 0;
