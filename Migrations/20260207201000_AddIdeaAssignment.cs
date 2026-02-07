using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LanzaTuIdea.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddIdeaAssignment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AssignedToUserId",
                table: "Ideas",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Ideas_AssignedToUserId",
                table: "Ideas",
                column: "AssignedToUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Ideas_AppUsers_AssignedToUserId",
                table: "Ideas",
                column: "AssignedToUserId",
                principalTable: "AppUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Ideas_AppUsers_AssignedToUserId",
                table: "Ideas");

            migrationBuilder.DropIndex(
                name: "IX_Ideas_AssignedToUserId",
                table: "Ideas");

            migrationBuilder.DropColumn(
                name: "AssignedToUserId",
                table: "Ideas");
        }
    }
}
