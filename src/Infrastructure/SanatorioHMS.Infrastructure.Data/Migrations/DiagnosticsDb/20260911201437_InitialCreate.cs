using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SanatorioHMS.Infrastructure.Data.Migrations.DiagnosticsDb
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "diag");

            migrationBuilder.CreateTable(
                name: "DiagnosticOrders",
                schema: "diag",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PatientId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EpisodeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RequestingProfessionalId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StudyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StudyVersion = table.Column<int>(type: "int", nullable: false),
                    Authorization = table.Column<int>(type: "int", nullable: false),
                    Priority = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PreparationInstructions = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Fulfilled = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DiagnosticOrders", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Studies",
                schema: "diag",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Version = table.Column<int>(type: "int", nullable: false),
                    Modality = table.Column<int>(type: "int", nullable: false),
                    PreparationInstructions = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsRetired = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Studies", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DiagnosticResults",
                schema: "diag",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DiagnosticOrderId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Value = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Unit = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RecordedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RecordedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CorrectionOfId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ValidatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ValidatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DiagnosticResults", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DiagnosticResults_DiagnosticOrders_DiagnosticOrderId",
                        column: x => x.DiagnosticOrderId,
                        principalSchema: "diag",
                        principalTable: "DiagnosticOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DiagnosticOrders_PatientId_Authorization",
                schema: "diag",
                table: "DiagnosticOrders",
                columns: new[] { "PatientId", "Authorization" });

            migrationBuilder.CreateIndex(
                name: "IX_DiagnosticResults_DiagnosticOrderId",
                schema: "diag",
                table: "DiagnosticResults",
                column: "DiagnosticOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_Studies_Code_Version",
                schema: "diag",
                table: "Studies",
                columns: new[] { "Code", "Version" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DiagnosticResults",
                schema: "diag");

            migrationBuilder.DropTable(
                name: "Studies",
                schema: "diag");

            migrationBuilder.DropTable(
                name: "DiagnosticOrders",
                schema: "diag");
        }
    }
}
