namespace SanatorioHMS.Domain.ClinicalCare.Entities;

public sealed class Episode
{
    public Guid Id { get; set; }
    public Guid PacienteId { get; set; }
    public Guid ProfesionalIngresoId { get; set; }
    public string Tipo { get; set; } = string.Empty;
    public DateTime FechaIngreso { get; set; }
    public DateTime? FechaAlta { get; set; }
    public DateTime? FechaEgreso { get; set; }
    public string Estado { get; set; } = "Activo";
    public string? MotivoIngreso { get; set; }
    public string? DiagnosticoPrincipal { get; set; }
    public string? DiagnosticoPrincipalCIE10 { get; set; }
    public string? Anamnesis { get; set; }
    public string? ExamenFisico { get; set; }
    public string? PlanTerapeutico { get; set; }
    public string ViaIngreso { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public ICollection<Encounter> Encounters { get; set; } = new List<Encounter>();
    public ICollection<ClinicalNote> ClinicalNotes { get; set; } = new List<ClinicalNote>();
    public ICollection<Order> Orders { get; set; } = new List<Order>();

    public bool IsClosed => Estado is "Alta" or "Egreso" or "Cerrado";

    public void Close(DateTime closedAt, bool requirementsSatisfied)
    {
        if (IsClosed)
        {
            throw new InvalidOperationException("The episode is already closed.");
        }

        if (!requirementsSatisfied)
        {
            throw new InvalidOperationException("The episode cannot be closed until its requirements are complete.");
        }

        Estado = "Cerrado";
        FechaAlta = closedAt;
        FechaEgreso = closedAt;
    }

    public void EnsureOpen()
    {
        if (IsClosed)
        {
            throw new InvalidOperationException("Closed clinical content cannot be edited.");
        }
    }

    public bool AddClinicalNote(ClinicalNote note)
    {
        EnsureOpen();
        if (ClinicalNotes.Any(existing => existing.Id == note.Id))
        {
            return false;
        }

        ClinicalNotes.Add(note);
        return true;
    }

    public bool AddOrder(Order order)
    {
        EnsureOpen();
        if (Orders.Any(existing => existing.Id == order.Id))
        {
            return false;
        }

        Orders.Add(order);
        return true;
    }
}

public sealed class Encounter
{
    public Guid Id { get; set; }
    public Guid EpisodioId { get; set; }
    public Episode Episode { get; set; } = null!;
    public Guid ProfesionalId { get; set; }
    public DateTime FechaHora { get; set; }
    public string Tipo { get; set; } = string.Empty;
    public string? Motivo { get; set; }
    public string? Anamnesis { get; set; }
    public string? ExamenFisico { get; set; }
    public string? Diagnosticos { get; set; }
    public string? PlanTrabajo { get; set; }
    public string? Observaciones { get; set; }
    public string Estado { get; set; } = "Realizada";
    public DateTime CreatedAt { get; set; }
    public ICollection<Order> Orders { get; set; } = new List<Order>();
}

public sealed class ClinicalNote
{
    public Guid Id { get; set; }
    public Guid EpisodioId { get; set; }
    public Episode Episode { get; set; } = null!;
    public Guid? AtencionId { get; set; }
    public Guid ProfesionalId { get; set; }
    public string TipoNota { get; set; } = string.Empty;
    public string Contenido { get; set; } = string.Empty;
    public DateTime FechaHora { get; set; }
    public bool EsPrivada { get; set; }
    public DateTime CreatedAt { get; set; }
}

public sealed class Order
{
    public Guid Id { get; set; }
    public Guid EpisodioId { get; set; }
    public Episode Episode { get; set; } = null!;
    public Guid AtencionId { get; set; }
    public Encounter Encounter { get; set; } = null!;
    public Guid ProfesionalId { get; set; }
    public string Tipo { get; set; } = string.Empty;
    public string Estado { get; set; } = "Activa";
    public DateTime FechaOrden { get; set; }
    public DateTime? FechaCumplimiento { get; set; }
    public DateTime CreatedAt { get; set; }
    public ICollection<OrderItem> Items { get; set; } = new List<OrderItem>();
}

public sealed class OrderItem
{
    public Guid Id { get; set; }
    public Guid OrdenId { get; set; }
    public Order Order { get; set; } = null!;
    public string Descripcion { get; set; } = string.Empty;
    public string? Frecuencia { get; set; }
    public string? Duracion { get; set; }
    public string? ViaAdministracion { get; set; }
    public string? Dosis { get; set; }
    public string Estado { get; set; } = "Pendiente";
    public string? Observaciones { get; set; }
}
