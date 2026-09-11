using FluentValidation;
using MediatR;
using SanatorioHMS.Application.Core;
using SanatorioHMS.Domain.Core;
using SanatorioHMS.Domain.PatientRegistry.Entities;

namespace SanatorioHMS.Application.PatientRegistry;

public sealed record PatientResponse(Guid Id, string Name, string Surname, DateOnly DateOfBirth);
public sealed record CreatePatientRequest(string Name, string Surname, DateOnly DateOfBirth, DocumentType DocumentType, string DocumentNumber, string? ContactType = null, string? ContactValue = null);
public sealed record UpdatePatientRequest(Guid PatientId, string Name, string Surname, DateOnly DateOfBirth);
public sealed record CoverageRequest(Guid PatientId, CoverageType Type, string? Payer);
public sealed record GuardianRequest(Guid PatientId, string Name, string Relationship, string ContactData, bool IsGuardian);

public sealed record CreatePatient(CreatePatientRequest Data) : IRequest<Result<PatientResponse>>, IRequirePermission { public string Permission => "Patient.Create"; }
public sealed record UpdatePatient(UpdatePatientRequest Data) : IRequest<Result<PatientResponse>>, IRequirePermission { public string Permission => "Patient.Update"; }
public sealed record AddCoverage(CoverageRequest Data) : IRequest<Result<PatientResponse>>, IRequirePermission { public string Permission => "Patient.Update"; }
public sealed record AddGuardian(GuardianRequest Data) : IRequest<Result<PatientResponse>>, IRequirePermission { public string Permission => "Patient.Update"; }
public sealed record SearchPatients(string? Query, Guid? PatientId = null) : IRequest<Result<IReadOnlyList<PatientResponse>>>, IRequirePermission { public string Permission => "Patient.Read"; }

public interface IPatientRegistryRepository : IRepository<Patient>
{
    Task<bool> HasDocumentAsync(DocumentType type, string number, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Patient>> SearchAsync(string? query, Guid? patientId, CancellationToken cancellationToken = default);
}

public sealed class CreatePatientValidator : AbstractValidator<CreatePatient>
{
    public CreatePatientValidator()
    {
        RuleFor(x => x.Data.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Data.Surname).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Data.DocumentNumber).NotEmpty().MaximumLength(30);
        RuleFor(x => x.Data.DateOfBirth).LessThanOrEqualTo(DateOnly.FromDateTime(DateTime.UtcNow));
    }
}
public sealed class UpdatePatientValidator : AbstractValidator<UpdatePatient>
{
    public UpdatePatientValidator() { RuleFor(x => x.Data.PatientId).NotEmpty(); RuleFor(x => x.Data.Name).NotEmpty(); RuleFor(x => x.Data.Surname).NotEmpty(); }
}
public sealed class AddCoverageValidator : AbstractValidator<AddCoverage>
{
    public AddCoverageValidator() { RuleFor(x => x.Data.PatientId).NotEmpty(); RuleFor(x => x.Data.Payer).NotEmpty().When(x => x.Data.Type is CoverageType.ObraSocial or CoverageType.Prepaga); }
}
public sealed class AddGuardianValidator : AbstractValidator<AddGuardian>
{
    public AddGuardianValidator() { RuleFor(x => x.Data.PatientId).NotEmpty(); RuleFor(x => x.Data.Name).NotEmpty(); RuleFor(x => x.Data.Relationship).NotEmpty(); RuleFor(x => x.Data.ContactData).NotEmpty(); }
}

public sealed class CreatePatientHandler(IPatientRegistryRepository repository) : IRequestHandler<CreatePatient, Result<PatientResponse>>
{
    public async Task<Result<PatientResponse>> Handle(CreatePatient request, CancellationToken ct)
    {
        if (await repository.HasDocumentAsync(request.Data.DocumentType, request.Data.DocumentNumber, ct)) return Result<PatientResponse>.Failure("Document already exists.");
        var patient = Patient.Create(request.Data.Name, request.Data.Surname, request.Data.DateOfBirth);
        patient.Documents.Add(new PatientDocument { Id = Guid.NewGuid(), PatientId = patient.Id, DocumentType = request.Data.DocumentType, DocumentNumber = request.Data.DocumentNumber.Trim() });
        if (!string.IsNullOrWhiteSpace(request.Data.ContactValue)) patient.Contacts.Add(new PatientContact { Id = Guid.NewGuid(), PatientId = patient.Id, Type = request.Data.ContactType ?? "Unknown", Value = request.Data.ContactValue });
        await repository.AddAsync(patient, ct);
        return Result<PatientResponse>.Success(ToResponse(patient));
    }
    internal static PatientResponse ToResponse(Patient p) => new(p.Id, p.Name, p.Surname, p.DateOfBirth);
}

public sealed class UpdatePatientHandler(IPatientRegistryRepository repository) : IRequestHandler<UpdatePatient, Result<PatientResponse>>
{
    public async Task<Result<PatientResponse>> Handle(UpdatePatient request, CancellationToken ct)
    {
        var patient = await repository.GetByIdAsync(request.Data.PatientId, ct);
        if (patient is null) return Result<PatientResponse>.Failure("Patient not found.");
        patient.Update(request.Data.Name, request.Data.Surname, request.Data.DateOfBirth);
        repository.Update(patient);
        return Result<PatientResponse>.Success(CreatePatientHandler.ToResponse(patient));
    }
}
public sealed class AddCoverageHandler(IPatientRegistryRepository repository) : IRequestHandler<AddCoverage, Result<PatientResponse>>
{
    public async Task<Result<PatientResponse>> Handle(AddCoverage request, CancellationToken ct)
    {
        var p = await repository.GetByIdAsync(request.Data.PatientId, ct); if (p is null) return Result<PatientResponse>.Failure("Patient not found.");
        if (request.Data.Type is CoverageType.ObraSocial or CoverageType.Prepaga && string.IsNullOrWhiteSpace(request.Data.Payer)) return Result<PatientResponse>.Failure("Payer is required.");
        p.Coverages.Add(new HealthCoverage { Id = Guid.NewGuid(), PatientId = p.Id, Type = request.Data.Type, Payer = request.Data.Payer }); repository.Update(p);
        return Result<PatientResponse>.Success(CreatePatientHandler.ToResponse(p));
    }
}
public sealed class AddGuardianHandler(IPatientRegistryRepository repository) : IRequestHandler<AddGuardian, Result<PatientResponse>>
{
    public async Task<Result<PatientResponse>> Handle(AddGuardian request, CancellationToken ct)
    { var p = await repository.GetByIdAsync(request.Data.PatientId, ct); if (p is null) return Result<PatientResponse>.Failure("Patient not found."); p.Guardians.Add(new GuardianRelationship { Id = Guid.NewGuid(), PatientId = p.Id, Name = request.Data.Name, Relationship = request.Data.Relationship, ContactData = request.Data.ContactData, IsGuardian = request.Data.IsGuardian }); repository.Update(p); return Result<PatientResponse>.Success(CreatePatientHandler.ToResponse(p)); }
}
public sealed class SearchPatientsHandler(IPatientRegistryRepository repository) : IRequestHandler<SearchPatients, Result<IReadOnlyList<PatientResponse>>>
{
    public async Task<Result<IReadOnlyList<PatientResponse>>> Handle(SearchPatients request, CancellationToken ct) => Result<IReadOnlyList<PatientResponse>>.Success((await repository.SearchAsync(request.Query, request.PatientId, ct)).Select(CreatePatientHandler.ToResponse).ToArray());
}
