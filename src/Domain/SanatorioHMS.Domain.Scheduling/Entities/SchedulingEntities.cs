namespace SanatorioHMS.Domain.Scheduling.Entities;

public sealed class Professional
{
    public Guid Id { get; set; }
    public string NroDocumento { get; set; } = string.Empty;
    public string TipoDocumento { get; set; } = string.Empty;
    public string Apellido { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public DateTime? FechaNacimiento { get; set; }
    public char? Sexo { get; set; }
    public string? MatriculaProvincial { get; set; }
    public string? MatriculaNacional { get; set; }
    public string? Telefono { get; set; }
    public string? Email { get; set; }
    public DateTime? FechaIngreso { get; set; }
    public bool Activo { get; set; } = true;
    public ICollection<ProfessionalSpecialty> ProfessionalSpecialties { get; set; } = new List<ProfessionalSpecialty>();
    public ICollection<Agenda> Agendas { get; set; } = new List<Agenda>();
}

public sealed class Specialty
{
    public Guid Id { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public string? Area { get; set; }
    public bool Activo { get; set; } = true;
    public ICollection<ProfessionalSpecialty> Professionals { get; set; } = new List<ProfessionalSpecialty>();
}

public sealed class ProfessionalSpecialty
{
    public Guid ProfessionalId { get; set; }
    public Professional Professional { get; set; } = null!;
    public Guid SpecialtyId { get; set; }
    public Specialty Specialty { get; set; } = null!;
    public DateTime FechaAsignacion { get; set; }
    public bool EsPrincipal { get; set; }
    public bool Activo { get; set; } = true;
}

public sealed class Room
{
    public Guid Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public bool Activo { get; set; } = true;
}

public sealed class Agenda
{
    public Guid Id { get; set; }
    public Guid ProfesionalId { get; set; }
    public Professional Profesional { get; set; } = null!;
    public Guid EspecialidadId { get; set; }
    public Specialty Especialidad { get; set; } = null!;
    public DateOnly Fecha { get; set; }
    public TimeOnly HoraInicio { get; set; }
    public TimeOnly HoraFin { get; set; }
    public int DuracionTurnoMinutos { get; set; } = 30;
    public Guid? SalaId { get; set; }
    public bool Activo { get; set; } = true;
    public ICollection<Turno> Turnos { get; set; } = new List<Turno>();

    public bool Contains(TimeOnly start, TimeOnly end) => start >= HoraInicio && end <= HoraFin && end > start;
    public bool IsAvailable(TimeOnly start, TimeOnly end) => Contains(start, end) && !Turnos.Any(existing =>
    {
        var existingStart = TimeOnly.FromDateTime(existing.FechaHora);
        var existingEnd = existingStart.AddMinutes(existing.DuracionMinutos);
        return existingStart < end && start < existingEnd;
    });
}

public sealed class Turno
{
    private static readonly HashSet<string> ValidStatuses = new(StringComparer.Ordinal)
    {
        "Reservado", "Confirmado", "Atendido", "Cancelado", "NoAsistio"
    };

    public Guid Id { get; set; }
    public Guid AgendaId { get; set; }
    public Agenda Agenda { get; set; } = null!;
    public Guid PacienteId { get; set; }
    public int Numero { get; set; }
    public DateTime FechaHora { get; set; }
    public int DuracionMinutos { get; set; } = 30;
    public string Estado { get; set; } = "Reservado";
    public string Tipo { get; set; } = "Presencial";
    public Guid? SalaId { get; set; }
    public string? DestinoTeleconsulta { get; set; }
    public string? MotivoConsulta { get; set; }
    public string? Observaciones { get; set; }
    public DateTime CreatedAt { get; set; }

    public bool IsValidStatus => ValidStatuses.Contains(Estado);

    public bool CanTransitionTo(string target) => Estado switch
    {
        "Reservado" => target is "Confirmado" or "Cancelado" or "NoAsistio",
        "Confirmado" => target is "Atendido" or "Cancelado" or "NoAsistio",
        _ => false
    };

    public void TransitionTo(string target)
    {
        if (!ValidStatuses.Contains(target) || !CanTransitionTo(target))
        {
            throw new InvalidOperationException($"Invalid appointment transition from '{Estado}' to '{target}'.");
        }

        Estado = target;
    }

    public void ValidateAllocation()
    {
        if (Tipo == "Presencial" && SalaId is null)
        {
            throw new InvalidOperationException("In-person appointments require a room.");
        }

        if (Tipo == "Teleconsulta" && string.IsNullOrWhiteSpace(DestinoTeleconsulta))
        {
            throw new InvalidOperationException("Teleconsultation appointments require a destination.");
        }
    }
}
