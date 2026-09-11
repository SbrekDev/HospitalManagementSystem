using SanatorioHMS.Domain.Core;

namespace SanatorioHMS.Domain.Diagnostics.Events;
public sealed record DiagnosticOrderCreated(Guid OrderId, Guid PatientId, Guid StudyId, DateTime OccurredAt) : IDomainEvent;
public sealed record DiagnosticResultValidated(Guid ResultId, Guid OrderId, Guid ValidatedBy, DateTime OccurredAt) : IDomainEvent;
