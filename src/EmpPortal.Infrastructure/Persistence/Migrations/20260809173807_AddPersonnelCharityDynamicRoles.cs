using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EmpPortal.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPersonnelCharityDynamicRoles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsSystem",
                schema: "identity",
                table: "Roles",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "CharityPledges",
                schema: "hr",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,0)", precision: 18, scale: 0, nullable: false),
                    Mode = table.Column<int>(type: "int", nullable: false),
                    StartPersianYear = table.Column<int>(type: "int", nullable: false),
                    StartPersianMonth = table.Column<int>(type: "int", nullable: false),
                    EndPersianYear = table.Column<int>(type: "int", nullable: true),
                    EndPersianMonth = table.Column<int>(type: "int", nullable: true),
                    Note = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsConfirmed = table.Column<bool>(type: "bit", nullable: false),
                    ConfirmedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CharityPledges", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CharityPledges_Users_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalSchema: "identity",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CharityPledges_Users_UpdatedByUserId",
                        column: x => x.UpdatedByUserId,
                        principalSchema: "identity",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CharityPledges_Users_UserId",
                        column: x => x.UserId,
                        principalSchema: "identity",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PersonnelProfiles",
                schema: "hr",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    InternalPhone = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PersonnelProfiles", x => x.UserId);
                    table.ForeignKey(
                        name: "FK_PersonnelProfiles_Users_UpdatedByUserId",
                        column: x => x.UpdatedByUserId,
                        principalSchema: "identity",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PersonnelProfiles_Users_UserId",
                        column: x => x.UserId,
                        principalSchema: "identity",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PersonnelVehicles",
                schema: "hr",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PlateNumber = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    VehicleType = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    Trim = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    Model = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    Color = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PersonnelVehicles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PersonnelVehicles_Users_UpdatedByUserId",
                        column: x => x.UpdatedByUserId,
                        principalSchema: "identity",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PersonnelVehicles_Users_UserId",
                        column: x => x.UserId,
                        principalSchema: "identity",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CharityPledges_CreatedByUserId",
                schema: "hr",
                table: "CharityPledges",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_CharityPledges_UpdatedByUserId",
                schema: "hr",
                table: "CharityPledges",
                column: "UpdatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_CharityPledges_UserId_CreatedAtUtc",
                schema: "hr",
                table: "CharityPledges",
                columns: new[] { "UserId", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_PersonnelProfiles_UpdatedByUserId",
                schema: "hr",
                table: "PersonnelProfiles",
                column: "UpdatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_PersonnelVehicles_UpdatedByUserId",
                schema: "hr",
                table: "PersonnelVehicles",
                column: "UpdatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_PersonnelVehicles_UserId",
                schema: "hr",
                table: "PersonnelVehicles",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CharityPledges",
                schema: "hr");

            migrationBuilder.DropTable(
                name: "PersonnelProfiles",
                schema: "hr");

            migrationBuilder.DropTable(
                name: "PersonnelVehicles",
                schema: "hr");

            migrationBuilder.DropColumn(
                name: "IsSystem",
                schema: "identity",
                table: "Roles");
        }
    }
}
