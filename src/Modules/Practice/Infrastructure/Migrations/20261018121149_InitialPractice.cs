using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EnglishSchool.Modules.Practice.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialPractice : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "practice");

            migrationBuilder.CreateTable(
                name: "exercises",
                schema: "practice",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    course_version = table.Column<int>(type: "integer", nullable: false),
                    day_number = table.Column<int>(type: "integer", nullable: false),
                    position = table.Column<int>(type: "integer", nullable: false),
                    type = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    instruction = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    prompt = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    explanation = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    answer_pattern = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_exercises", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "outbox_messages",
                schema: "practice",
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
                name: "user_errors",
                schema: "practice",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    error_code = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    occurrences = table.Column<int>(type: "integer", nullable: false),
                    first_seen_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    last_seen_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    last_attempt_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_user_errors", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "attempts",
                schema: "practice",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    exercise_id = table.Column<Guid>(type: "uuid", nullable: false),
                    answer = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    verdict = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    score = table.Column<decimal>(type: "numeric(3,2)", precision: 3, scale: 2, nullable: false),
                    evaluation_tier = table.Column<int>(type: "integer", nullable: false),
                    time_taken_ms = table.Column<int>(type: "integer", nullable: false),
                    submitted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    detected_error_codes = table.Column<List<string>>(type: "text[]", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_attempts", x => x.id);
                    table.ForeignKey(
                        name: "fk_attempts_exercises_exercise_id",
                        column: x => x.exercise_id,
                        principalSchema: "practice",
                        principalTable: "exercises",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "exercise_options",
                schema: "practice",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    exercise_id = table.Column<Guid>(type: "uuid", nullable: false),
                    position = table.Column<int>(type: "integer", nullable: false),
                    text = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    is_correct = table.Column<bool>(type: "boolean", nullable: false),
                    match_text = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    error_code = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    feedback = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_exercise_options", x => x.id);
                    table.ForeignKey(
                        name: "fk_exercise_options_exercises_exercise_id",
                        column: x => x.exercise_id,
                        principalSchema: "practice",
                        principalTable: "exercises",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_attempts_exercise_id",
                schema: "practice",
                table: "attempts",
                column: "exercise_id");

            migrationBuilder.CreateIndex(
                name: "ix_attempts_user_id_submitted_at",
                schema: "practice",
                table: "attempts",
                columns: new[] { "user_id", "submitted_at" });

            migrationBuilder.CreateIndex(
                name: "ix_exercise_options_exercise_id_position",
                schema: "practice",
                table: "exercise_options",
                columns: new[] { "exercise_id", "position" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_exercises_course_version_day_number_position",
                schema: "practice",
                table: "exercises",
                columns: new[] { "course_version", "day_number", "position" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_outbox_messages_occurred_at",
                schema: "practice",
                table: "outbox_messages",
                column: "occurred_at",
                filter: "processed_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_user_errors_user_id_error_code",
                schema: "practice",
                table: "user_errors",
                columns: new[] { "user_id", "error_code" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "attempts",
                schema: "practice");

            migrationBuilder.DropTable(
                name: "exercise_options",
                schema: "practice");

            migrationBuilder.DropTable(
                name: "outbox_messages",
                schema: "practice");

            migrationBuilder.DropTable(
                name: "user_errors",
                schema: "practice");

            migrationBuilder.DropTable(
                name: "exercises",
                schema: "practice");
        }
    }
}
