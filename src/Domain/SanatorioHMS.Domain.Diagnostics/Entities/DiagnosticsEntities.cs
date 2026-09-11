using SanatorioHMS.Domain.Core;
using SanatorioHMS.Domain.Diagnostics.Events;

namespace SanatorioHMS.Domain.Diagnostics.Entities;

public enum StudyModality { Laboratory, Imaging }
public enum AuthorizationStatus { NotRequired, Pending, Approved, Denied }
public enum DiagnosticResultStatus { Registered, Validated }

public sealed class Study : AggregateRoot<Guid>
{
    private Study(Guid id) : base(id) { }
    public string Code { get; private set; } = string.Empty;
    public string LoincCode => Code;
    public string Name { get; private set; } = string.Empty;
    public int Version { get; private set; }
    public StudyModality Modality { get; private set; }
    public string? PreparationInstructions { get; private set; }
    public bool IsRetired { get; private set; }

    public static Study Create(string loincCode, string name, StudyModality modality, int version = 1, string? preparationInstructions = null)
    {
        if (string.IsNullOrWhiteSpace(loincCode) || string.IsNullOrWhiteSpace(name) || version < 1) throw new ArgumentException("A study requires a LOINC code, name, and positive version.");
        return new Study(Guid.NewGuid()) { Code = loincCode.Trim(), Name = name.Trim(), Modality = modality, Version = version, PreparationInstructions = preparationInstructions };
    }
    public Study CreateVersion(string name, string? preparationInstructions = null) => Create(Code, name, Modality, Version + 1, preparationInstructions);
    public void Retire() => IsRetired = true;
    public void EnsureAvailableForNewOrder() { if (IsRetired) throw new InvalidOperationException("Retired studies cannot be used for new diagnostic orders."); }
}

public sealed class DiagnosticOrder : AggregateRoot<Guid>
{
    private DiagnosticOrder(Guid id) : base(id) { }
    public Guid PatientId { get; private set; }
    public Guid EpisodeId { get; private set; }
    public Guid RequestingProfessionalId { get; private set; }
    public Guid StudyId { get; private set; }
    public int StudyVersion { get; private set; }
    public AuthorizationStatus Authorization { get; private set; }
    public string Priority { get; private set; } = "Routine";
    public string? PreparationInstructions { get; private set; }
    public bool Fulfilled { get; private set; }

    public static DiagnosticOrder Create(Guid patientId, Guid episodeId, Guid professionalId, Study study, AuthorizationStatus authorization = AuthorizationStatus.NotRequired, string priority = "Routine")
    {
        study.EnsureAvailableForNewOrder();
        var order = new DiagnosticOrder(Guid.NewGuid()) { PatientId = patientId, EpisodeId = episodeId, RequestingProfessionalId = professionalId, StudyId = study.Id, StudyVersion = study.Version, Authorization = authorization, Priority = priority, PreparationInstructions = study.PreparationInstructions };
        order.AddDomainEvent(new DiagnosticOrderCreated(order.Id, order.PatientId, order.StudyId, DateTime.UtcNow));
        return order;
    }
    public bool CanFulfill => Authorization is AuthorizationStatus.NotRequired or AuthorizationStatus.Approved;
    public void EnsureCanFulfill() { if (!CanFulfill) throw new InvalidOperationException($"Diagnostic order cannot be fulfilled while authorization is {Authorization}."); if (Fulfilled) throw new InvalidOperationException("Diagnostic order has already been fulfilled."); }
    public void Fulfill() { EnsureCanFulfill(); Fulfilled = true; }
    public void ApproveAuthorization() => Authorization = AuthorizationStatus.Approved;
    public void DenyAuthorization() => Authorization = AuthorizationStatus.Denied;
}

public sealed class DiagnosticResult : Entity<Guid>
{
    private DiagnosticResult(Guid id) : base(id) { }
    public Guid DiagnosticOrderId { get; private set; }
    public string Value { get; private init; } = string.Empty;
    public string? Unit { get; private init; }
    public Guid RecordedBy { get; private init; }
    public DateTime RecordedAt { get; private init; }
    public DiagnosticResultStatus Status { get; private set; }
    public Guid? CorrectionOfId { get; private set; }
    public Guid? ValidatedBy { get; private set; }
    public DateTime? ValidatedAt { get; private set; }
    public static DiagnosticResult Register(Guid orderId, string value, Guid recordedBy, string? unit = null)
    {
        if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException("A diagnostic result requires a value.");
        return new DiagnosticResult(Guid.NewGuid()) { DiagnosticOrderId = orderId, Value = value, Unit = unit, RecordedBy = recordedBy, RecordedAt = DateTime.UtcNow, Status = DiagnosticResultStatus.Registered };
    }
    public DiagnosticResult Correct(string correctedValue, Guid correctedBy) { var correction = Register(DiagnosticOrderId, correctedValue, correctedBy, Unit); correction.CorrectionOfId = Id; return correction; }
    public void Validate(Guid professionalId) { if (Status == DiagnosticResultStatus.Validated) throw new InvalidOperationException("Diagnostic result is already validated."); Status = DiagnosticResultStatus.Validated; ValidatedBy = professionalId; ValidatedAt = DateTime.UtcNow; }
    public DiagnosticResultValidated ToValidatedEvent() => new(Id, DiagnosticOrderId, ValidatedBy ?? throw new InvalidOperationException("Result is not validated."), ValidatedAt ?? DateTime.UtcNow);
}
