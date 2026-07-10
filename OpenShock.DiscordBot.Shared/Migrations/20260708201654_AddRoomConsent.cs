using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OpenShock.DiscordBot.Migrations
{
    /// <inheritdoc />
    public partial class AddRoomConsent : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "allow_room_shocks",
                table: "users",
                type: "boolean",
                nullable: false,
                defaultValueSql: "false");

            migrationBuilder.AddColumn<int>(
                name: "room_max_duration_ms",
                table: "users",
                type: "integer",
                nullable: false,
                defaultValue: 3000);

            migrationBuilder.AddColumn<byte>(
                name: "room_max_intensity",
                table: "users",
                type: "smallint",
                nullable: false,
                defaultValue: (byte)30);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "allow_room_shocks",
                table: "users");

            migrationBuilder.DropColumn(
                name: "room_max_duration_ms",
                table: "users");

            migrationBuilder.DropColumn(
                name: "room_max_intensity",
                table: "users");
        }
    }
}
