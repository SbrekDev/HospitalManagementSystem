using FluentValidation;
using MediatR;
using SanatorioHMS.Application.Core;
using SanatorioHMS.Domain.Core;
using SanatorioHMS.Domain.Scheduling.Entities;
using SanatorioHMS.Domain.Scheduling.Events;

namespace SanatorioHMS.Application.Scheduling;

public sealed record AgendaResponse(Guid Id, Guid ProfessionalId, Guid SpecialtyId, DateOnly Date, TimeOnly Start, TimeOnly End, int SlotMinutes);
public sealed record TurnResponse(Guid Id, Guid AgendaId, Guid PatientId, DateTime Start, string Status, string Modality, Guid? RoomId, string? TeleconsultationDestination);
public sealed record PublishAgendaRequest(Guid AgendaId);
public sealed record ReserveTurnRequest(Guid AgendaId, Guid PatientId, DateTime Start, string Modality, Guid? RoomId, string? TeleconsultationDestination, string? Reason);
public sealed record CancelTurnRequest(Guid TurnId);
public sealed record ChangeTurnStatusRequest(Guid TurnId, string Status);

public sealed record PublishAgenda(PublishAgendaRequest Data) : IRequest<Result<AgendaResponse>>, IRequirePermission { public string Permission => "Scheduling.Publish"; }
public sealed record ReserveTurn(ReserveTurnRequest Data) : IRequest<Result<TurnResponse>>, IRequirePermission { public string Permission => "Scheduling.Reserve"; }
public sealed record CancelTurn(CancelTurnRequest Data) : IRequest<Result<TurnResponse>>, IRequirePermission { public string Permission => "Scheduling.Cancel"; }
public sealed record ChangeTurnStatus(ChangeTurnStatusRequest Data) : IRequest<Result<TurnResponse>>, IRequirePermission { public string Permission => "Scheduling.ChangeStatus"; }
public sealed record GetAgenda(Guid AgendaId) : IRequest<Result<AgendaResponse>>, IRequirePermission { public string Permission => "Scheduling.Read"; }
public sealed record GetAvailableSlots(Guid AgendaId) : IRequest<Result<IReadOnlyList<DateTime>>>, IRequirePermission { public string Permission => "Scheduling.Read"; }
public sealed record GetTurnDetails(Guid TurnId) : IRequest<Result<TurnResponse>>, IRequirePermission { public string Permission => "Scheduling.Read"; }

public interface ISchedulingRepository : IRepository<Agenda>
{ Task<IReadOnlyList<Agenda>> GetAgendasAsync(CancellationToken ct = default); Task<IReadOnlyList<DateTime>> GetAvailableSlotsAsync(Guid agendaId, CancellationToken ct = default); }
public interface ITurnRepository : IRepository<Turno>
{ Task<IReadOnlyList<Turno>> GetTurnsAsync(CancellationToken ct = default); Task<Turno?> FindSlotAsync(Guid agendaId, DateTime start, CancellationToken ct = default); }
public sealed record TurnBookedNotification(TurnBooked Event) : INotification;

public sealed class ReserveTurnValidator : AbstractValidator<ReserveTurn>
{
    public ReserveTurnValidator() { RuleFor(x => x.Data.AgendaId).NotEmpty(); RuleFor(x => x.Data.PatientId).NotEmpty(); RuleFor(x => x.Data.Start).GreaterThan(DateTime.UtcNow.AddMinutes(-1)); RuleFor(x => x.Data.Modality).Must(x => x is "Presencial" or "Teleconsulta"); RuleFor(x => x.Data.RoomId).NotEmpty().When(x => x.Data.Modality == "Presencial"); RuleFor(x => x.Data.TeleconsultationDestination).NotEmpty().When(x => x.Data.Modality == "Teleconsulta"); }
}
public sealed class ChangeTurnStatusValidator : AbstractValidator<ChangeTurnStatus>
{
    public ChangeTurnStatusValidator() { RuleFor(x => x.Data.TurnId).NotEmpty(); RuleFor(x => x.Data.Status).Must(x => x is "Confirmado" or "Atendido" or "Cancelado" or "NoAsistio"); }
}

