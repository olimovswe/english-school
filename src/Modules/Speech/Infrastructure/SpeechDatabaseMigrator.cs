using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace EnglishSchool.Modules.Speech.Infrastructure;

/// <summary>Lokal muhitda <c>speech</c> schema migratsiyalarini qoʻllaydi. Productionda bu alohida qadam.</summary>
internal sealed class SpeechDatabaseMigrator(
    IServiceScopeFactory scopeFactory,
    IOptions<SpeechOptions> options) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (!options.Value.ApplyMigrationsOnStartup)
        {
            return;
        }

        await using var scope = scopeFactory.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<SpeechDbContext>().Database.MigrateAsync(cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
