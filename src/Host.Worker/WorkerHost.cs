using EnglishSchool.BuildingBlocks.Caching;
using EnglishSchool.BuildingBlocks.Hosting;
using EnglishSchool.BuildingBlocks.Outbox;
using GenericHost = Microsoft.Extensions.Hosting.Host;

namespace EnglishSchool.Host.Worker;

/// <summary>
/// Worker'ning composition root'i. <c>Program</c> va tizim testlari bir xil yigʻishni ishlatadi, shuning uchun testdagi
/// Worker productiondagi bilan aynan bir xil: barcha modullar va outbox dispatcher.
/// </summary>
public static class WorkerHost
{
    public static HostApplicationBuilder CreateBuilder(string[] args) =>
        CreateBuilder(new HostApplicationBuilderSettings { Args = args });

    /// <param name="overrides">Standart manbalardan (appsettings, muhit oʻzgaruvchilari) ustun turadigan qiymatlar.
    /// Modullar baʼzi sozlamalarni roʻyxatga olish paytida oʻqiydi, shuning uchun ular yigʻishdan oldin qoʻshiladi.</param>
    public static HostApplicationBuilder CreateBuilder(
        HostApplicationBuilderSettings settings, IReadOnlyDictionary<string, string?>? overrides = null)
    {
        var builder = GenericHost.CreateApplicationBuilder(settings);
        if (overrides is not null)
        {
            builder.Configuration.AddInMemoryCollection(overrides);
        }

        builder.Services.AddGracefulShutdown(builder.Configuration);
        builder.Services.AddHybridCaching(builder.Configuration);

        // Eventlarni faqat Worker yetkazadi; API ularni outbox'ga yozadi, xolos.
        builder.Services.AddOutboxDispatcher(builder.Configuration);

        foreach (var module in ModuleCatalog.All)
        {
            module.AddServices(builder.Services, builder.Configuration);
        }

        return builder;
    }
}
