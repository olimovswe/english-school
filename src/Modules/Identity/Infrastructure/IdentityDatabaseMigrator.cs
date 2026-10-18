using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace EnglishSchool.Modules.Identity.Infrastructure;

/// <summary>Lokal muhitda <c>identity</c> schema migratsiyalarini qoʻllaydi. Productionda bu alohida qadam.</summary>
internal sealed class IdentityDatabaseMigrator(
    IServiceScopeFactory scopeFactory,
    IOptions<IdentityModuleOptions> options) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (!options.Value.ApplyMigrationsOnStartup)
        {
            return;
        }

        await using var scope = scopeFactory.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<IdentityDbContext>().Database.MigrateAsync(cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
