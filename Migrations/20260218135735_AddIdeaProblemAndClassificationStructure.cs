using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LanzaTuIdea.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddIdeaProblemAndClassificationStructure : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Problema",
                table: "Ideas",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Descripcion",
                table: "Classifications",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Icono",
                table: "Classifications",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Proceso",
                table: "Classifications",
                type: "nvarchar(150)",
                maxLength: 150,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Subproceso",
                table: "Classifications",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Problema",
                table: "Ideas");

            migrationBuilder.DropColumn(
                name: "Descripcion",
                table: "Classifications");

            migrationBuilder.DropColumn(
                name: "Icono",
                table: "Classifications");

            migrationBuilder.DropColumn(
                name: "Proceso",
                table: "Classifications");

            migrationBuilder.DropColumn(
                name: "Subproceso",
                table: "Classifications");
        }
    }
}
