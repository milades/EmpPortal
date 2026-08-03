using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EmpPortal.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSessionUserForeignKey : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddForeignKey(
                name: "FK_ApplicationSessions_Users_UserId",
                schema: "security",
                table: "ApplicationSessions",
                column: "UserId",
                principalSchema: "identity",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ApplicationSessions_Users_UserId",
                schema: "security",
                table: "ApplicationSessions");
        }
    }
}
