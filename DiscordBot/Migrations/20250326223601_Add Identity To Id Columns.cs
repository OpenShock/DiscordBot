using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace OpenShock.DiscordBot.Migrations
{
    /// <inheritdoc />
    public partial class AddIdentityToIdColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<long>(
                name: "id",
                table: "rejected_profanity_suggestions",
                type: "bigint",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(20,0)")
                .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

            migrationBuilder.AlterColumn<long>(
                name: "id",
                table: "profanity_suggestions",
                type: "bigint",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(20,0)")
                .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

            migrationBuilder.AlterColumn<long>(
                name: "id",
                table: "profanity_rules",
                type: "bigint",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(20,0)")
                .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<decimal>(
                name: "id",
                table: "rejected_profanity_suggestions",
                type: "numeric(20,0)",
                nullable: false,
                oldClrType: typeof(long),
                oldType: "bigint")
                .OldAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

            migrationBuilder.AlterColumn<decimal>(
                name: "id",
                table: "profanity_suggestions",
                type: "numeric(20,0)",
                nullable: false,
                oldClrType: typeof(long),
                oldType: "bigint")
                .OldAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

            migrationBuilder.AlterColumn<decimal>(
                name: "id",
                table: "profanity_rules",
                type: "numeric(20,0)",
                nullable: false,
                oldClrType: typeof(long),
                oldType: "bigint")
                .OldAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);
        }
    }
}
