using FluentValidation;
using MediatR;
using SanatorioHMS.Application.Core;
using SanatorioHMS.Domain.Core;
using SanatorioHMS.Domain.Diagnostics.Entities;

namespace SanatorioHMS.Application.Diagnostics;

public sealed record StudyResponse(Guid Id, string Code, string Name, StudyModality Modality, int Version, string? PreparationInstructions);
public sealed record DiagnosticOrderResponse(Guid Id, Guid PatientId, Guid EpisodeId, Guid StudyId, AuthorizationStatus Authorization, string Priority, bool Fulfilled);
public sealed record DiagnosticResultResponse(Guid Id, Guid OrderId, string Value, DiagnosticResultStatus Status, Guid? ValidatedBy);
public sealed record CreateDiagnosticOrderRequest(Guid PatientId, Guid EpisodeId, Guid ProfessionalId, Guid StudyId, AuthorizationStatus Authorization, string Priority);
public sealed record RegisterDiagnosticResultRequest(Guid OrderId, string Value, Guid RecordedBy, string? Unit);
public sealed record ValidateDiagnosticResultRequest(Guid ResultId, Guid ProfessionalId);
public sealed record CreateDiagnosticOrder(CreateDiagnosticOrderRequest Data) : IRequest<Result<DiagnosticOrderResponse>>, IRequirePermission { public string Permission => "Diagnostics.Order.Create"; }
public sealed record RegisterDiagnosticResult(RegisterDiagnosticResultRequest Data) : IRequest<Result<DiagnosticResultResponse>>, IRequirePermission { public string Permission => "Diagnostics.Result.Register"; }
public sealed record ValidateDiagnosticResult(ValidateDiagnosticResultRequest Data) : IRequest<Result<DiagnosticResultResponse>>, IRequirePermission { public string Permission => "Diagnostics.Result.Validate"; }
public sealed record GetStudies(string? Search = null) : IRequest<Result<IReadOnlyList<StudyResponse>>>, IRequirePermission { public string Permission => "Diagnostics.Study.Read"; }
public sealed record GetOrderDetails(Guid OrderId) : IRequest<Result<DiagnosticOrderResponse>>, IRequirePermission { public string Permission => "Diagnostics.Order.Read"; }
public sealed record GetPendingOrders(Guid? PatientId = null) : IRequest<Result<IReadOnlyList<DiagnosticOrderResponse>>>, IRequirePermission { public string Permission => "Diagnostics.Order.Read"; }

