using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RAQEEB.Migrations
{
    /// <inheritdoc />
    public partial class AddDiagnosticsLabsAndImaging : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DiagnosticFiles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PatientId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FileName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    ContentType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    StoredFileName = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    FileSizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DiagnosticFiles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DiagnosticFiles_Patients_PatientId",
                        column: x => x.PatientId,
                        principalTable: "Patients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ImagingRecords",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PatientId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StudyName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    StudyDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Finding = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    DiagnosticFileId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ImagingRecords", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ImagingRecords_DiagnosticFiles_DiagnosticFileId",
                        column: x => x.DiagnosticFileId,
                        principalTable: "DiagnosticFiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ImagingRecords_Patients_PatientId",
                        column: x => x.PatientId,
                        principalTable: "Patients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "LabResults",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PatientId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TestName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ResultValue = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Unit = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Flag = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    RecordedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DiagnosticFileId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LabResults", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LabResults_DiagnosticFiles_DiagnosticFileId",
                        column: x => x.DiagnosticFileId,
                        principalTable: "DiagnosticFiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LabResults_Patients_PatientId",
                        column: x => x.PatientId,
                        principalTable: "Patients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DiagnosticFiles_PatientId",
                table: "DiagnosticFiles",
                column: "PatientId");

            migrationBuilder.CreateIndex(
                name: "IX_ImagingRecords_DiagnosticFileId",
                table: "ImagingRecords",
                column: "DiagnosticFileId");

            migrationBuilder.CreateIndex(
                name: "IX_ImagingRecords_PatientId_StudyDate",
                table: "ImagingRecords",
                columns: new[] { "PatientId", "StudyDate" });

            migrationBuilder.CreateIndex(
                name: "IX_LabResults_DiagnosticFileId",
                table: "LabResults",
                column: "DiagnosticFileId");

            migrationBuilder.CreateIndex(
                name: "IX_LabResults_PatientId_RecordedAt",
                table: "LabResults",
                columns: new[] { "PatientId", "RecordedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ImagingRecords");

            migrationBuilder.DropTable(
                name: "LabResults");

            migrationBuilder.DropTable(
                name: "DiagnosticFiles");
        }
    }
}
