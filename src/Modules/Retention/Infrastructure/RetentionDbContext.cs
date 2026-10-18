using EnglishSchool.BuildingBlocks.Outbox;
using EnglishSchool.Modules.Retention.Domain;
using Microsoft.EntityFrameworkCore;

namespace EnglishSchool.Modules.Retention.Infrastructure;

/// <summary>
/// Modul faqat oʻz <c>retention</c> schema'si bilan ishlaydi. Kurs lugʻatini Curriculum'dan
/// <c>ICurriculumQueries</c> orqali oʻqiydi; kartochka matni yaratilganda nusxalanadi.
/// </summary>
internal sealed class RetentionDbContext(DbContextOptions<RetentionDbContext> options) : DbContext(options)
{
    public const string Schema = "retention";

    public DbSet<Flashcard> Flashcards => Set<Flashcard>();

    public DbSet<CardReview> CardReviews => Set<CardReview>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schema);
        modelBuilder.AddOutboxMessages();

        modelBuilder.Entity<Flashcard>(card =>
        {
            card.ToTable("flashcards");
            card.HasKey(x => x.Id);
            card.HasIndex(x => new { x.UserId, x.VocabularyItemId }).IsUnique();
            card.HasIndex(x => new { x.UserId, x.DueAt });
            card.Property(x => x.Kind).HasConversion<string>().HasMaxLength(16);
            card.Property(x => x.Front).HasMaxLength(120);
            card.Property(x => x.Back).HasMaxLength(200);
            card.Property(x => x.Version).IsRowVersion();

            card.ComplexProperty(x => x.State, state =>
            {
                state.Property(x => x.Phase).HasConversion<string>().HasMaxLength(16);
            });
        });

        modelBuilder.Entity<CardReview>(review =>
        {
            review.ToTable("card_reviews");
            review.HasKey(x => x.Id);
            review.HasIndex(x => new { x.FlashcardId, x.ReviewedAt });
            review.HasIndex(x => new { x.UserId, x.ReviewedAt });
            review.Property(x => x.PhaseBefore).HasConversion<string>().HasMaxLength(16);

            review.HasOne<Flashcard>()
                .WithMany()
                .HasForeignKey(x => x.FlashcardId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
