using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OpenShock.DiscordBot.Migrations
{
    /// <inheritdoc />
    public partial class AddWidgetAuthorized : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "widget_authorized",
                table: "users",
                type: "boolean",
                nullable: false,
                defaultValueSql: "false");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "widget_authorized",
                table: "users");
        }
    }
}
