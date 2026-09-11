using MediatR;
using Moq;
using SanatorioHMS.Application.Auth;
using SanatorioHMS.Application.ClinicalCare;
using SanatorioHMS.Application.Diagnostics;
using SanatorioHMS.Application.PatientRegistry;
using SanatorioHMS.Application.Scheduling;
using SanatorioHMS.Domain.Auth.Entities;
using SanatorioHMS.Domain.ClinicalCare.Entities;
using SanatorioHMS.Domain.Diagnostics.Entities;
using SanatorioHMS.Domain.PatientRegistry.Entities;
using SanatorioHMS.Domain.Scheduling.Entities;

namespace SanatorioHMS.Application.Tests;

public sealed class HandlerCoverageTests
{
    [Fact]
    public async Task PatientHandlersCoverCreateUpdateCoverageGuardianAndSearch()
    {
        var patient = Patient.Create("Ana", "Perez", new(1990, 1, 1));
        var repo = new Mock<IPatientRegistryRepository>();
        repo.Setup(x => x.HasDocumentAsync(It.IsAny<DocumentType>(), It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(false);
        repo.Setup(x => x.GetByIdAsync(patient.Id, It.IsAny<CancellationToken>())).ReturnsAsync(patient);
        repo.Setup(x => x.SearchAsync(It.IsAny<string?>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>())).ReturnsAsync([patient]);

        var create = await new CreatePatientHandler(repo.Object).Handle(new(new("Ana", "Perez", new(1990, 1, 1), DocumentType.DNI, "123", "Phone", "555")), default);
        Assert.True(create.IsSuccess);
        repo.Setup(x => x.GetByIdAsync(It.IsAny<object>(), It.IsAny<CancellationToken>())).ReturnsAsync(patient);
        Assert.True((await new UpdatePatientHandler(repo.Object).Handle(new(new(patient.Id, "Ana", "Perez", new(1991, 1, 1))), default)).IsSuccess);
        Assert.True((await new AddCoverageHandler(repo.Object).Handle(new(new(patient.Id, CoverageType.Particular, null)), default)).IsSuccess);
        Assert.True((await new AddGuardianHandler(repo.Object).Handle(new(new(patient.Id, "Guardian", "Parent", "555", true)), default)).IsSuccess);
        Assert.Single((await new SearchPatientsHandler(repo.Object).Handle(new("Ana"), default)).Value!);
        repo.Verify(x => x.Update(patient), Times.AtLeast(3));
    }

    [Fact]
    public async Task PatientHandlersReturnNotFoundAndDuplicateFailures()
    {
        var repo = new Mock<IPatientRegistryRepository>();
        repo.Setup(x => x.HasDocumentAsync(It.IsAny<DocumentType>(), It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);
        Assert.Contains("already", (await new CreatePatientHandler(repo.Object).Handle(new(new("A", "B", new(2000, 1, 1), DocumentType.DNI, "1")), default)).Error);
        repo.Setup(x => x.GetByIdAsync(It.IsAny<object>(), It.IsAny<CancellationToken>())).ReturnsAsync((Patient?)null);
        Assert.False((await new UpdatePatientHandler(repo.Object).Handle(new(new(Guid.NewGuid(), "A", "B", new(2000, 1, 1))), default)).IsSuccess);
        Assert.False((await new AddCoverageHandler(repo.Object).Handle(new(new(Guid.NewGuid(), CoverageType.Particular, null)), default)).IsSuccess);
        Assert.False((await new AddGuardianHandler(repo.Object).Handle(new(new(Guid.NewGuid(), "A", "B", "C", true)), default)).IsSuccess);
    }

    [Fact]
    public async Task SchedulingHandlersCoverAgendaTurnQueriesAndFailures()
    {
        var agenda = new Agenda { Id = Guid.NewGuid(), ProfesionalId = Guid.NewGuid(), EspecialidadId = Guid.NewGuid(), Fecha = DateOnly.FromDateTime(DateTime.UtcNow), HoraInicio = new(8, 0), HoraFin = new(18, 0), DuracionTurnoMinutos = 30, Activo = false };
        var turn = new Turno { Id = Guid.NewGuid(), AgendaId = agenda.Id, PacienteId = Guid.NewGuid(), FechaHora = DateTime.UtcNow, Estado = "Reservado", Tipo = "Presencial", SalaId = Guid.NewGuid() };
        var agendas = new Mock<ISchedulingRepository>(); agendas.Setup(x => x.GetByIdAsync(agenda.Id, It.IsAny<CancellationToken>())).ReturnsAsync(agenda); agendas.Setup(x => x.GetAvailableSlotsAsync(agenda.Id, It.IsAny<CancellationToken>())).ReturnsAsync([DateTime.UtcNow]);
        var turns = new Mock<ITurnRepository>(); turns.Setup(x => x.GetByIdAsync(turn.Id, It.IsAny<CancellationToken>())).ReturnsAsync(turn); turns.Setup(x => x.FindSlotAsync(agenda.Id, It.IsAny<DateTime>(), It.IsAny<CancellationToken>())).ReturnsAsync((Turno?)null);
        var publisher = new Mock<IPublisher>();

        Assert.True((await new GetAgendaHandler(agendas.Object).Handle(new(agenda.Id), default)).IsSuccess);
        Assert.True((await new PublishAgendaHandler(agendas.Object).Handle(new(new(agenda.Id)), default)).IsSuccess);
        Assert.Single((await new GetAvailableSlotsHandler(agendas.Object).Handle(new(agenda.Id), default)).Value!);
        var reserved = await new ReserveTurnHandler(agendas.Object, turns.Object, publisher.Object).Handle(new(new(agenda.Id, turn.PacienteId, DateTime.UtcNow.Date.AddHours(9), "Presencial", Guid.NewGuid(), null, null)), default);
        Assert.True(reserved.IsSuccess);
        Assert.True((await new GetTurnDetailsHandler(turns.Object).Handle(new(turn.Id), default)).IsSuccess);
        Assert.True((await new CancelTurnHandler(turns.Object, publisher.Object).Handle(new(new(turn.Id)), default)).IsSuccess);
        turn.Estado = "Reservado";
        Assert.True((await new ChangeTurnStatusHandler(turns.Object).Handle(new(new(turn.Id, "Confirmado")), default)).IsSuccess);

        agendas.Setup(x => x.GetByIdAsync(It.IsAny<object>(), It.IsAny<CancellationToken>())).ReturnsAsync((Agenda?)null);
        Assert.False((await new GetAgendaHandler(agendas.Object).Handle(new(Guid.NewGuid()), default)).IsSuccess);
        turns.Setup(x => x.GetByIdAsync(It.IsAny<object>(), It.IsAny<CancellationToken>())).ReturnsAsync((Turno?)null);
        Assert.False((await new GetTurnDetailsHandler(turns.Object).Handle(new(Guid.NewGuid()), default)).IsSuccess);
        Assert.False((await new CancelTurnHandler(turns.Object, publisher.Object).Handle(new(new(Guid.NewGuid())), default)).IsSuccess);
        Assert.False((await new ChangeTurnStatusHandler(turns.Object).Handle(new(new(Guid.NewGuid(), "Confirmado")), default)).IsSuccess);
    }

    [Fact]
    public async Task ClinicalHandlersCoverAllCommandsAndFailures()
    {
        var episode = new Episode { Id = Guid.NewGuid(), Estado = "Activo" };
        var repo = new Mock<IClinicalCareRepository>();
        repo.Setup(x => x.GetByIdAsync(episode.Id, It.IsAny<CancellationToken>())).ReturnsAsync(episode);
        repo.Setup(x => x.GetEncountersAsync(episode.Id, It.IsAny<CancellationToken>())).ReturnsAsync([new Encounter { Id = Guid.NewGuid(), EpisodioId = episode.Id, Tipo = "Consulta" }]);
        repo.Setup(x => x.GetOrdersAsync(episode.Id, It.IsAny<CancellationToken>())).ReturnsAsync([new Order { Id = Guid.NewGuid(), EpisodioId = episode.Id, Tipo = "Study" }]);
        Assert.True((await new OpenEpisodeHandler(repo.Object).Handle(new(new(Guid.NewGuid(), Guid.NewGuid(), "Ambulatorio", "Urgencia", "Motivo")), default)).IsSuccess);
        Assert.True((await new AppendClinicalNoteHandler(repo.Object).Handle(new(new(episode.Id, Guid.NewGuid(), "Evolucion", "Contenido", Guid.NewGuid())), default)).IsSuccess);
        Assert.True((await new IssueOrderHandler(repo.Object).Handle(new(new(episode.Id, Guid.NewGuid(), Guid.NewGuid(), "Study", ["CBC"])), default)).IsSuccess);
        Assert.True((await new GetEpisodeHandler(repo.Object).Handle(new(episode.Id), default)).IsSuccess);
        Assert.Single((await new GetEncountersHandler(repo.Object).Handle(new(episode.Id), default)).Value!);
        Assert.Single((await new GetOrdersHandler(repo.Object).Handle(new(episode.Id), default)).Value!);
        repo.Setup(x => x.HasIncompleteRequiredOrdersAsync(episode.Id, It.IsAny<CancellationToken>())).ReturnsAsync(false);
        Assert.True((await new CloseEpisodeHandler(repo.Object).Handle(new(new(episode.Id)), default)).IsSuccess);

        repo.Setup(x => x.GetByIdAsync(It.IsAny<object>(), It.IsAny<CancellationToken>())).ReturnsAsync((Episode?)null);
        Assert.False((await new GetEpisodeHandler(repo.Object).Handle(new(Guid.NewGuid()), default)).IsSuccess);
        Assert.False((await new AppendClinicalNoteHandler(repo.Object).Handle(new(new(Guid.NewGuid(), Guid.NewGuid(), "Evolucion", "x")), default)).IsSuccess);
        Assert.False((await new IssueOrderHandler(repo.Object).Handle(new(new(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "Study", ["x"])), default)).IsSuccess);
        Assert.False((await new CloseEpisodeHandler(repo.Object).Handle(new(new(Guid.NewGuid())), default)).IsSuccess);
    }

    [Fact]
    public async Task DiagnosticsHandlersCoverOrderResultAndQueries()
    {
        var study = Study.Create("L1", "CBC", StudyModality.Laboratory);
        var order = DiagnosticOrder.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), study, AuthorizationStatus.Approved);
        var result = DiagnosticResult.Register(order.Id, "10", Guid.NewGuid());
        var repo = new Mock<IDiagnosticsRepository>();
        repo.Setup(x => x.GetStudyAsync(study.Id, It.IsAny<CancellationToken>())).ReturnsAsync(study);
        repo.Setup(x => x.GetByIdAsync(order.Id, It.IsAny<CancellationToken>())).ReturnsAsync(order);
        repo.Setup(x => x.GetResultAsync(result.Id, It.IsAny<CancellationToken>())).ReturnsAsync(result);
        repo.Setup(x => x.GetStudiesAsync(It.IsAny<string?>(), It.IsAny<CancellationToken>())).ReturnsAsync([study]);
        repo.Setup(x => x.GetPendingOrdersAsync(It.IsAny<Guid?>(), It.IsAny<CancellationToken>())).ReturnsAsync([order]);
        Assert.True((await new CreateDiagnosticOrderHandler(repo.Object).Handle(new(new(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), study.Id, AuthorizationStatus.Approved, "Routine")), default)).IsSuccess);
        Assert.True((await new RegisterDiagnosticResultHandler(repo.Object).Handle(new(new(order.Id, "10", Guid.NewGuid(), "mg/dL")), default)).IsSuccess);
        Assert.True((await new ValidateDiagnosticResultHandler(repo.Object).Handle(new(new(result.Id, Guid.NewGuid())), default)).IsSuccess);
        Assert.Single((await new GetStudiesHandler(repo.Object).Handle(new("CBC"), default)).Value!);
        Assert.True((await new GetOrderDetailsHandler(repo.Object).Handle(new(order.Id), default)).IsSuccess);
        Assert.Single((await new GetPendingOrdersHandler(repo.Object).Handle(new(order.PatientId), default)).Value!);

        repo.Setup(x => x.GetStudyAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync((Study?)null);
        Assert.False((await new CreateDiagnosticOrderHandler(repo.Object).Handle(new(new(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), AuthorizationStatus.Approved, "Routine")), default)).IsSuccess);
        repo.Setup(x => x.GetByIdAsync(It.IsAny<object>(), It.IsAny<CancellationToken>())).ReturnsAsync((DiagnosticOrder?)null);
        Assert.False((await new RegisterDiagnosticResultHandler(repo.Object).Handle(new(new(Guid.NewGuid(), "x", Guid.NewGuid(), null)), default)).IsSuccess);
        Assert.False((await new GetOrderDetailsHandler(repo.Object).Handle(new(Guid.NewGuid()), default)).IsSuccess);
        repo.Setup(x => x.GetResultAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync((DiagnosticResult?)null);
        Assert.False((await new ValidateDiagnosticResultHandler(repo.Object).Handle(new(new(Guid.NewGuid(), Guid.NewGuid())), default)).IsSuccess);
    }

    [Fact]
    public async Task AuthHandlersCoverSuccessAndFailurePaths()
    {
        var user = new User(Guid.NewGuid(), "doctor");
        var repo = new Mock<IUserAuthRepository>();
        var credentials = new Mock<IUserCredentialService>();
        var tokens = new Mock<IAuthTokenService>();
        repo.Setup(x => x.FindByUsernameAsync("doctor", It.IsAny<CancellationToken>())).ReturnsAsync(user);
        repo.Setup(x => x.GetByIdAsync(user.Id, It.IsAny<CancellationToken>())).ReturnsAsync(user);
        repo.Setup(x => x.GetPermissionsAsync(user.Id, It.IsAny<CancellationToken>())).ReturnsAsync(["Patient.Read"]);
        credentials.Setup(x => x.Verify(user, "secret")).Returns(true);
        credentials.Setup(x => x.ResetAsync(user, "token", "NewPassword123", It.IsAny<CancellationToken>())).ReturnsAsync(true);
        var auth = new AuthResponse(user.Id, user.Username, "access", "refresh", DateTime.UtcNow.AddMinutes(1), Guid.NewGuid());
        tokens.Setup(x => x.Issue(user)).Returns(auth);
        tokens.Setup(x => x.RotateAsync(It.IsAny<Guid>(), "refresh", It.IsAny<CancellationToken>())).ReturnsAsync(auth);

        Assert.True((await new LoginHandler(repo.Object, credentials.Object, tokens.Object).Handle(new(new("doctor", "secret")), default)).IsSuccess);
        Assert.True((await new RefreshTokenHandler(tokens.Object).Handle(new(new(auth.SessionId, "refresh")), default)).IsSuccess);
        Assert.True((await new LogoutHandler(tokens.Object).Handle(new(auth.SessionId), default)).IsSuccess);
        Assert.True((await new ResetPasswordHandler(repo.Object, credentials.Object).Handle(new(new(user.Id, "token", "NewPassword123")), default)).IsSuccess);
        Assert.True((await new GetCurrentUserHandler(repo.Object).Handle(new(user.Id), default)).IsSuccess);
        Assert.Single((await new GetUserPermissionsHandler(repo.Object).Handle(new(user.Id), default)).Value!);

        credentials.Setup(x => x.Verify(user, It.IsAny<string>())).Returns(false);
        Assert.False((await new LoginHandler(repo.Object, credentials.Object, tokens.Object).Handle(new(new("doctor", "bad")), default)).IsSuccess);
        tokens.Setup(x => x.RotateAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync((AuthResponse?)null);
        Assert.False((await new RefreshTokenHandler(tokens.Object).Handle(new(new(Guid.NewGuid(), "bad")), default)).IsSuccess);
        repo.Setup(x => x.GetByIdAsync(It.IsAny<object>(), It.IsAny<CancellationToken>())).ReturnsAsync((User?)null);
        Assert.False((await new ResetPasswordHandler(repo.Object, credentials.Object).Handle(new(new(Guid.NewGuid(), "bad", "NewPassword123")), default)).IsSuccess);
        Assert.False((await new GetCurrentUserHandler(repo.Object).Handle(new(Guid.NewGuid()), default)).IsSuccess);
    }
}
