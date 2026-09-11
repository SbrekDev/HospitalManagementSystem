namespace SanatorioHMS.Domain.ClinicalCare.Events;

public sealed record EpisodeOpened(Guid EpisodeId, Guid PacienteId, DateTime OccurredAt);

public sealed record EpisodeClosed(Guid EpisodeId, DateTime OccurredAt);
