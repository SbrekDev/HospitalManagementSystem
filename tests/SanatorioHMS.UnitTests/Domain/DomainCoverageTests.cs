using SanatorioHMS.Domain.Auth.Entities;
using SanatorioHMS.Domain.Core;
using SanatorioHMS.Domain.Diagnostics.Entities;
using SanatorioHMS.Domain.PatientRegistry.Entities;
using SanatorioHMS.Domain.Scheduling.Entities;

namespace SanatorioHMS.UnitTests.Domain;

public sealed class DomainCoverageTests
{
    [Fact]
    public void TurnStateMachineSupportsEveryValidTransition()
    {
        var reserved = new Turno { Estado = "Reservado" };
        Assert.True(reserved.CanTransitionTo("Confirmado"));
        Assert.True(reserved.CanTransitionTo("Cancelado"));
        Assert.True(reserved.CanTransitionTo("NoAsistio"));
        reserved.TransitionTo("Confirmado");
        Assert.True(reserved.CanTransitionTo("Atendido"));
        reserved.TransitionTo("Atendido");
        Assert.True(reserved.IsValidStatus);

        foreach (var terminal in new[] { "Atendido", "Cancelado", "NoAsistio" })
        {
            var turn = new Turno { Estado = terminal };
            Assert.False(turn.CanTransitionTo("Confirmado"));
            Assert.Throws<InvalidOperationException>(() => turn.TransitionTo("Reservado"));
        }
    }

    [Theory]
    [InlineData("Presencial", null, null)]
    [InlineData("Teleconsulta", null, " ")]
    public void TurnAllocationRejectsMissingRequiredResource(string modality, Guid? room, string? destination)
    {
        var turn = new Turno { Tipo = modality, SalaId = room, DestinoTeleconsulta = destination };
        Assert.Throws<InvalidOperationException>(turn.ValidateAllocation);
    }

    [Fact]
    public void TurnAllocationAcceptsBothModalities()
    {
        new Turno { Tipo = "Presencial", SalaId = Guid.NewGuid() }.ValidateAllocation();
        new Turno { Tipo = "Teleconsulta", DestinoTeleconsulta = "https://meet.test" }.ValidateAllocation();
    }

    [Fact]
    public void AgendaContainsOnlyPositiveIntervalsInsideWorkingHours()
    {
        var agenda = new Agenda { HoraInicio = new(8, 0), HoraFin = new(18, 0) };
        Assert.True(agenda.Contains(new(9, 0), new(9, 30)));
        Assert.False(agenda.Contains(new(7, 59), new(8, 30)));
        Assert.False(agenda.Contains(new(9, 30), new(9, 30)));
        Assert.False(agenda.Contains(new(17, 45), new(18, 30)));
    }

