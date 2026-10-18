using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EnglishSchool.Modules.Analytics.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAnalyticsEventConsumers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "accuracy_attempt_count",
                schema: "analytics",
                table: "user_progress",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "accuracy_score_total",
                schema: "analytics",
                table: "user_progress",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "active_vocabulary",
                schema: "analytics",
                table: "user_progress",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateOnly>(
                name: "last_completed_day",
                schema: "analytics",
                table: "user_progress",
                type: "date",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "processed_events",
                schema: "analytics",
                columns: table => new
                {
                    event_id = table.Column<Guid>(type: "uuid", nullable: false),
                    event_type = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    occurred_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_processed_events", x => x.event_id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "processed_events",
                schema: "analytics");

            migrationBuilder.DropColumn(
                name: "accuracy_attempt_count",
                schema: "analytics",
                table: "user_progress");

            migrationBuilder.DropColumn(
                name: "accuracy_score_total",
                schema: "analytics",
                table: "user_progress");

            migrationBuilder.DropColumn(
                name: "active_vocabulary",
                schema: "analytics",
                table: "user_progress");

            migrationBuilder.DropColumn(
                name: "last_completed_day",
                schema: "analytics",
                table: "user_progress");
        }
    }
}
