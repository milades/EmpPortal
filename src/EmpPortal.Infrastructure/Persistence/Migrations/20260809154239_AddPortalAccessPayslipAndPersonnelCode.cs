using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EmpPortal.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPortalAccessPayslipAndPersonnelCode : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "hr");

            migrationBuilder.AddColumn<string>(
                name: "PersonnelCode",
                schema: "identity",
                table: "Users",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "PayslipPeriodSettings",
                schema: "hr",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PersianYear = table.Column<int>(type: "int", nullable: false),
                    PersianMonth = table.Column<int>(type: "int", nullable: false),
                    IsVisibleToEmployees = table.Column<bool>(type: "bit", nullable: false),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PayslipPeriodSettings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PayslipPeriodSettings_Users_UpdatedByUserId",
                        column: x => x.UpdatedByUserId,
                        principalSchema: "identity",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PortalAccessGrants",
                schema: "security",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ResourceKey = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    SubjectType = table.Column<int>(type: "int", nullable: false),
                    SubjectKey = table.Column<string>(type: "nvarchar(320)", maxLength: 320, nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PortalAccessGrants", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PortalAccessGrants_Users_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalSchema: "identity",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Users_PersonnelCode",
                schema: "identity",
                table: "Users",
                column: "PersonnelCode",
                unique: true,
                filter: "[PersonnelCode] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_PayslipPeriodSettings_PersianYear_PersianMonth",
                schema: "hr",
                table: "PayslipPeriodSettings",
                columns: new[] { "PersianYear", "PersianMonth" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PayslipPeriodSettings_UpdatedByUserId",
                schema: "hr",
                table: "PayslipPeriodSettings",
                column: "UpdatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_PortalAccessGrants_CreatedByUserId",
                schema: "security",
                table: "PortalAccessGrants",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_PortalAccessGrants_ResourceKey",
                schema: "security",
                table: "PortalAccessGrants",
                column: "ResourceKey");

            migrationBuilder.CreateIndex(
                name: "IX_PortalAccessGrants_ResourceKey_SubjectType_SubjectKey",
                schema: "security",
                table: "PortalAccessGrants",
                columns: new[] { "ResourceKey", "SubjectType", "SubjectKey" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PayslipPeriodSettings",
                schema: "hr");

            migrationBuilder.DropTable(
                name: "PortalAccessGrants",
                schema: "security");

            migrationBuilder.DropIndex(
                name: "IX_Users_PersonnelCode",
                schema: "identity",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "PersonnelCode",
                schema: "identity",
                table: "Users");
        }
    }
}
