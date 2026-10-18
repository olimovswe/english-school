using EnglishSchool.BuildingBlocks.Outbox;
using EnglishSchool.Modules.Discipline.Domain;
using Microsoft.EntityFrameworkCore;

namespace EnglishSchool.Modules.Discipline.Infrastructure;

internal sealed class DisciplineDbContext(DbContextOptions<DisciplineDbContext> options) : DbContext(options)
{
    public const string Schema = "discipline";

    public DbSet<DailyPlan> DailyPlans => Set<DailyPlan>();

    public DbSet<StudySession> StudySessions => Set<StudySession>();

    public DbSet<FocusBlock> FocusBlocks => Set<FocusBlock>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schema);
        modelBuilder.AddOutboxMessages();

        modelBuilder.Entity<DailyPlan>(plan =>
        {
            plan.ToTable("daily_plans");
            plan.HasKey(x => x.Id);
            plan.HasIndex(x => new { x.UserId, x.AssignedDay }).IsUnique();
            plan.Property(x => x.GatePassPercentage).HasPrecision(5, 2);
        });

        modelBuilder.Entity<StudySession>(session =>
        {
            session.ToTable("study_sessions");
            session.HasKey(x => x.Id);
            session.HasIndex(x => new { x.UserId, x.Status });
            session.Property(x => x.Status).HasConversion<string>().HasMaxLength(16);
            session.HasOne<DailyPlan>()
                .WithMany()
                .HasForeignKey(x => x.DailyPlanId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<FocusBlock>(block =>
        {
            block.ToTable("focus_blocks");
            block.HasKey(x => x.Id);
            block.HasIndex(x => new { x.StudySessionId, x.StartedAt });
            block.Property(x => x.BlockType).HasMaxLength(32);
            block.HasOne<StudySession>()
                .WithMany()
                .HasForeignKey(x => x.StudySessionId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}