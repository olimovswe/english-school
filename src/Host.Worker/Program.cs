using EnglishSchool.Host.Worker;

// Worker modullarning fon ishlarini bajaradi: outbox orqali eventlarni yetkazadi, keyinroq tungi job'lar va Batch API.
// «migrate» buyrugʻi esa bazani tayyorlab chiqadi: dotnet EnglishSchool.Host.Worker.dll migrate
// Har qanday halokat: tuzilgan Critical log va 1 kodi bilan chiqish.
if (args is ["migrate", .. var migrateArgs])
{
    try
    {
        await WorkerHost.MigrateAsync(new HostApplicationBuilderSettings { Args = migrateArgs });
        return 0;
    }
    catch (Exception exception)
    {
        WorkerHostLog.Crashed(host: null, exception);
        return 1;
    }
}

IHost? host = null;
try
{
    host = WorkerHost.CreateBuilder(args).Build();
    await host.RunAsync();
    return 0;
}
catch (Exception exception) when (exception is not HostAbortedException)
{
    WorkerHostLog.Crashed(host, exception);
    return 1;
}
finally
{
    host?.Dispose();
}
