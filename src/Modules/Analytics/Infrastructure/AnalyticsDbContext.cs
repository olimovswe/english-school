using EnglishSchool.Modules.Analytics.Domain;
using Microsoft.EntityFrameworkCore;

namespace EnglishSchool.Modules.Analytics.Infrastructure;

internal sealed class AnalyticsDbContext(DbContextOptions<AnalyticsDbContext> options) : DbContext(options)
{
    public const string Schema = "analytics";

    public DbSet<UserProgressReadModel> UserProgress => Set<UserProgressReadModel>();

    public DbSet<WeeklyReportReadModel> WeeklyReports => Set<WeeklyReportReadModel>();

    public DbSet<ErrorSummaryReadModel> ErrorSummaries => Set<ErrorSummaryReadModel>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schema);

        modelBuilder.Entity<UserProgressReadModel>(progress =>
        {
            progress.ToTable("user_progress");
            progress.HasKey(x => x.UserId);
        });

        modelBuilder.Entity<WeeklyReportReadModel>(report =>
        {
            report.ToTable("weekly_reports");
            report.HasKey(x => new { x.UserId, x.WeekStarting });
            report.Ignore(x => x.Accuracy);
            report.Ignore(x => x.TotalFocusMinutes);
            report.Property(x => x.AccuracyScoreTotal).HasPrecision(10, 4);
        });

        modelBuilder.Entity<ErrorSummaryReadModel>(error =>
        {
            error.ToTable("error_summaries");
            error.HasKey(x => new { x.UserId, x.ErrorCode });
            error.HasIndex(x => new { x.UserId, x.Occurrences });
            error.Property(x => x.ErrorCode).HasMaxLength(64);
            error.Property(x => x.Correction).HasMaxLength(500);
        });
    }
}