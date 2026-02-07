using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LanzaTuIdea.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddIdeaComments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "IdeaComments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IdeaId = table.Column<int>(type: "int", nullable: false),
                    CommentedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CommentedByUserId = table.Column<int>(type: "int", nullable: false),
                    CommentedByRole = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CommentedByName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Comment = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IdeaComments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_IdeaComments_AppUsers_CommentedByUserId",
                        column: x => x.CommentedByUserId,
                        principalTable: "AppUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_IdeaComments_Ideas_IdeaId",
                        column: x => x.IdeaId,
                        principalTable: "Ideas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_IdeaComments_CommentedByUserId",
                table: "IdeaComments",
                column: "CommentedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_IdeaComments_IdeaId",
                table: "IdeaComments",
                column: "IdeaId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "IdeaComments");
        }
    }
}
