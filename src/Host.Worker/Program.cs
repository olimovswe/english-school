using EnglishSchool.BuildingBlocks.Caching;
using EnglishSchool.BuildingBlocks.Outbox;
using EnglishSchool.Host.Worker;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddHybridCaching(builder.Configuration);

// Worker modullarning fon ishlarini bajaradi: outbox (3-hafta), keyinroq tungi job'lar, Batch API, embedding.
// Eventlarni faqat Worker yetkazadi; API ularni outbox'ga yozadi, xolos.
builder.Services.AddOutboxDispatcher(builder.Configuration);

foreach (var module in ModuleCatalog.All)
{
    module.AddServices(builder.Services, builder.Configuration);
}

using var host = builder.Build();
await host.RunAsync();
