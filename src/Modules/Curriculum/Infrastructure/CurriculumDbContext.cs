using EnglishSchool.Modules.Curriculum.Domain;
using Microsoft.EntityFrameworkCore;

namespace EnglishSchool.Modules.Curriculum.Infrastructure;

/// <summary>
/// Modul faqat oʻz <c>curriculum</c> schema'si bilan ishlaydi. Boshqa modullar bu jadvallarga
/// SQL yozmaydi: ular <see cref="Contracts.ICurriculumQueries"/> orqali oʻqiydi.
/// </summary>
internal sealed class CurriculumDbContext(DbContextOptions<CurriculumDbContext> options) : DbContext(options)
{
    public const string Schema = "curriculum";

    public DbSet<CourseDay> Days => Set<CourseDay>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schema);

        modelBuilder.Entity<CourseDay>(day =>
        {
            day.ToTable("days");
            day.HasKey(x => x.Id);
            day.HasIndex(x => new { x.CourseVersion, x.DayNumber }).IsUnique();
            day.Property(x => x.Kind).HasConversion<string>().HasMaxLength(16);
            day.Property(x => x.Title).HasMaxLength(200);
            day.Property(x => x.VocabularyTheme).HasMaxLength(200);
            day.Property(x => x.PreparesFor).HasMaxLength(200);

            day.HasOne(x => x.Grammar)
                .WithOne()
                .HasForeignKey<GrammarTopic>(x => x.DayId)
                .OnDelete(DeleteBehavior.Cascade);

            day.HasMany(x => x.Vocabulary)
                .WithOne()
                .HasForeignKey(x => x.DayId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<GrammarTopic>(topic =>
        {
            topic.ToTable("grammar_topics");
            topic.HasKey(x => x.Id);
            topic.Property(x => x.Title).HasMaxLength(200);
        });

        modelBuilder.Entity<VocabularyItem>(item =>
        {
            item.ToTable("vocabulary_items");
            item.HasKey(x => x.Id);
            item.HasIndex(x => new { x.DayId, x.Position }).IsUnique();
            item.Property(x => x.English).HasMaxLength(120);
            item.Property(x => x.Uzbek).HasMaxLength(200);
        });
    }
}
