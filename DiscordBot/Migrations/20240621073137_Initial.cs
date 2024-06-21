using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OpenShock.DiscordBot.Migrations
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "users",
                columns: table => new
                {
                    discord_id = table.Column<decimal>(type: "numeric", nullable: false),
                    openshock_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_on = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    api_server = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    api_key = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("users_pkey", x => x.discord_id);
                });

            migrationBuilder.CreateTable(
                name: "users_friendwhitelist",
                columns: table => new
                {
                    user = table.Column<decimal>(type: "numeric", nullable: false),
                    whitelisted_friend = table.Column<decimal>(type: "numeric", nullable: false),
                    created_on = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("users_friendwhitelist_pkey", x => new { x.user, x.whitelisted_friend });
                    table.ForeignKey(
                        name: "fk_user",
                        column: x => x.user,
                        principalTable: "users",
                        principalColumn: "discord_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "users_shockers",
                columns: table => new
                {
                    user = table.Column<decimal>(type: "numeric", nullable: false),
                    shocker_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("users_shockers_pkey", x => new { x.user, x.shocker_id });
                    table.ForeignKey(
                        name: "fk_user",
                        column: x => x.user,
                        principalTable: "users",
                        principalColumn: "discord_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "users_openshock_id",
                table: "users",
                column: "openshock_id",
                unique: true)
                .Annotation("Npgsql:StorageParameter:deduplicate_items", "true");

            migrationBuilder.CreateIndex(
                name: "friend_id",
                table: "users_friendwhitelist",
                column: "whitelisted_friend")
                .Annotation("Npgsql:StorageParameter:deduplicate_items", "true");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "users_friendwhitelist");

            migrationBuilder.DropTable(
                name: "users_shockers");

            migrationBuilder.DropTable(
                name: "users");
        }
    }
}
