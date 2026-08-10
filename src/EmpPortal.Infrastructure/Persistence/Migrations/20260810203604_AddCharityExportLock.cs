using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EmpPortal.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCharityExportLock : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ResultsExportedAtUtc",
                schema: "hr",
                table: "CharityPledges",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ResultsExportedByUserId",
                schema: "hr",
                table: "CharityPledges",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_CharityPledges_ResultsExportedAtUtc",
                schema: "hr",
                table: "CharityPledges",
                column: "ResultsExportedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_CharityPledges_ResultsExportedByUserId",
                schema: "hr",
                table: "CharityPledges",
                column: "ResultsExportedByUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_CharityPledges_Users_ResultsExportedByUserId",
                schema: "hr",
                table: "CharityPledges",
                column: "ResultsExportedByUserId",
                principalSchema: "identity",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CharityPledges_Users_ResultsExportedByUserId",
                schema: "hr",
                table: "CharityPledges");

            migrationBuilder.DropIndex(
                name: "IX_CharityPledges_ResultsExportedAtUtc",
                schema: "hr",
                table: "CharityPledges");

            migrationBuilder.DropIndex(
                name: "IX_CharityPledges_ResultsExportedByUserId",
                schema: "hr",
                table: "CharityPledges");

            migrationBuilder.DropColumn(
                name: "ResultsExportedAtUtc",
                schema: "hr",
                table: "CharityPledges");

            migrationBuilder.DropColumn(
                name: "ResultsExportedByUserId",
                schema: "hr",
                table: "CharityPledges");
        }
    }
}