public sealed class GetAgendaHandler(ISchedulingRepository repository) : IRequestHandler<GetAgenda, Result<AgendaResponse>>
{ public async Task<Result<AgendaResponse>> Handle(GetAgenda r, CancellationToken ct) { var a = await repository.GetByIdAsync(r.AgendaId, ct); return a is null ? Result<AgendaResponse>.Failure("Agenda not found.") : Result<AgendaResponse>.Success(ToResponse(a)); } internal static AgendaResponse ToResponse(Agenda a) => new(a.Id, a.ProfesionalId, a.EspecialidadId, a.Fecha, a.HoraInicio, a.HoraFin, a.DuracionTurnoMinutos); }
public sealed class PublishAgendaHandler(ISchedulingRepository repository) : IRequestHandler<PublishAgenda, Result<AgendaResponse>>
{ public async Task<Result<AgendaResponse>> Handle(PublishAgenda r, CancellationToken ct) { var a = await repository.GetByIdAsync(r.Data.AgendaId, ct); if (a is null) return Result<AgendaResponse>.Failure("Agenda not found."); a.Activo = true; repository.Update(a); return Result<AgendaResponse>.Success(GetAgendaHandler.ToResponse(a)); } }
public sealed class GetAvailableSlotsHandler(ISchedulingRepository repository) : IRequestHandler<GetAvailableSlots, Result<IReadOnlyList<DateTime>>>
{ public async Task<Result<IReadOnlyList<DateTime>>> Handle(GetAvailableSlots r, CancellationToken ct) => Result<IReadOnlyList<DateTime>>.Success(await repository.GetAvailableSlotsAsync(r.AgendaId, ct)); }
public sealed class ReserveTurnHandler(ISchedulingRepository agendas, ITurnRepository turns, IPublisher publisher) : IRequestHandler<ReserveTurn, Result<TurnResponse>>
{
    public async Task<Result<TurnResponse>> Handle(ReserveTurn r, CancellationToken ct)
    {
        var agenda = await agendas.GetByIdAsync(r.Data.AgendaId, ct); if (agenda is null || !agenda.Activo || !agenda.IsAvailable(TimeOnly.FromDateTime(r.Data.Start), TimeOnly.FromDateTime(r.Data.Start.AddMinutes(agenda.DuracionTurnoMinutos)))) return Result<TurnResponse>.Failure("Slot is not available.");
        if (await turns.FindSlotAsync(r.Data.AgendaId, r.Data.Start, ct) is not null) return Result<TurnResponse>.Failure("Slot conflict.");
        var turn = new Turno { Id = Guid.NewGuid(), AgendaId = agenda.Id, PacienteId = r.Data.PatientId, FechaHora = r.Data.Start, DuracionMinutos = agenda.DuracionTurnoMinutos, Tipo = r.Data.Modality, SalaId = r.Data.RoomId, DestinoTeleconsulta = r.Data.TeleconsultationDestination, MotivoConsulta = r.Data.Reason, Estado = "Reservado", CreatedAt = DateTime.UtcNow }; turn.ValidateAllocation(); await turns.AddAsync(turn, ct); await publisher.Publish(new TurnBookedNotification(new TurnBooked(turn.Id, turn.PacienteId, DateTime.UtcNow)), ct); return Result<TurnResponse>.Success(ToResponse(turn));
    }
    internal static TurnResponse ToResponse(Turno x) => new(x.Id, x.AgendaId, x.PacienteId, x.FechaHora, x.Estado, x.Tipo, x.SalaId, x.DestinoTeleconsulta);
}
public sealed class GetTurnDetailsHandler(ITurnRepository repository) : IRequestHandler<GetTurnDetails, Result<TurnResponse>>
{ public async Task<Result<TurnResponse>> Handle(GetTurnDetails r, CancellationToken ct) { var t = await repository.GetByIdAsync(r.TurnId, ct); return t is null ? Result<TurnResponse>.Failure("Turn not found.") : Result<TurnResponse>.Success(ReserveTurnHandler.ToResponse(t)); } }
public sealed class CancelTurnHandler(ITurnRepository repository, IPublisher publisher) : IRequestHandler<CancelTurn, Result<TurnResponse>>
{ public async Task<Result<TurnResponse>> Handle(CancelTurn r, CancellationToken ct) { var t = await repository.GetByIdAsync(r.Data.TurnId, ct); if (t is null) return Result<TurnResponse>.Failure("Turn not found."); try { t.TransitionTo("Cancelado"); repository.Update(t); await publisher.Publish(new TurnCancelledNotification(new TurnCancelled(t.Id, DateTime.UtcNow)), ct); return Result<TurnResponse>.Success(ReserveTurnHandler.ToResponse(t)); } catch (InvalidOperationException e) { return Result<TurnResponse>.Failure(e.Message); } } }
public sealed record TurnCancelledNotification(TurnCancelled Event) : INotification;
public sealed class ChangeTurnStatusHandler(ITurnRepository repository) : IRequestHandler<ChangeTurnStatus, Result<TurnResponse>>
{ public async Task<Result<TurnResponse>> Handle(ChangeTurnStatus r, CancellationToken ct) { var t = await repository.GetByIdAsync(r.Data.TurnId, ct); if (t is null) return Result<TurnResponse>.Failure("Turn not found."); try { t.TransitionTo(r.Data.Status); repository.Update(t); return Result<TurnResponse>.Success(ReserveTurnHandler.ToResponse(t)); } catch (InvalidOperationException e) { return Result<TurnResponse>.Failure(e.Message); } } }