    [Fact]
    public void DiagnosticOrderAuthorizationGateCoversAllStates()
    {
        var study = Study.Create("L1", "Study", StudyModality.Laboratory);
        var notRequired = DiagnosticOrder.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), study);
        notRequired.Fulfill();
        Assert.True(notRequired.Fulfilled);
        Assert.Throws<InvalidOperationException>(notRequired.Fulfill);

        var pending = DiagnosticOrder.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), study, AuthorizationStatus.Pending);
        Assert.False(pending.CanFulfill);
        Assert.Throws<InvalidOperationException>(pending.Fulfill);
        pending.ApproveAuthorization();
        pending.Fulfill();

        var denied = DiagnosticOrder.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), study, AuthorizationStatus.Approved);
        denied.DenyAuthorization();
        Assert.Throws<InvalidOperationException>(denied.Fulfill);
    }

    [Fact]
    public void StudyVersionsAndRetirementAreEnforced()
    {
        var study = Study.Create(" L1 ", " Blood ", StudyModality.Laboratory, preparationInstructions: "Fasting");
        var version = study.CreateVersion("Blood v2");
        Assert.Equal("L1", study.Code);
        Assert.Equal(2, version.Version);
        study.Retire();
        Assert.Throws<InvalidOperationException>(study.EnsureAvailableForNewOrder);
        Assert.Throws<ArgumentException>(() => Study.Create("", "x", StudyModality.Imaging));
        Assert.Throws<ArgumentException>(() => Study.Create("x", "", StudyModality.Imaging));
        Assert.Throws<ArgumentException>(() => Study.Create("x", "x", StudyModality.Imaging, 0));
    }

    [Fact]
    public void DiagnosticResultsValidateOnceAndExposeEventOnlyAfterValidation()
    {
        var result = DiagnosticResult.Register(Guid.NewGuid(), "10", Guid.NewGuid());
        Assert.Throws<InvalidOperationException>(result.ToValidatedEvent);
        result.Validate(Guid.NewGuid());
        Assert.Equal(DiagnosticResultStatus.Validated, result.Status);
        Assert.NotNull(result.ToValidatedEvent());
        Assert.Throws<InvalidOperationException>(() => result.Validate(Guid.NewGuid()));
        Assert.Throws<ArgumentException>(() => DiagnosticResult.Register(Guid.NewGuid(), " ", Guid.NewGuid()));
    }

    [Fact]
    public void PatientAndAuthRulesRejectInvalidInputAndManageState()
    {
        Assert.Throws<ArgumentException>(() => Patient.Create("", "x", DateOnly.MinValue));
        var patient = Patient.Create(" Ana ", " Perez ", new(1990, 1, 1));
        patient.Update(" Ana María ", " Perez ", new(1991, 2, 2));
        Assert.Equal("Ana María", patient.Name);
        Assert.Throws<ArgumentException>(() => patient.Update(" ", "Perez", patient.DateOfBirth));

        var user = new User(Guid.NewGuid(), " user ");
        var role = new Role(Guid.NewGuid(), "Doctor");
        user.AssignRole(role);
        user.AssignRole(role);
        Assert.Single(user.UserRoles);
        user.Disable(); Assert.False(user.IsActive); user.Enable(); Assert.True(user.IsActive);
        Assert.Throws<ArgumentException>(() => new User(Guid.NewGuid(), ""));
        Assert.Throws<ArgumentException>(() => new Role(Guid.NewGuid(), ""));
        Assert.Throws<ArgumentException>(() => new Permission(Guid.NewGuid(), ""));

        var session = new Session(Guid.NewGuid(), user.Id, DateTime.UtcNow.AddMinutes(1), "old");
        Assert.True(session.IsValid(DateTime.UtcNow));
        session.Rotate("new", DateTime.UtcNow.AddMinutes(2)); session.Revoke();
        Assert.False(session.IsValid(DateTime.UtcNow));
    }

    [Fact]
    public void AuditChainAndAggregateEventsAreConsumable()
    {
        var first = AuditLog.Record(null, "Create", "Patient", "Success");
        var second = AuditLog.Record(Guid.NewGuid(), "Update", "Patient", "Success", first.Hash);
        Assert.True(second.LinksTo(first));
        Assert.False(second.LinksTo(AuditLog.Record(null, "Other", "Patient", "Success")));
        Assert.Single(first.DomainEvents);
        Assert.Single(first.DequeueDomainEvents());
        Assert.Empty(first.DomainEvents);
        Assert.Throws<ArgumentException>(() => AuditLog.Record(null, "", "Patient", "Failure"));
    }

    private sealed record PairValue(string Left, int Right) : ValueObject
    {
        protected override IEnumerable<object?> GetEqualityComponents() => [Left, Right];
    }

    [Fact]
    public void ValueObjectsCompareByComponents()
    {
        var first = new PairValue("x", 1);
        Assert.Equal(first, new PairValue("x", 1));
        Assert.NotEqual(first, new PairValue("x", 2));
        Assert.Equal(first.GetHashCode(), new PairValue("x", 1).GetHashCode());
    }
}