public interface IDiagnosticsRepository : IRepository<DiagnosticOrder>
{
    Task<Study?> GetStudyAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<Study>> GetStudiesAsync(string? search, CancellationToken ct = default);
    Task<DiagnosticResult?> GetResultAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<DiagnosticOrder>> GetPendingOrdersAsync(Guid? patientId, CancellationToken ct = default);
    Task AddResultAsync(DiagnosticResult result, CancellationToken ct = default);
    void UpdateResult(DiagnosticResult result);
}
public sealed class CreateDiagnosticOrderValidator : AbstractValidator<CreateDiagnosticOrder>
{ public CreateDiagnosticOrderValidator() { RuleFor(x => x.Data.PatientId).NotEmpty(); RuleFor(x => x.Data.EpisodeId).NotEmpty(); RuleFor(x => x.Data.ProfessionalId).NotEmpty(); RuleFor(x => x.Data.StudyId).NotEmpty(); RuleFor(x => x.Data.Priority).NotEmpty(); } }
public sealed class RegisterDiagnosticResultValidator : AbstractValidator<RegisterDiagnosticResult>
{ public RegisterDiagnosticResultValidator() { RuleFor(x => x.Data.OrderId).NotEmpty(); RuleFor(x => x.Data.RecordedBy).NotEmpty(); RuleFor(x => x.Data.Value).NotEmpty(); } }
public sealed class ValidateDiagnosticResultValidator : AbstractValidator<ValidateDiagnosticResult>
{ public ValidateDiagnosticResultValidator() { RuleFor(x => x.Data.ResultId).NotEmpty(); RuleFor(x => x.Data.ProfessionalId).NotEmpty(); } }
internal static class DiagnosticMappings
{ public static DiagnosticOrderResponse Order(DiagnosticOrder x) => new(x.Id, x.PatientId, x.EpisodeId, x.StudyId, x.Authorization, x.Priority, x.Fulfilled); public static DiagnosticResultResponse Result(DiagnosticResult x) => new(x.Id, x.DiagnosticOrderId, x.Value, x.Status, x.ValidatedBy); public static StudyResponse Study(Study x) => new(x.Id, x.Code, x.Name, x.Modality, x.Version, x.PreparationInstructions); }
public sealed class CreateDiagnosticOrderHandler(IDiagnosticsRepository repository) : IRequestHandler<CreateDiagnosticOrder, Result<DiagnosticOrderResponse>>
{ public async Task<Result<DiagnosticOrderResponse>> Handle(CreateDiagnosticOrder r, CancellationToken ct) { var s = await repository.GetStudyAsync(r.Data.StudyId, ct); if (s is null) return Result<DiagnosticOrderResponse>.Failure("Study not found."); try { var o = DiagnosticOrder.Create(r.Data.PatientId, r.Data.EpisodeId, r.Data.ProfessionalId, s, r.Data.Authorization, r.Data.Priority); await repository.AddAsync(o, ct); return Result<DiagnosticOrderResponse>.Success(DiagnosticMappings.Order(o)); } catch (InvalidOperationException ex) { return Result<DiagnosticOrderResponse>.Failure(ex.Message); } } }
public sealed class RegisterDiagnosticResultHandler(IDiagnosticsRepository repository) : IRequestHandler<RegisterDiagnosticResult, Result<DiagnosticResultResponse>>
{ public async Task<Result<DiagnosticResultResponse>> Handle(RegisterDiagnosticResult r, CancellationToken ct) { var o = await repository.GetByIdAsync(r.Data.OrderId, ct); if (o is null) return Result<DiagnosticResultResponse>.Failure("Diagnostic order not found."); try { o.Fulfill(); var result = DiagnosticResult.Register(o.Id, r.Data.Value, r.Data.RecordedBy, r.Data.Unit); repository.Update(o); await repository.AddResultAsync(result, ct); return Result<DiagnosticResultResponse>.Success(DiagnosticMappings.Result(result)); } catch (InvalidOperationException ex) { return Result<DiagnosticResultResponse>.Failure($"Conflict: {ex.Message}"); } } }
public sealed class ValidateDiagnosticResultHandler(IDiagnosticsRepository repository) : IRequestHandler<ValidateDiagnosticResult, Result<DiagnosticResultResponse>>
{ public async Task<Result<DiagnosticResultResponse>> Handle(ValidateDiagnosticResult r, CancellationToken ct) { var result = await repository.GetResultAsync(r.Data.ResultId, ct); if (result is null) return Result<DiagnosticResultResponse>.Failure("Diagnostic result not found."); try { result.Validate(r.Data.ProfessionalId); repository.UpdateResult(result); return Result<DiagnosticResultResponse>.Success(DiagnosticMappings.Result(result)); } catch (InvalidOperationException ex) { return Result<DiagnosticResultResponse>.Failure(ex.Message); } } }
public sealed class GetStudiesHandler(IDiagnosticsRepository repository) : IRequestHandler<GetStudies, Result<IReadOnlyList<StudyResponse>>>
{ public async Task<Result<IReadOnlyList<StudyResponse>>> Handle(GetStudies r, CancellationToken ct) => Result<IReadOnlyList<StudyResponse>>.Success((await repository.GetStudiesAsync(r.Search, ct)).Select(DiagnosticMappings.Study).ToArray()); }
public sealed class GetOrderDetailsHandler(IDiagnosticsRepository repository) : IRequestHandler<GetOrderDetails, Result<DiagnosticOrderResponse>>
{ public async Task<Result<DiagnosticOrderResponse>> Handle(GetOrderDetails r, CancellationToken ct) { var o = await repository.GetByIdAsync(r.OrderId, ct); return o is null ? Result<DiagnosticOrderResponse>.Failure("Diagnostic order not found.") : Result<DiagnosticOrderResponse>.Success(DiagnosticMappings.Order(o)); } }
public sealed class GetPendingOrdersHandler(IDiagnosticsRepository repository) : IRequestHandler<GetPendingOrders, Result<IReadOnlyList<DiagnosticOrderResponse>>>
{ public async Task<Result<IReadOnlyList<DiagnosticOrderResponse>>> Handle(GetPendingOrders r, CancellationToken ct) => Result<IReadOnlyList<DiagnosticOrderResponse>>.Success((await repository.GetPendingOrdersAsync(r.PatientId, ct)).Select(DiagnosticMappings.Order).ToArray()); }
