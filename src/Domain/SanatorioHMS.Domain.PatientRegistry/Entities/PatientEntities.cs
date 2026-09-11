using SanatorioHMS.Domain.Core;

namespace SanatorioHMS.Domain.PatientRegistry.Entities;

public enum DocumentType { DNI, LC, LE, Passport }
public enum CoverageType { ObraSocial, Prepaga, Particular }

public sealed class Patient : AggregateRoot<Guid>
{
    private Patient(Guid id) : base(id) { }
    public string Name { get; private set; } = string.Empty;
    public string Surname { get; private set; } = string.Empty;
    public DateOnly DateOfBirth { get; private set; }
    public ICollection<PatientDocument> Documents { get; } = new List<PatientDocument>();
    public ICollection<PatientContact> Contacts { get; } = new List<PatientContact>();
    public ICollection<HealthCoverage> Coverages { get; } = new List<HealthCoverage>();
    public ICollection<GuardianRelationship> Guardians { get; } = new List<GuardianRelationship>();
    public static Patient Create(string name, string surname, DateOnly dateOfBirth)
    {
        if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(surname)) throw new ArgumentException("Patient name is required.");
        return new Patient(Guid.NewGuid()) { Name = name.Trim(), Surname = surname.Trim(), DateOfBirth = dateOfBirth };
    }
    public void Update(string name, string surname, DateOnly dateOfBirth)
    {
        if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(surname)) throw new ArgumentException("Patient name is required.");
        Name = name.Trim(); Surname = surname.Trim(); DateOfBirth = dateOfBirth;
    }
}
public sealed class PatientDocument { public Guid Id { get; set; } public Guid PatientId { get; set; } public Patient Patient { get; set; } = null!; public DocumentType DocumentType { get; set; } public string DocumentNumber { get; set; } = string.Empty; }
public sealed class PatientContact { public Guid Id { get; set; } public Guid PatientId { get; set; } public Patient Patient { get; set; } = null!; public string Type { get; set; } = string.Empty; public string Value { get; set; } = string.Empty; }
public sealed class HealthCoverage { public Guid Id { get; set; } public Guid PatientId { get; set; } public Patient Patient { get; set; } = null!; public CoverageType Type { get; set; } public string? Payer { get; set; } }
public sealed class GuardianRelationship { public Guid Id { get; set; } public Guid PatientId { get; set; } public Patient Patient { get; set; } = null!; public string Name { get; set; } = string.Empty; public string Relationship { get; set; } = string.Empty; public string ContactData { get; set; } = string.Empty; public bool IsGuardian { get; set; } }
