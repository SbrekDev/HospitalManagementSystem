using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SanatorioHMS.Infrastructure.Data.Migrations.ClinicalCareDb
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "clinical");

            migrationBuilder.CreateTable(
                name: "Episodes",
                schema: "clinical",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PacienteId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProfesionalIngresoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Tipo = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FechaIngreso = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FechaAlta = table.Column<DateTime>(type: "datetime2", nullable: true),
                    FechaEgreso = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Estado = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    MotivoIngreso = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DiagnosticoPrincipal = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DiagnosticoPrincipalCIE10 = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Anamnesis = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ExamenFisico = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PlanTerapeutico = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ViaIngreso = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Episodes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ClinicalNotes",
                schema: "clinical",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EpisodioId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AtencionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ProfesionalId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TipoNota = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Contenido = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FechaHora = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EsPrivada = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClinicalNotes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ClinicalNotes_Episodes_EpisodioId",
                        column: x => x.EpisodioId,
                        principalSchema: "clinical",
                        principalTable: "Episodes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Encounters",
                schema: "clinical",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EpisodioId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProfesionalId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FechaHora = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Tipo = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Motivo = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Anamnesis = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ExamenFisico = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Diagnosticos = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PlanTrabajo = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Observaciones = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Estado = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Encounters", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Encounters_Episodes_EpisodioId",
                        column: x => x.EpisodioId,
                        principalSchema: "clinical",
                        principalTable: "Episodes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Orders",
                schema: "clinical",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EpisodioId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AtencionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProfesionalId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Tipo = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Estado = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FechaOrden = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FechaCumplimiento = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Orders", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Orders_Encounters_AtencionId",
                        column: x => x.AtencionId,
                        principalSchema: "clinical",
                        principalTable: "Encounters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Orders_Episodes_EpisodioId",
                        column: x => x.EpisodioId,
                        principalSchema: "clinical",
                        principalTable: "Episodes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "OrderItems",
                schema: "clinical",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrdenId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Descripcion = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Frecuencia = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Duracion = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ViaAdministracion = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Dosis = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Estado = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Observaciones = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrderItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OrderItems_Orders_OrdenId",
                        column: x => x.OrdenId,
                        principalSchema: "clinical",
                        principalTable: "Orders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ClinicalNotes_EpisodioId",
                schema: "clinical",
                table: "ClinicalNotes",
                column: "EpisodioId");

            migrationBuilder.CreateIndex(
                name: "IX_Encounters_EpisodioId",
                schema: "clinical",
                table: "Encounters",
                column: "EpisodioId");

            migrationBuilder.CreateIndex(
                name: "IX_OrderItems_OrdenId",
                schema: "clinical",
                table: "OrderItems",
                column: "OrdenId");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_AtencionId",
                schema: "clinical",
                table: "Orders",
                column: "AtencionId");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_EpisodioId",
                schema: "clinical",
                table: "Orders",
                column: "EpisodioId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ClinicalNotes",
                schema: "clinical");

            migrationBuilder.DropTable(
                name: "OrderItems",
                schema: "clinical");

            migrationBuilder.DropTable(
                name: "Orders",
                schema: "clinical");

            migrationBuilder.DropTable(
                name: "Encounters",
                schema: "clinical");

            migrationBuilder.DropTable(
                name: "Episodes",
                schema: "clinical");
        }
    }
}
