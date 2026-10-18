using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EnglishSchool.Modules.Retention.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialRetention : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "retention");

            migrationBuilder.CreateTable(
                name: "flashcards",
                schema: "retention",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    vocabulary_item_id = table.Column<Guid>(type: "uuid", nullable: false),
                    kind = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    front = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    back = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    day_number = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    due_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    scheduled_days = table.Column<int>(type: "integer", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    state_difficulty = table.Column<double>(type: "double precision", nullable: false),
                    state_lapses = table.Column<int>(type: "integer", nullable: false),
                    state_last_reviewed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    state_phase = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    state_reps = table.Column<int>(type: "integer", nullable: false),
                    state_stability = table.Column<double>(type: "double precision", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_flashcards", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "outbox_messages",
                schema: "retention",
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
                name: "card_reviews",
                schema: "retention",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    flashcard_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    rating = table.Column<int>(type: "integer", nullable: false),
                    reviewed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    phase_before = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    elapsed_days = table.Column<int>(type: "integer", nullable: false),
                    scheduled_days = table.Column<int>(type: "integer", nullable: false),
                    stability = table.Column<double>(type: "double precision", nullable: false),
                    difficulty = table.Column<double>(type: "double precision", nullable: false),
                    retrievability = table.Column<double>(type: "double precision", nullable: true),
                    duration_ms = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_card_reviews", x => x.id);
                    table.ForeignKey(
                        name: "fk_card_reviews_flashcards_flashcard_id",
                        column: x => x.flashcard_id,
                        principalSchema: "retention",
                        principalTable: "flashcards",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_card_reviews_flashcard_id_reviewed_at",
                schema: "retention",
                table: "card_reviews",
                columns: new[] { "flashcard_id", "reviewed_at" });

            migrationBuilder.CreateIndex(
                name: "ix_card_reviews_user_id_reviewed_at",
                schema: "retention",
                table: "card_reviews",
                columns: new[] { "user_id", "reviewed_at" });

            migrationBuilder.CreateIndex(
                name: "ix_flashcards_user_id_due_at",
                schema: "retention",
                table: "flashcards",
                columns: new[] { "user_id", "due_at" });

            migrationBuilder.CreateIndex(
                name: "ix_flashcards_user_id_vocabulary_item_id",
                schema: "retention",
                table: "flashcards",
                columns: new[] { "user_id", "vocabulary_item_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_outbox_messages_occurred_at",
                schema: "retention",
                table: "outbox_messages",
                column: "occurred_at",
                filter: "processed_at IS NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "card_reviews",
                schema: "retention");

            migrationBuilder.DropTable(
                name: "outbox_messages",
                schema: "retention");

            migrationBuilder.DropTable(
                name: "flashcards",
                schema: "retention");
        }
    }
}
