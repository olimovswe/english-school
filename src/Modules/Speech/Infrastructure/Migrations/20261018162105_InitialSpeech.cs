using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EnglishSchool.Modules.Speech.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialSpeech : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "speech");

            migrationBuilder.CreateTable(
                name: "audio_artifacts",
                schema: "speech",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    kind = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    owner_id = table.Column<Guid>(type: "uuid", nullable: true),
                    text = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    voice = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    cache_key = table.Column<string>(type: "character(64)", fixedLength: true, maxLength: 64, nullable: true),
                    storage_key = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    content_type = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    size_bytes = table.Column<long>(type: "bigint", nullable: false),
                    duration_ms = table.Column<int>(type: "integer", nullable: true),
                    sha256 = table.Column<string>(type: "character(64)", fixedLength: true, maxLength: 64, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_audio_artifacts", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "outbox_messages",
                schema: "speech",
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
                name: "pronunciation_assessments",
                schema: "speech",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    recording_id = table.Column<Guid>(type: "uuid", nullable: false),
                    target_text = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    transcript = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    language = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: false),
                    accuracy_score = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    completeness_score = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    fluency_score = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: true),
                    overall_score = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    verdict = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    speech_duration_ms = table.Column<int>(type: "integer", nullable: true),
                    words_per_minute = table.Column<double>(type: "double precision", nullable: true),
                    pause_count = table.Column<int>(type: "integer", nullable: true),
                    longest_pause_ms = table.Column<int>(type: "integer", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    words = table.Column<string>(type: "jsonb", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_pronunciation_assessments", x => x.id);
                    table.ForeignKey(
                        name: "fk_pronunciation_assessments_audio_artifacts_recording_id",
                        column: x => x.recording_id,
                        principalSchema: "speech",
                        principalTable: "audio_artifacts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_audio_artifacts_cache_key",
                schema: "speech",
                table: "audio_artifacts",
                column: "cache_key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_audio_artifacts_owner_id_created_at",
                schema: "speech",
                table: "audio_artifacts",
                columns: new[] { "owner_id", "created_at" });

            migrationBuilder.CreateIndex(
                name: "ix_audio_artifacts_storage_key",
                schema: "speech",
                table: "audio_artifacts",
                column: "storage_key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_outbox_messages_occurred_at",
                schema: "speech",
                table: "outbox_messages",
                column: "occurred_at",
                filter: "processed_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_pronunciation_assessments_recording_id",
                schema: "speech",
                table: "pronunciation_assessments",
                column: "recording_id");

            migrationBuilder.CreateIndex(
                name: "ix_pronunciation_assessments_user_id_created_at",
                schema: "speech",
                table: "pronunciation_assessments",
                columns: new[] { "user_id", "created_at" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "outbox_messages",
                schema: "speech");

            migrationBuilder.DropTable(
                name: "pronunciation_assessments",
                schema: "speech");

            migrationBuilder.DropTable(
                name: "audio_artifacts",
                schema: "speech");
        }
    }
}
