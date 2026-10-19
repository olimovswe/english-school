using EnglishSchool.Host.Api;

// Sozlama xatosi (masalan, JWT kaliti yoʻq yoki notoʻgʻri CORS manbasi) yoki ish paytidagi halokat: tuzilgan Critical
// log va 1 kodi bilan chiqish, shunda konteyner orkestratori sababni koʻrsatadi va xizmatni qayta ishga tushiradi.
WebApplication? app = null;
try
{
    app = ApiHost.Build(args);
    await app.RunAsync();
    return 0;
}
catch (Exception exception) when (exception is not HostAbortedException)
{
    ApiHostLog.Crashed(app, exception);
    return 1;
}
finally
{
    if (app is not null)
    {
        await app.DisposeAsync();
    }
}
