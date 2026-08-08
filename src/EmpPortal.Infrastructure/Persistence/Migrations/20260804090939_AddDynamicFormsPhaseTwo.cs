using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EmpPortal.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddDynamicFormsPhaseTwo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "forms");

            migrationBuilder.CreateTable(
                name: "FormAccessRules",
                schema: "forms",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FormId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SubjectType = table.Column<int>(type: "int", nullable: false),
                    SubjectKey = table.Column<string>(type: "nvarchar(320)", maxLength: 320, nullable: false),
                    Rights = table.Column<int>(type: "int", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FormAccessRules", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FormAccessRules_Users_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalSchema: "identity",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "FormAnswerIndexes",
                schema: "forms",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SubmissionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FieldId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FieldName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    FieldType = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Sequence = table.Column<int>(type: "int", nullable: false),
                    StringValue = table.Column<string>(type: "nvarchar(700)", maxLength: 700, nullable: true),
                    DecimalValue = table.Column<decimal>(type: "decimal(38,10)", precision: 38, scale: 10, nullable: true),
                    DateTimeValue = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    BooleanValue = table.Column<bool>(type: "bit", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FormAnswerIndexes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Forms",
                schema: "forms",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Slug = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CurrentPublishedVersionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    OpensAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ClosesAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    AllowDrafts = table.Column<bool>(type: "bit", nullable: false),
                    AllowEditAfterSubmit = table.Column<bool>(type: "bit", nullable: false),
                    MaxSubmissionsPerUser = table.Column<int>(type: "int", nullable: true),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Forms", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Forms_Users_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalSchema: "identity",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Forms_Users_UpdatedByUserId",
                        column: x => x.UpdatedByUserId,
                        principalSchema: "identity",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "FormVersions",
                schema: "forms",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FormId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VersionNumber = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    DefinitionJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SchemaHash = table.Column<string>(type: "char(64)", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    PublishedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FormVersions", x => x.Id);
                    table.CheckConstraint("CK_FormVersions_DefinitionJson_IsJson", "ISJSON([DefinitionJson]) = 1");
                    table.ForeignKey(
                        name: "FK_FormVersions_Forms_FormId",
                        column: x => x.FormId,
                        principalSchema: "forms",
                        principalTable: "Forms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FormVersions_Users_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalSchema: "identity",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FormVersions_Users_UpdatedByUserId",
                        column: x => x.UpdatedByUserId,
                        principalSchema: "identity",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "FormSubmissions",
                schema: "forms",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FormId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FormVersionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SubmittedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    DataJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TrackingCode = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    SubmittedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    WithdrawnAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FormSubmissions", x => x.Id);
                    table.CheckConstraint("CK_FormSubmissions_DataJson_IsJson", "ISJSON([DataJson]) = 1");
                    table.ForeignKey(
                        name: "FK_FormSubmissions_FormVersions_FormVersionId",
                        column: x => x.FormVersionId,
                        principalSchema: "forms",
                        principalTable: "FormVersions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FormSubmissions_Forms_FormId",
                        column: x => x.FormId,
                        principalSchema: "forms",
                        principalTable: "Forms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FormSubmissions_Users_SubmittedByUserId",
                        column: x => x.SubmittedByUserId,
                        principalSchema: "identity",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FormAccessRules_CreatedByUserId",
                schema: "forms",
                table: "FormAccessRules",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_FormAccessRules_FormId_SubjectType_SubjectKey",
                schema: "forms",
                table: "FormAccessRules",
                columns: new[] { "FormId", "SubjectType", "SubjectKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FormAnswerIndexes_FieldId_DateTimeValue",
                schema: "forms",
                table: "FormAnswerIndexes",
                columns: new[] { "FieldId", "DateTimeValue" });

            migrationBuilder.CreateIndex(
                name: "IX_FormAnswerIndexes_FieldId_DecimalValue",
                schema: "forms",
                table: "FormAnswerIndexes",
                columns: new[] { "FieldId", "DecimalValue" });

            migrationBuilder.CreateIndex(
                name: "IX_FormAnswerIndexes_FieldId_StringValue",
                schema: "forms",
                table: "FormAnswerIndexes",
                columns: new[] { "FieldId", "StringValue" });

            migrationBuilder.CreateIndex(
                name: "IX_FormAnswerIndexes_SubmissionId_FieldId_Sequence",
                schema: "forms",
                table: "FormAnswerIndexes",
                columns: new[] { "SubmissionId", "FieldId", "Sequence" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Forms_CreatedByUserId",
                schema: "forms",
                table: "Forms",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Forms_CurrentPublishedVersionId",
                schema: "forms",
                table: "Forms",
                column: "CurrentPublishedVersionId");

            migrationBuilder.CreateIndex(
                name: "IX_Forms_Slug",
                schema: "forms",
                table: "Forms",
                column: "Slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Forms_Status_OpensAtUtc_ClosesAtUtc",
                schema: "forms",
                table: "Forms",
                columns: new[] { "Status", "OpensAtUtc", "ClosesAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_Forms_UpdatedByUserId",
                schema: "forms",
                table: "Forms",
                column: "UpdatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_FormSubmissions_FormId_Status_SubmittedAtUtc",
                schema: "forms",
                table: "FormSubmissions",
                columns: new[] { "FormId", "Status", "SubmittedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_FormSubmissions_FormVersionId",
                schema: "forms",
                table: "FormSubmissions",
                column: "FormVersionId");

            migrationBuilder.CreateIndex(
                name: "IX_FormSubmissions_SubmittedByUserId_FormId",
                schema: "forms",
                table: "FormSubmissions",
                columns: new[] { "SubmittedByUserId", "FormId" },
                unique: true,
                filter: "[Status] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_FormSubmissions_SubmittedByUserId_FormId_Status",
                schema: "forms",
                table: "FormSubmissions",
                columns: new[] { "SubmittedByUserId", "FormId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_FormSubmissions_TrackingCode",
                schema: "forms",
                table: "FormSubmissions",
                column: "TrackingCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FormVersions_CreatedByUserId",
                schema: "forms",
                table: "FormVersions",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_FormVersions_FormId_Status",
                schema: "forms",
                table: "FormVersions",
                columns: new[] { "FormId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_FormVersions_FormId_VersionNumber",
                schema: "forms",
                table: "FormVersions",
                columns: new[] { "FormId", "VersionNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FormVersions_UpdatedByUserId",
                schema: "forms",
                table: "FormVersions",
                column: "UpdatedByUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_FormAccessRules_Forms_FormId",
                schema: "forms",
                table: "FormAccessRules",
                column: "FormId",
                principalSchema: "forms",
                principalTable: "Forms",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_FormAnswerIndexes_FormSubmissions_SubmissionId",
                schema: "forms",
                table: "FormAnswerIndexes",
                column: "SubmissionId",
                principalSchema: "forms",
                principalTable: "FormSubmissions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Forms_FormVersions_CurrentPublishedVersionId",
                schema: "forms",
                table: "Forms",
                column: "CurrentPublishedVersionId",
                principalSchema: "forms",
                principalTable: "FormVersions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_FormVersions_Forms_FormId",
                schema: "forms",
                table: "FormVersions");

            migrationBuilder.DropTable(
                name: "FormAccessRules",
                schema: "forms");

            migrationBuilder.DropTable(
                name: "FormAnswerIndexes",
                schema: "forms");

            migrationBuilder.DropTable(
                name: "FormSubmissions",
                schema: "forms");

            migrationBuilder.DropTable(
                name: "Forms",
                schema: "forms");

            migrationBuilder.DropTable(
                name: "FormVersions",
                schema: "forms");
        }
    }
}
