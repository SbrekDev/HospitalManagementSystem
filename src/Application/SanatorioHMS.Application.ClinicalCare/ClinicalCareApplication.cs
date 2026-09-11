using FluentValidation;
using MediatR;
using SanatorioHMS.Application.Core;
using SanatorioHMS.Domain.Core;
using SanatorioHMS.Domain.ClinicalCare.Entities;

namespace SanatorioHMS.Application.ClinicalCare;

public sealed record EpisodeResponse(Guid Id, Guid PatientId, Guid ProfessionalId, string Status, DateTime AdmissionAt);
public sealed record EncounterResponse(Guid Id, Guid EpisodeId, Guid ProfessionalId, DateTime OccurredAt, string Type);
public sealed record OrderResponse(Guid Id, Guid EpisodeId, Guid ProfessionalId, string Type, string Status);
public sealed record OpenEpisodeRequest(Guid PatientId, Guid ProfessionalId, string Type, string AdmissionRoute, string? Reason);
public sealed record CloseEpisodeRequest(Guid EpisodeId);
public sealed record AppendClinicalNoteRequest(Guid EpisodeId, Guid ProfessionalId, string NoteType, string Content, Guid? EncounterId = null);
public sealed record IssueOrderRequest(Guid EpisodeId, Guid EncounterId, Guid ProfessionalId, string Type, IReadOnlyList<string> Items);
public sealed record OpenEpisode(OpenEpisodeRequest Data) : IRequest<Result<EpisodeResponse>>, IRequirePermission { public string Permission => "Clinical.Episode.Open"; }
public sealed record CloseEpisode(CloseEpisodeRequest Data) : IRequest<Result<EpisodeResponse>>, IRequirePermission { public string Permission => "Clinical.Episode.Close"; }
public sealed record AppendClinicalNote(AppendClinicalNoteRequest Data) : IRequest<Result<Guid>>, IRequirePermission { public string Permission => "Clinical.Note.Append"; }
public sealed record IssueOrder(IssueOrderRequest Data) : IRequest<Result<OrderResponse>>, IRequirePermission { public string Permission => "Clinical.Order.Issue"; }
public sealed record GetEpisode(Guid EpisodeId) : IRequest<Result<EpisodeResponse>>, IRequirePermission { public string Permission => "Clinical.Read"; }
public sealed record GetEncounters(Guid EpisodeId) : IRequest<Result<IReadOnlyList<EncounterResponse>>>, IRequirePermission { public string Permission => "Clinical.Read"; }
public sealed record GetOrders(Guid EpisodeId) : IRequest<Result<IReadOnlyList<OrderResponse>>>, IRequirePermission { public string Permission => "Clinical.Read"; }

public interface IClinicalCareRepository : IRepository<Episode>
{
    Task<IReadOnlyList<Episode>> GetEpisodesAsync(CancellationToken ct = default);
    Task<IReadOnlyList<Encounter>> GetEncountersAsync(Guid episodeId, CancellationToken ct = default);
    Task<IReadOnlyList<Order>> GetOrdersAsync(Guid episodeId, CancellationToken ct = default);
    Task<bool> HasIncompleteRequiredOrdersAsync(Guid episodeId, CancellationToken ct = default);
}
public sealed class OpenEpisodeValidator : AbstractValidator<OpenEpisode>
{ public OpenEpisodeValidator() { RuleFor(x => x.Data.PatientId).NotEmpty(); RuleFor(x => x.Data.ProfessionalId).NotEmpty(); RuleFor(x => x.Data.Type).NotEmpty(); RuleFor(x => x.Data.AdmissionRoute).NotEmpty(); } }
public sealed class AppendClinicalNoteValidator : AbstractValidator<AppendClinicalNote>
{ public AppendClinicalNoteValidator() { RuleFor(x => x.Data.EpisodeId).NotEmpty(); RuleFor(x => x.Data.ProfessionalId).NotEmpty(); RuleFor(x => x.Data.NoteType).Must(x => x is "Evolution" or "Procedure"); RuleFor(x => x.Data.Content).NotEmpty(); } }
public sealed class IssueOrderValidator : AbstractValidator<IssueOrder>
{ public IssueOrderValidator() { RuleFor(x => x.Data.EpisodeId).NotEmpty(); RuleFor(x => x.Data.EncounterId).NotEmpty(); RuleFor(x => x.Data.ProfessionalId).NotEmpty(); RuleFor(x => x.Data.Type).Must(x => x is "Medication" or "Study" or "Procedure" or "Diet"); RuleFor(x => x.Data.Items).NotEmpty(); } }

