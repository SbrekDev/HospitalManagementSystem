namespace SanatorioHMS.Domain.Scheduling.Events;

public sealed record TurnBooked(Guid TurnoId, Guid PacienteId, DateTime OccurredAt);

public sealed record TurnCancelled(Guid TurnoId, DateTime OccurredAt);
