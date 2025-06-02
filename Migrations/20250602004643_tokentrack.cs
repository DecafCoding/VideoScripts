using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VideoScripts.Migrations
{
    /// <inheritdoc />
    public partial class tokentrack : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CompletionTokens",
                table: "Scripts",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "PromptTokens",
                table: "Scripts",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "TotalTokens",
                table: "Scripts",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CompletionTokens",
                table: "Scripts");

            migrationBuilder.DropColumn(
                name: "PromptTokens",
                table: "Scripts");

            migrationBuilder.DropColumn(
                name: "TotalTokens",
                table: "Scripts");
        }
    }
}