internal static class ClinicalMappings
{
    public static EpisodeResponse Episode(Episode x) => new(x.Id, x.PacienteId, x.ProfesionalIngresoId, x.Estado, x.FechaIngreso);
    public static OrderResponse Order(Order x) => new(x.Id, x.EpisodioId, x.ProfesionalId, x.Tipo, x.Estado);
}
public sealed class OpenEpisodeHandler(IClinicalCareRepository repository) : IRequestHandler<OpenEpisode, Result<EpisodeResponse>>
{ public async Task<Result<EpisodeResponse>> Handle(OpenEpisode r, CancellationToken ct) { var e = new Episode { Id = Guid.NewGuid(), PacienteId = r.Data.PatientId, ProfesionalIngresoId = r.Data.ProfessionalId, Tipo = r.Data.Type, ViaIngreso = r.Data.AdmissionRoute, MotivoIngreso = r.Data.Reason, FechaIngreso = DateTime.UtcNow, CreatedAt = DateTime.UtcNow, Estado = "Activo" }; await repository.AddAsync(e, ct); return Result<EpisodeResponse>.Success(ClinicalMappings.Episode(e)); } }
public sealed class CloseEpisodeHandler(IClinicalCareRepository repository) : IRequestHandler<CloseEpisode, Result<EpisodeResponse>>
{ public async Task<Result<EpisodeResponse>> Handle(CloseEpisode r, CancellationToken ct) { var e = await repository.GetByIdAsync(r.Data.EpisodeId, ct); if (e is null) return Result<EpisodeResponse>.Failure("Episode not found."); if (await repository.HasIncompleteRequiredOrdersAsync(e.Id, ct)) return Result<EpisodeResponse>.Failure("Episode requirements are incomplete."); try { e.Close(DateTime.UtcNow, true); repository.Update(e); return Result<EpisodeResponse>.Success(ClinicalMappings.Episode(e)); } catch (InvalidOperationException ex) { return Result<EpisodeResponse>.Failure(ex.Message); } } }
public sealed class AppendClinicalNoteHandler(IClinicalCareRepository repository) : IRequestHandler<AppendClinicalNote, Result<Guid>>
{ public async Task<Result<Guid>> Handle(AppendClinicalNote r, CancellationToken ct) { var e = await repository.GetByIdAsync(r.Data.EpisodeId, ct); if (e is null) return Result<Guid>.Failure("Episode not found."); var note = new ClinicalNote { Id = Guid.NewGuid(), EpisodioId = e.Id, ProfesionalId = r.Data.ProfessionalId, TipoNota = r.Data.NoteType, Contenido = r.Data.Content, AtencionId = r.Data.EncounterId, FechaHora = DateTime.UtcNow, CreatedAt = DateTime.UtcNow }; try { if (!e.AddClinicalNote(note)) return Result<Guid>.Failure("Duplicate note."); repository.Update(e); return Result<Guid>.Success(note.Id); } catch (InvalidOperationException ex) { return Result<Guid>.Failure(ex.Message); } } }
public sealed class IssueOrderHandler(IClinicalCareRepository repository) : IRequestHandler<IssueOrder, Result<OrderResponse>>
{ public async Task<Result<OrderResponse>> Handle(IssueOrder r, CancellationToken ct) { var e = await repository.GetByIdAsync(r.Data.EpisodeId, ct); if (e is null) return Result<OrderResponse>.Failure("Episode not found."); var order = new Order { Id = Guid.NewGuid(), EpisodioId = e.Id, AtencionId = r.Data.EncounterId, ProfesionalId = r.Data.ProfessionalId, Tipo = r.Data.Type, FechaOrden = DateTime.UtcNow, CreatedAt = DateTime.UtcNow }; foreach (var item in r.Data.Items) order.Items.Add(new OrderItem { Id = Guid.NewGuid(), OrdenId = order.Id, Descripcion = item }); try { if (!e.AddOrder(order)) return Result<OrderResponse>.Failure("Duplicate order."); repository.Update(e); return Result<OrderResponse>.Success(ClinicalMappings.Order(order)); } catch (InvalidOperationException ex) { return Result<OrderResponse>.Failure(ex.Message); } } }
public sealed class GetEpisodeHandler(IClinicalCareRepository repository) : IRequestHandler<GetEpisode, Result<EpisodeResponse>>
{ public async Task<Result<EpisodeResponse>> Handle(GetEpisode r, CancellationToken ct) { var e = await repository.GetByIdAsync(r.EpisodeId, ct); return e is null ? Result<EpisodeResponse>.Failure("Episode not found.") : Result<EpisodeResponse>.Success(ClinicalMappings.Episode(e)); } }
public sealed class GetEncountersHandler(IClinicalCareRepository repository) : IRequestHandler<GetEncounters, Result<IReadOnlyList<EncounterResponse>>>
{ public async Task<Result<IReadOnlyList<EncounterResponse>>> Handle(GetEncounters r, CancellationToken ct) => Result<IReadOnlyList<EncounterResponse>>.Success((await repository.GetEncountersAsync(r.EpisodeId, ct)).Select(x => new EncounterResponse(x.Id, x.EpisodioId, x.ProfesionalId, x.FechaHora, x.Tipo)).ToArray()); }
public sealed class GetOrdersHandler(IClinicalCareRepository repository) : IRequestHandler<GetOrders, Result<IReadOnlyList<OrderResponse>>>
{ public async Task<Result<IReadOnlyList<OrderResponse>>> Handle(GetOrders r, CancellationToken ct) => Result<IReadOnlyList<OrderResponse>>.Success((await repository.GetOrdersAsync(r.EpisodeId, ct)).Select(ClinicalMappings.Order).ToArray()); }
