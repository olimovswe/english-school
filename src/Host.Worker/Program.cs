using EnglishSchool.BuildingBlocks.Caching;
using EnglishSchool.Host.Worker;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddHybridCaching(builder.Configuration);

// Worker modullarning fon ishlarini bajaradi: outbox, tungi job'lar, Batch API, embedding.
// 1-haftada faqat modullar roʻyxatga olinadi; job'lar keyingi haftalarda qoʻshiladi.
foreach (var module in ModuleCatalog.All)
{
    module.AddServices(builder.Services, builder.Configuration);
}

using var host = builder.Build();
await host.RunAsync();
