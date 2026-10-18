using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace EnglishSchool.BuildingBlocks.Outbox;

/// <summary>
/// Faqat Worker'da ishlaydi: har <see cref="OutboxOptions.PollingInterval"/> da outbox'larni tekshiradi va
/// navbat boʻshaguncha partiyama-partiya yetkazadi.
/// </summary>
internal sealed class OutboxDispatcher(OutboxProcessor processor, IOptions<OutboxOptions> options) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(options.Value.PollingInterval);

        while (!stoppingToken.IsCancellationRequested)
        {
            // Nimadir yetkazilgan boʻlsa, navbatda yana boʻlishi mumkin: kutmasdan keyingi partiya olinadi.
            if (await processor.ProcessAsync(stoppingToken) > 0)
            {
                continue;
            }

            await timer.WaitForNextTickAsync(stoppingToken);
        }
    }
}
