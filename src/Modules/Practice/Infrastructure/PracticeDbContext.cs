using EnglishSchool.BuildingBlocks.Outbox;
using EnglishSchool.Modules.Practice.Domain;
using Microsoft.EntityFrameworkCore;

namespace EnglishSchool.Modules.Practice.Infrastructure;

/// <summary>
/// Modul faqat oʻz <c>practice</c> schema'si bilan ishlaydi. Kurs kunlarini Curriculum'dan
/// <c>ICurriculumQueries</c> orqali oʻqiydi, foydalanuvchini esa token'dagi UserId orqali biladi.
/// </summary>
internal sealed class PracticeDbContext(DbContextOptions<PracticeDbContext> options) : DbContext(options)
{
    public const string Schema = "practice";

    public DbSet<Exercise> Exercises => Set<Exercise>();

    public DbSet<Attempt> Attempts => Set<Attempt>();

    public DbSet<UserError> UserErrors => Set<UserError>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schema);
        modelBuilder.AddOutboxMessages();

        modelBuilder.Entity<Exercise>(exercise =>
        {
            exercise.ToTable("exercises");
            exercise.HasKey(x => x.Id);
            exercise.HasIndex(x => new { x.CourseVersion, x.DayNumber, x.Position }).IsUnique();
            exercise.Property(x => x.Type).HasConversion<string>().HasMaxLength(32);
            exercise.Property(x => x.Instruction).HasMaxLength(200);
            exercise.Property(x => x.Prompt).HasMaxLength(500);
            exercise.Property(x => x.Explanation).HasMaxLength(500);
            exercise.Property(x => x.AnswerPattern).HasMaxLength(200);

            exercise.HasMany(x => x.Options)
                .WithOne()
                .HasForeignKey(x => x.ExerciseId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ExerciseOption>(option =>
        {
            option.ToTable("exercise_options");
            option.HasKey(x => x.Id);
            option.HasIndex(x => new { x.ExerciseId, x.Position }).IsUnique();
            option.Property(x => x.Text).HasMaxLength(200);
            option.Property(x => x.MatchText).HasMaxLength(200);
            option.Property(x => x.ErrorCode).HasMaxLength(32);
            option.Property(x => x.Feedback).HasMaxLength(500);
        });

        modelBuilder.Entity<Attempt>(attempt =>
        {
            attempt.ToTable("attempts");
            attempt.HasKey(x => x.Id);
            attempt.HasIndex(x => new { x.UserId, x.SubmittedAt });
            attempt.Property(x => x.Answer).HasMaxLength(500);
            attempt.Property(x => x.Verdict).HasConversion<string>().HasMaxLength(24);
            attempt.Property(x => x.Score).HasPrecision(3, 2);

            attempt.HasOne<Exercise>()
                .WithMany()
                .HasForeignKey(x => x.ExerciseId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<UserError>(error =>
        {
            error.ToTable("user_errors");
            error.HasKey(x => x.Id);
            error.HasIndex(x => new { x.UserId, x.ErrorCode }).IsUnique();
            error.Property(x => x.ErrorCode).HasMaxLength(32);
        });
    }
}
