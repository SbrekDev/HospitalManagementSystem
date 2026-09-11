using SanatorioHMS.Application.ClinicalCare;
using SanatorioHMS.Application.Diagnostics;
using SanatorioHMS.Application.Scheduling;

namespace SanatorioHMS.Application.Tests;

public sealed class ValidatorTests
{
    [Fact]
    public void ClinicalNoteValidatorRejectsUnknownNoteType()
    {
        var request = new AppendClinicalNote(new AppendClinicalNoteRequest(Guid.NewGuid(), Guid.NewGuid(), "Correction", "text"));
        Assert.False(new AppendClinicalNoteValidator().Validate(request).IsValid);
    }

    [Fact]
    public void OrderValidatorRejectsEmptyItems()
    {
        var request = new IssueOrder(new IssueOrderRequest(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "Study", []));
        Assert.False(new IssueOrderValidator().Validate(request).IsValid);
    }

    [Fact]
    public void DiagnosticResultValidatorRejectsBlankValue()
    {
        var request = new RegisterDiagnosticResult(new RegisterDiagnosticResultRequest(Guid.NewGuid(), "", Guid.NewGuid(), null));
        Assert.False(new RegisterDiagnosticResultValidator().Validate(request).IsValid);
    }

    [Fact]
    public void TurnValidatorRequiresTeleconsultationDestination()
    {
        var request = new ReserveTurn(new ReserveTurnRequest(Guid.NewGuid(), Guid.NewGuid(), DateTime.UtcNow.AddMinutes(5), "Teleconsulta", null, null, null));
        Assert.False(new ReserveTurnValidator().Validate(request).IsValid);
    }
}
