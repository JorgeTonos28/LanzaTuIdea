using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LanzaTuIdea.Api.Migrations;

public partial class AddAppBranding : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "AppBrandings",
            columns: table => new
            {
                Id = table.Column<int>(type: "int", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                LogoPath = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                FaviconPath = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()")
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_AppBrandings", x => x.Id);
            });
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "AppBrandings");
    }
}
