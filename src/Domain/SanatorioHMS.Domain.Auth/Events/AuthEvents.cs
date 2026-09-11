using SanatorioHMS.Domain.Core;

namespace SanatorioHMS.Domain.Auth.Events;
public sealed record AuditRecorded(Guid AuditId, Guid? ActorId, string Action, DateTime OccurredAt) : IDomainEvent;
