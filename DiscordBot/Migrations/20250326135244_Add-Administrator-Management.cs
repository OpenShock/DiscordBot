using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OpenShock.DiscordBot.Migrations
{
    /// <inheritdoc />
    public partial class AddAdministratorManagement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "administrators",
                columns: table => new
                {
                    discord_id = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    is_removable = table.Column<bool>(type: "boolean", nullable: false),
                    created_on = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("administrators_pkey", x => x.discord_id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "administrators");
        }
    }
}
