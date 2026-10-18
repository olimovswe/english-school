using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EnglishSchool.Modules.Discipline.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialDiscipline : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "discipline");

            migrationBuilder.CreateTable(
                name: "daily_plans",
                schema: "discipline",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    day_number = table.Column<int>(type: "integer", nullable: false),
                    assigned_day = table.Column<DateOnly>(type: "date", nullable: false),
                    morning_review_passed = table.Column<bool>(type: "boolean", nullable: false),
                    gate_pass_percentage = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: true),
                    curriculum_completed = table.Column<bool>(type: "boolean", nullable: false),
                    practice_completed = table.Column<bool>(type: "boolean", nullable: false),
                    focus_completed = table.Column<bool>(type: "boolean", nullable: false),
                    day_completed = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_daily_plans", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "outbox_messages",
                schema: "discipline",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    type = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    content = table.Column<string>(type: "jsonb", nullable: false),
                    occurred_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    processed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    failed_attempts = table.Column<int>(type: "integer", nullable: false),
                    next_attempt_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    last_error = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_outbox_messages", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "study_sessions",
                schema: "discipline",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    daily_plan_id = table.Column<Guid>(type: "uuid", nullable: false),
                    status = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    started_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ended_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    total_focus_seconds = table.Column<int>(type: "integer", nullable: false),
                    minimal_day = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_study_sessions", x => x.id);
                    table.ForeignKey(
                        name: "fk_study_sessions_daily_plans_daily_plan_id",
                        column: x => x.daily_plan_id,
                        principalSchema: "discipline",
                        principalTable: "daily_plans",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "focus_blocks",
                schema: "discipline",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    study_session_id = table.Column<Guid>(type: "uuid", nullable: false),
                    block_type = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    duration_seconds = table.Column<int>(type: "integer", nullable: false),
                    distraction_count = table.Column<int>(type: "integer", nullable: false),
                    is_completed = table.Column<bool>(type: "boolean", nullable: false),
                    started_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    completed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_focus_blocks", x => x.id);
                    table.ForeignKey(
                        name: "fk_focus_blocks_study_sessions_study_session_id",
                        column: x => x.study_session_id,
                        principalSchema: "discipline",
                        principalTable: "study_sessions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_daily_plans_user_id_assigned_day",
                schema: "discipline",
                table: "daily_plans",
                columns: new[] { "user_id", "assigned_day" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_focus_blocks_study_session_id_started_at",
                schema: "discipline",
                table: "focus_blocks",
                columns: new[] { "study_session_id", "started_at" });

            migrationBuilder.CreateIndex(
                name: "ix_outbox_messages_occurred_at",
                schema: "discipline",
                table: "outbox_messages",
                column: "occurred_at",
                filter: "processed_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_study_sessions_daily_plan_id",
                schema: "discipline",
                table: "study_sessions",
                column: "daily_plan_id");

            migrationBuilder.CreateIndex(
                name: "ix_study_sessions_user_id_status",
                schema: "discipline",
                table: "study_sessions",
                columns: new[] { "user_id", "status" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "focus_blocks",
                schema: "discipline");

            migrationBuilder.DropTable(
                name: "outbox_messages",
                schema: "discipline");

            migrationBuilder.DropTable(
                name: "study_sessions",
                schema: "discipline");

            migrationBuilder.DropTable(
                name: "daily_plans",
                schema: "discipline");
        }
    }
}
