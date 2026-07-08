using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OpenShock.DiscordBot.Migrations
{
    /// <inheritdoc />
    public partial class ChangeToULong : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<decimal>(
                name: "user",
                table: "users_shockers",
                type: "numeric(20,0)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric");

            migrationBuilder.AlterColumn<decimal>(
                name: "whitelisted_friend",
                table: "users_friendwhitelist",
                type: "numeric(20,0)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric");

            migrationBuilder.AlterColumn<decimal>(
                name: "user",
                table: "users_friendwhitelist",
                type: "numeric(20,0)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric");

            migrationBuilder.AlterColumn<decimal>(
                name: "discord_id",
                table: "users",
                type: "numeric(20,0)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<decimal>(
                name: "user",
                table: "users_shockers",
                type: "numeric",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(20,0)");

            migrationBuilder.AlterColumn<decimal>(
                name: "whitelisted_friend",
                table: "users_friendwhitelist",
                type: "numeric",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(20,0)");

            migrationBuilder.AlterColumn<decimal>(
                name: "user",
                table: "users_friendwhitelist",
                type: "numeric",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(20,0)");

            migrationBuilder.AlterColumn<decimal>(
                name: "discord_id",
                table: "users",
                type: "numeric",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(20,0)");
        }
    }
}
