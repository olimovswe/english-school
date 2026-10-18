using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EnglishSchool.Modules.Curriculum.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCurriculum : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "curriculum");

            migrationBuilder.CreateTable(
                name: "days",
                schema: "curriculum",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    course_version = table.Column<int>(type: "integer", nullable: false),
                    day_number = table.Column<int>(type: "integer", nullable: false),
                    week_number = table.Column<int>(type: "integer", nullable: false),
                    kind = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    vocabulary_theme = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    source_refs = table.Column<List<string>>(type: "text[]", nullable: false),
                    prepares_for = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_days", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "grammar_topics",
                schema: "curriculum",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    day_id = table.Column<Guid>(type: "uuid", nullable: false),
                    title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    formula = table.Column<List<string>>(type: "text[]", nullable: false),
                    examples = table.Column<List<string>>(type: "text[]", nullable: false),
                    notes = table.Column<List<string>>(type: "text[]", nullable: false),
                    warnings = table.Column<List<string>>(type: "text[]", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_grammar_topics", x => x.id);
                    table.ForeignKey(
                        name: "fk_grammar_topics_days_day_id",
                        column: x => x.day_id,
                        principalSchema: "curriculum",
                        principalTable: "days",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "vocabulary_items",
                schema: "curriculum",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    day_id = table.Column<Guid>(type: "uuid", nullable: false),
                    position = table.Column<int>(type: "integer", nullable: false),
                    english = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    uzbek = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    is_phrase = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_vocabulary_items", x => x.id);
                    table.ForeignKey(
                        name: "fk_vocabulary_items_days_day_id",
                        column: x => x.day_id,
                        principalSchema: "curriculum",
                        principalTable: "days",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_days_course_version_day_number",
                schema: "curriculum",
                table: "days",
                columns: new[] { "course_version", "day_number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_grammar_topics_day_id",
                schema: "curriculum",
                table: "grammar_topics",
                column: "day_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_vocabulary_items_day_id_position",
                schema: "curriculum",
                table: "vocabulary_items",
                columns: new[] { "day_id", "position" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "grammar_topics",
                schema: "curriculum");

            migrationBuilder.DropTable(
                name: "vocabulary_items",
                schema: "curriculum");

            migrationBuilder.DropTable(
                name: "days",
                schema: "curriculum");
        }
    }
}
