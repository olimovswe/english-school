using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EnglishSchool.Modules.Analytics.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialAnalytics : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "analytics");

            migrationBuilder.CreateTable(
                name: "error_summaries",
                schema: "analytics",
                columns: table => new
                {
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    error_code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    correction = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    occurrences = table.Column<int>(type: "integer", nullable: false),
                    first_seen_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    last_seen_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    last_attempt_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_error_summaries", x => new { x.user_id, x.error_code });
                });

            migrationBuilder.CreateTable(
                name: "user_progress",
                schema: "analytics",
                columns: table => new
                {
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    current_day = table.Column<int>(type: "integer", nullable: false),
                    completed_days = table.Column<int>(type: "integer", nullable: false),
                    total_practice_attempts = table.Column<int>(type: "integer", nullable: false),
                    streak_count = table.Column<int>(type: "integer", nullable: false),
                    last_study_day = table.Column<DateOnly>(type: "date", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_user_progress", x => x.user_id);
                });

            migrationBuilder.CreateTable(
                name: "weekly_reports",
                schema: "analytics",
                columns: table => new
                {
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    week_starting = table.Column<DateOnly>(type: "date", nullable: false),
                    accuracy_score_total = table.Column<decimal>(type: "numeric(10,4)", precision: 10, scale: 4, nullable: false),
                    attempt_count = table.Column<int>(type: "integer", nullable: false),
                    total_focus_seconds = table.Column<int>(type: "integer", nullable: false),
                    words_learned = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_weekly_reports", x => new { x.user_id, x.week_starting });
                });

            migrationBuilder.CreateIndex(
                name: "ix_error_summaries_user_id_occurrences",
                schema: "analytics",
                table: "error_summaries",
                columns: new[] { "user_id", "occurrences" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "error_summaries",
                schema: "analytics");

            migrationBuilder.DropTable(
                name: "user_progress",
                schema: "analytics");

            migrationBuilder.DropTable(
                name: "weekly_reports",
                schema: "analytics");
        }
    }
}
