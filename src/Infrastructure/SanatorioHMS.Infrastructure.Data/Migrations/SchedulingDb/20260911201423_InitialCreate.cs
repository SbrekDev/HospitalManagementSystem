using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SanatorioHMS.Infrastructure.Data.Migrations.SchedulingDb
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "sched");

            migrationBuilder.CreateTable(
                name: "Professionals",
                schema: "sched",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    NroDocumento = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TipoDocumento = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Apellido = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Nombre = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FechaNacimiento = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Sexo = table.Column<string>(type: "nvarchar(1)", nullable: true),
                    MatriculaProvincial = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    MatriculaNacional = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Telefono = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Email = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FechaIngreso = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Activo = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Professionals", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Rooms",
                schema: "sched",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Nombre = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Activo = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Rooms", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Specialties",
                schema: "sched",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Codigo = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Nombre = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Descripcion = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Area = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Activo = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Specialties", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Agendas",
                schema: "sched",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProfesionalId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EspecialidadId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Fecha = table.Column<DateOnly>(type: "date", nullable: false),
                    HoraInicio = table.Column<TimeOnly>(type: "time", nullable: false),
                    HoraFin = table.Column<TimeOnly>(type: "time", nullable: false),
                    DuracionTurnoMinutos = table.Column<int>(type: "int", nullable: false),
                    SalaId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Activo = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Agendas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Agendas_Professionals_ProfesionalId",
                        column: x => x.ProfesionalId,
                        principalSchema: "sched",
                        principalTable: "Professionals",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Agendas_Specialties_EspecialidadId",
                        column: x => x.EspecialidadId,
                        principalSchema: "sched",
                        principalTable: "Specialties",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ProfessionalSpecialties",
                schema: "sched",
                columns: table => new
                {
                    ProfessionalId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SpecialtyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FechaAsignacion = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EsPrincipal = table.Column<bool>(type: "bit", nullable: false),
                    Activo = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProfessionalSpecialties", x => new { x.ProfessionalId, x.SpecialtyId });
                    table.ForeignKey(
                        name: "FK_ProfessionalSpecialties_Professionals_ProfessionalId",
                        column: x => x.ProfessionalId,
                        principalSchema: "sched",
                        principalTable: "Professionals",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProfessionalSpecialties_Specialties_SpecialtyId",
                        column: x => x.SpecialtyId,
                        principalSchema: "sched",
                        principalTable: "Specialties",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Turns",
                schema: "sched",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AgendaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PacienteId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Numero = table.Column<int>(type: "int", nullable: false),
                    FechaHora = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DuracionMinutos = table.Column<int>(type: "int", nullable: false),
                    Estado = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Tipo = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SalaId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DestinoTeleconsulta = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    MotivoConsulta = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Observaciones = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Turns", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Turns_Agendas_AgendaId",
                        column: x => x.AgendaId,
                        principalSchema: "sched",
                        principalTable: "Agendas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Agendas_EspecialidadId",
                schema: "sched",
                table: "Agendas",
                column: "EspecialidadId");

            migrationBuilder.CreateIndex(
                name: "IX_Agendas_ProfesionalId",
                schema: "sched",
                table: "Agendas",
                column: "ProfesionalId");

            migrationBuilder.CreateIndex(
                name: "IX_ProfessionalSpecialties_SpecialtyId",
                schema: "sched",
                table: "ProfessionalSpecialties",
                column: "SpecialtyId");

            migrationBuilder.CreateIndex(
                name: "IX_Turns_AgendaId_FechaHora",
                schema: "sched",
                table: "Turns",
                columns: new[] { "AgendaId", "FechaHora" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ProfessionalSpecialties",
                schema: "sched");

            migrationBuilder.DropTable(
                name: "Rooms",
                schema: "sched");

            migrationBuilder.DropTable(
                name: "Turns",
                schema: "sched");

            migrationBuilder.DropTable(
                name: "Agendas",
                schema: "sched");

            migrationBuilder.DropTable(
                name: "Professionals",
                schema: "sched");

            migrationBuilder.DropTable(
                name: "Specialties",
                schema: "sched");
        }
    }
}
