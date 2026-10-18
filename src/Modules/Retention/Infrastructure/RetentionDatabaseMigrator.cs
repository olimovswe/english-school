using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace EnglishSchool.Modules.Retention.Infrastructure;

/// <summary>Lokal muhitda <c>retention</c> schema migratsiyalarini qoʻllaydi. Productionda bu alohida qadam.</summary>
internal sealed class RetentionDatabaseMigrator(
    IServiceScopeFactory scopeFactory,
    IOptions<RetentionOptions> options) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (!options.Value.ApplyMigrationsOnStartup)
        {
            return;
        }

        await using var scope = scopeFactory.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<RetentionDbContext>().Database.MigrateAsync(cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
