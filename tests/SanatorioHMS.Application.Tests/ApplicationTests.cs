using MediatR;
using Moq;
using SanatorioHMS.Application.Auth;
using SanatorioHMS.Application.ClinicalCare;
using SanatorioHMS.Application.Diagnostics;
using SanatorioHMS.Application.PatientRegistry;
using SanatorioHMS.Application.Scheduling;
using SanatorioHMS.Domain.ClinicalCare.Entities;
using SanatorioHMS.Domain.Auth.Entities;
using SanatorioHMS.Domain.Core;
using SanatorioHMS.Domain.Diagnostics.Entities;
using SanatorioHMS.Domain.PatientRegistry.Entities;
using SanatorioHMS.Domain.Scheduling.Entities;
using Xunit;

namespace SanatorioHMS.Application.Tests;

public sealed class ApplicationTests
{
    [Fact]
    public void PatientValidator_rejects_missing_identity_document() =>
        Assert.False(new CreatePatientValidator().Validate(new CreatePatient(new("", "", DateOnly.MinValue, DocumentType.DNI, ""))).IsValid);

    [Fact]
    public void CoverageValidator_requires_payer_for_prepaid_coverage() =>
        Assert.False(new AddCoverageValidator().Validate(new AddCoverage(new(Guid.NewGuid(), CoverageType.Prepaga, null))).IsValid);

    [Fact]
    public async Task Reservation_publishes_turn_booked()
    {
        var agenda = new Agenda { Id = Guid.NewGuid(), Fecha = DateOnly.FromDateTime(DateTime.UtcNow), HoraInicio = new(8, 0), HoraFin = new(18, 0), DuracionTurnoMinutos = 30, Activo = true };
        var agendas = new Mock<ISchedulingRepository>(); agendas.Setup(x => x.GetByIdAsync(agenda.Id, It.IsAny<CancellationToken>())).ReturnsAsync(agenda);
        var turns = new Mock<ITurnRepository>(); turns.Setup(x => x.FindSlotAsync(agenda.Id, It.IsAny<DateTime>(), It.IsAny<CancellationToken>())).ReturnsAsync((Turno?)null);
        var publisher = new Mock<IPublisher>();
        var result = await new ReserveTurnHandler(agendas.Object, turns.Object, publisher.Object).Handle(new(new(agenda.Id, Guid.NewGuid(), DateTime.UtcNow.Date.AddHours(9), "Presencial", Guid.NewGuid(), null, null)), default);
        Assert.True(result.IsSuccess); publisher.Verify(x => x.Publish(It.IsAny<TurnBookedNotification>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Episode_close_is_blocked_when_required_orders_are_incomplete()
    {
        var episode = new Episode { Id = Guid.NewGuid(), Estado = "Activo" }; var repo = new Mock<IClinicalCareRepository>();
        repo.Setup(x => x.GetByIdAsync(episode.Id, It.IsAny<CancellationToken>())).ReturnsAsync(episode); repo.Setup(x => x.HasIncompleteRequiredOrdersAsync(episode.Id, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        var result = await new CloseEpisodeHandler(repo.Object).Handle(new(new(episode.Id)), default);
        Assert.False(result.IsSuccess); Assert.Equal("Activo", episode.Estado);
    }

    [Fact]
    public async Task Diagnostic_result_is_blocked_when_authorization_is_denied()
    {
        var order = DiagnosticOrder.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Study.Create("L1", "Study", StudyModality.Laboratory), AuthorizationStatus.Denied);
        var repo = new Mock<IDiagnosticsRepository>(); repo.Setup(x => x.GetByIdAsync(order.Id, It.IsAny<CancellationToken>())).ReturnsAsync(order);
        var result = await new RegisterDiagnosticResultHandler(repo.Object).Handle(new(new(order.Id, "value", Guid.NewGuid(), null)), default);
        Assert.False(result.IsSuccess); Assert.Contains("Conflict", result.Error);
    }

    [Fact]
    public async Task Login_does_not_expose_protected_data_for_unknown_user()
    {
        var repo = new Mock<IUserAuthRepository>(); repo.Setup(x => x.FindByUsernameAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync((User?)null);
        var credentials = new Mock<IUserCredentialService>(); var tokens = new Mock<IAuthTokenService>();
        var result = await new LoginHandler(repo.Object, credentials.Object, tokens.Object).Handle(new(new("unknown", "password")), default);
        Assert.False(result.IsSuccess); Assert.Equal("Invalid credentials.", result.Error); tokens.VerifyNoOtherCalls();
    }
}
