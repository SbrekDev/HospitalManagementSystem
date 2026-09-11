using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SanatorioHMS.Infrastructure.Data.Migrations.PatientRegistryDb
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "patient");

            migrationBuilder.CreateTable(
                name: "Patients",
                schema: "patient",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Surname = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    DateOfBirth = table.Column<DateOnly>(type: "date", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Patients", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "GuardianRelationships",
                schema: "patient",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PatientId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Relationship = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ContactData = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsGuardian = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GuardianRelationships", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GuardianRelationships_Patients_PatientId",
                        column: x => x.PatientId,
                        principalSchema: "patient",
                        principalTable: "Patients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "HealthCoverages",
                schema: "patient",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PatientId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Type = table.Column<int>(type: "int", nullable: false),
                    Payer = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HealthCoverages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HealthCoverages_Patients_PatientId",
                        column: x => x.PatientId,
                        principalSchema: "patient",
                        principalTable: "Patients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PatientContacts",
                schema: "patient",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PatientId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Type = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Value = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PatientContacts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PatientContacts_Patients_PatientId",
                        column: x => x.PatientId,
                        principalSchema: "patient",
                        principalTable: "Patients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PatientDocuments",
                schema: "patient",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PatientId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DocumentType = table.Column<int>(type: "int", nullable: false),
                    DocumentNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PatientDocuments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PatientDocuments_Patients_PatientId",
                        column: x => x.PatientId,
                        principalSchema: "patient",
                        principalTable: "Patients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_GuardianRelationships_PatientId",
                schema: "patient",
                table: "GuardianRelationships",
                column: "PatientId");

            migrationBuilder.CreateIndex(
                name: "IX_HealthCoverages_PatientId",
                schema: "patient",
                table: "HealthCoverages",
                column: "PatientId");

            migrationBuilder.CreateIndex(
                name: "IX_PatientContacts_PatientId",
                schema: "patient",
                table: "PatientContacts",
                column: "PatientId");

            migrationBuilder.CreateIndex(
                name: "IX_PatientDocuments_DocumentType_DocumentNumber",
                schema: "patient",
                table: "PatientDocuments",
                columns: new[] { "DocumentType", "DocumentNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PatientDocuments_PatientId",
                schema: "patient",
                table: "PatientDocuments",
                column: "PatientId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GuardianRelationships",
                schema: "patient");

            migrationBuilder.DropTable(
                name: "HealthCoverages",
                schema: "patient");

            migrationBuilder.DropTable(
                name: "PatientContacts",
                schema: "patient");

            migrationBuilder.DropTable(
                name: "PatientDocuments",
                schema: "patient");

            migrationBuilder.DropTable(
                name: "Patients",
                schema: "patient");
        }
    }
}
