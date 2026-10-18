using Microsoft.EntityFrameworkCore;

namespace EnglishSchool.BuildingBlocks.Outbox;

public static class OutboxModelBuilderExtensions
{
    /// <summary>
    /// Modul DbContext'ining default schema'sida <c>outbox_messages</c> jadvalini xaritalaydi.
    /// Qisman indeks faqat ishlov berilmagan qatorlarni qamraydi: jadval oʻsib ketsa ham polling tez qoladi.
    /// </summary>
    public static ModelBuilder AddOutboxMessages(this ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        modelBuilder.Entity<OutboxMessage>(message =>
        {
            message.ToTable("outbox_messages");
            message.HasKey(x => x.Id);
            message.Property(x => x.Id).ValueGeneratedNever();
            message.Property(x => x.Type).HasMaxLength(300);
            message.Property(x => x.Content).HasColumnType("jsonb");
            message.Property(x => x.LastError).HasMaxLength(2000);
            message.HasIndex(x => x.OccurredAt).HasFilter("processed_at IS NULL");
        });

        return modelBuilder;
    }

    /// <summary>DbContext'ga <see cref="OutboxInterceptor"/> ulaydi.</summary>
    public static DbContextOptionsBuilder UseOutbox(this DbContextOptionsBuilder options)
    {
        ArgumentNullException.ThrowIfNull(options);
        return options.AddInterceptors(OutboxInterceptor.Instance);
    }
}
