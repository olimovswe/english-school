using EnglishSchool.BuildingBlocks.Outbox;
using EnglishSchool.Modules.Speech.Domain;
using Microsoft.EntityFrameworkCore;

namespace EnglishSchool.Modules.Speech.Infrastructure;

/// <summary>
/// Modul faqat oʻz <c>speech</c> schema'si bilan ishlaydi. Audio fayllar bazada emas, audio omborida;
/// bu yerda ularning metamaʼlumoti va talaffuz baholari.
/// </summary>
internal sealed class SpeechDbContext(DbContextOptions<SpeechDbContext> options) : DbContext(options)
{
    public const string Schema = "speech";

    public DbSet<AudioArtifact> AudioArtifacts => Set<AudioArtifact>();

    public DbSet<PronunciationAssessment> PronunciationAssessments => Set<PronunciationAssessment>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schema);
        modelBuilder.AddOutboxMessages();

        modelBuilder.Entity<AudioArtifact>(audio =>
        {
            audio.ToTable("audio_artifacts");
            audio.HasKey(x => x.Id);
            audio.Property(x => x.Kind).HasConversion<string>().HasMaxLength(16);
            audio.Property(x => x.Text).HasMaxLength(500);
            audio.Property(x => x.Voice).HasMaxLength(32);
            audio.Property(x => x.CacheKey).HasMaxLength(64).IsFixedLength();
            audio.Property(x => x.StorageKey).HasMaxLength(300);
            audio.Property(x => x.ContentType).HasMaxLength(64);
            audio.Property(x => x.Sha256).HasMaxLength(64).IsFixedLength();

            // NULL kalitlar (yozuvlar) PostgreSQL'da unikal indeksga xalaqit bermaydi.
            audio.HasIndex(x => x.CacheKey).IsUnique();
            audio.HasIndex(x => x.StorageKey).IsUnique();
            audio.HasIndex(x => new { x.OwnerId, x.CreatedAt });
        });

        modelBuilder.Entity<PronunciationAssessment>(assessment =>
        {
            assessment.ToTable("pronunciation_assessments");
            assessment.HasKey(x => x.Id);
            assessment.HasIndex(x => new { x.UserId, x.CreatedAt });
            assessment.Property(x => x.TargetText).HasMaxLength(300);
            assessment.Property(x => x.Transcript).HasMaxLength(2000);
            assessment.Property(x => x.Language).HasMaxLength(8);
            assessment.Property(x => x.Verdict).HasConversion<string>().HasMaxLength(24);
            assessment.Property(x => x.AccuracyScore).HasPrecision(5, 2);
            assessment.Property(x => x.CompletenessScore).HasPrecision(5, 2);
            assessment.Property(x => x.FluencyScore).HasPrecision(5, 2);
            assessment.Property(x => x.OverallScore).HasPrecision(5, 2);

            assessment.ComplexCollection(x => x.Words, words =>
            {
                words.ToJson("words");
                words.Property(x => x.Status).HasConversion<string>();
            });

            assessment.HasOne<AudioArtifact>()
                .WithMany()
                .HasForeignKey(x => x.RecordingId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
