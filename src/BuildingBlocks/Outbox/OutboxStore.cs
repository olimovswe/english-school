using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;

namespace EnglishSchool.BuildingBlocks.Outbox;

/// <summary>Bitta modulning outbox jadvali. Modul uni <c>AddOutboxStore&lt;TContext&gt;()</c> bilan roʻyxatga oladi.</summary>
public interface IOutboxStore
{
    string Name { get; }

    /// <returns>Muvaffaqiyatli yetkazilgan xabarlar soni.</returns>
    Task<int> ProcessBatchAsync(
        Func<OutboxMessage, CancellationToken, Task> dispatch,
        int batchSize,
        int maxAttempts,
        TimeProvider clock,
        CancellationToken cancellationToken);
}

/// <summary>
/// Navbatdagi xabarlarni <c>FOR UPDATE SKIP LOCKED</c> bilan oladi: bir nechta Worker nusxasi parallel ishlasa ham
/// bitta xabarni ikkitasi olmaydi. Qulf xabarlar yetkazilib, holati yozilguncha (tranzaksiya oxirigacha) turadi.
/// Toʻxtash soʻralsa, joriy partiya xabarlar orasida toʻxtaydi: yetkazilganlari saqlanadi, qolganlari navbatga qaytadi.
/// </summary>
internal sealed class OutboxStore<TContext>(IServiceScopeFactory scopeFactory) : IOutboxStore
    where TContext : DbContext
{
    private string? _claimSql;

    public string Name => typeof(TContext).Name;

    public async Task<int> ProcessBatchAsync(
        Func<OutboxMessage, CancellationToken, Task> dispatch,
        int batchSize,
        int maxAttempts,
        TimeProvider clock,
        CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<TContext>();

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        var batch = await db.Set<OutboxMessage>()
            .FromSqlRaw(_claimSql ??= BuildClaimSql(db), maxAttempts, clock.GetUtcNow(), batchSize)
            .ToListAsync(cancellationToken);

        var delivered = 0;
        foreach (var message in batch)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                break;
            }

            try
            {
                await dispatch(message, cancellationToken);
                message.MarkProcessed(clock.GetUtcNow());
                delivered++;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                // Toʻxtash shu xabar ustida keldi: u belgilanmaydi va keyin qayta yetkaziladi.
                break;
            }
            catch (Exception exception)
            {
                // Consumer'ning oʻz timeout'i (TaskCanceledException) ham oddiy xato: qayta urinish kutish bilan.
                message.RecordFailure($"{exception.GetType().Name}: {exception.Message}", clock.GetUtcNow());
            }
        }

        // Toʻxtayotgan boʻlsa ham bajarilgan ish yoziladi, aks holda yetkazilgan xabarlar qayta yuborilardi.
        await db.SaveChangesAsync(CancellationToken.None);
        await transaction.CommitAsync(CancellationToken.None);
        return delivered;
    }

    /// <summary>Jadval va ustun nomlari modeldan olinadi; qiymatlar parametr sifatida beriladi.</summary>
    private static string BuildClaimSql(TContext db)
    {
        var entity = db.Model.FindEntityType(typeof(OutboxMessage))
            ?? throw new InvalidOperationException($"{typeof(TContext).Name} modelida outbox jadvali yoʻq.");
        var sql = db.GetService<ISqlGenerationHelper>();

        string Column(string property) => sql.DelimitIdentifier(entity.FindProperty(property)!.GetColumnName());

        var table = sql.DelimitIdentifier(entity.GetTableName()!, entity.GetSchema());
        return $"SELECT * FROM {table} " +
            $"WHERE {Column(nameof(OutboxMessage.ProcessedAt))} IS NULL " +
            $"AND {Column(nameof(OutboxMessage.FailedAttempts))} < {{0}} " +
            $"AND ({Column(nameof(OutboxMessage.NextAttemptAt))} IS NULL OR {Column(nameof(OutboxMessage.NextAttemptAt))} <= {{1}}) " +
            $"ORDER BY {Column(nameof(OutboxMessage.OccurredAt))} " +
            "LIMIT {2} FOR UPDATE SKIP LOCKED";
    }
}
