using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OpenShock.DiscordBot.Migrations
{
    /// <inheritdoc />
    public partial class AddAdvancedProfanityBehaviour : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "profanity_rules",
                columns: table => new
                {
                    id = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    trigger = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    severity_score = table.Column<float>(type: "real", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    match_whole_word = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    validation_regex = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    language_code = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    category = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    comment = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    added_by_user_id = table.Column<decimal>(type: "numeric(20,0)", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("profanity_rules_pkey", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "profanity_suggestions",
                columns: table => new
                {
                    id = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    trigger = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    comment = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    language_code = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    suggested_by_user_id = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    suggested_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("profanity_suggestions_pkey", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "rejected_profanity_suggestions",
                columns: table => new
                {
                    id = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    trigger = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    reason = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    language_code = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    suggested_by_user_id = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("rejected_profanity_suggestions_pkey", x => x.id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "profanity_rules");

            migrationBuilder.DropTable(
                name: "profanity_suggestions");

            migrationBuilder.DropTable(
                name: "rejected_profanity_suggestions");
        }
    }
}
