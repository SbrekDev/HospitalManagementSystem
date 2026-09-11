using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using SanatorioHMS.Api.Client;
using SanatorioHMS.Application.Auth;
using SanatorioHMS.Application.ClinicalCare;
using SanatorioHMS.Application.Diagnostics;
using SanatorioHMS.Application.PatientRegistry;
using SanatorioHMS.Application.Scheduling;
using SanatorioHMS.Desktop.ViewModels;
using SanatorioHMS.Domain.Diagnostics.Entities;
using Xunit;

namespace SanatorioHMS.Desktop.Tests;

public sealed class ViewModelCoverageTests
{
    [Fact]
    public async Task LoginAndShellCoverSuccessFailureAndNavigation()
    {
        var login = new LoginViewModel(Client(HttpStatusCode.OK)) { Username = "user", Password = "password" };
        IReadOnlyList<string>? permissions = null;
        login.LoggedIn += value => permissions = value;
        await login.LoginCommand.ExecuteAsync(null);
        Assert.NotNull(permissions);
        Assert.False(login.IsBusy);

        var failed = new LoginViewModel(Client(HttpStatusCode.Unauthorized)) { Username = "user", Password = "bad" };
        await failed.LoginCommand.ExecuteAsync(null);
        Assert.NotEmpty(failed.ErrorMessage);

        var services = new ServiceCollection().AddSingleton(new HmsApiClient(new HttpClient(new ViewModelHandler(HttpStatusCode.OK)) { BaseAddress = new Uri("https://desktop.test/") })).AddTransient<PatientSearchViewModel>().BuildServiceProvider();
        var shell = new ShellViewModel(services);
        shell.Initialize(["Patient.Read", "Scheduling.Read"]);
        Assert.Equal(2, shell.NavigationItems.Count);
        Assert.NotNull(shell.CurrentView);
    }

    [Fact]
    public async Task PatientViewModelsCoverSearchDetailAndEditors()
    {
        var patientId = Guid.NewGuid();
        var vm = new PatientSearchViewModel(Client(HttpStatusCode.OK)) { SearchText = "Ana" };
        await vm.SearchCommand.ExecuteAsync(null);
        Assert.Single(vm.Results);

        var detail = new PatientDetailViewModel(Client(HttpStatusCode.OK));
        await detail.LoadAsync(patientId);
        Assert.NotNull(detail.Patient);
        await detail.SaveCommand.ExecuteAsync(null);
        Assert.Contains("guardados", detail.ErrorMessage);

        var coverage = new CoverageEditorViewModel(Client(HttpStatusCode.OK)) { PatientId = patientId, Payer = "SAP" };
        await coverage.SaveCommand.ExecuteAsync(null);
        Assert.Contains("Cobertura", coverage.ErrorMessage);
        var guardian = new GuardianEditorViewModel(Client(HttpStatusCode.OK)) { PatientId = patientId, Name = "Tutor", Relationship = "Parent", ContactData = "555" };
        await guardian.SaveCommand.ExecuteAsync(null);
        Assert.Contains("Tutor", guardian.ErrorMessage);
    }

    [Fact]
    public async Task SchedulingViewModelsCoverRefreshBookingAndStatus()
    {
        var grid = new AgendaGridViewModel(Client(HttpStatusCode.OK));
        await grid.RefreshCommand.ExecuteAsync(null);
        Assert.Single(grid.Turns);

        var booking = new BookingDialogViewModel(Client(HttpStatusCode.OK)) { AgendaId = Guid.NewGuid(), PatientId = Guid.NewGuid(), RoomId = Guid.NewGuid() };
        await booking.ReserveCommand.ExecuteAsync(null);
        Assert.True(booking.CanReserve);

        var action = new TurnStatusActionsViewModel(Client(HttpStatusCode.OK)) { SelectedTurn = grid.Turns[0] };
        await action.ConfirmCommand.ExecuteAsync(null);
        Assert.Contains("actualizado", action.ErrorMessage);
        action.SelectedTurn = null;
        await action.CancelCommand.ExecuteAsync(null);
    }

    [Fact]
    public async Task ClinicalAndDiagnosticsViewModelsCoverAllCommands()
    {
        var episodeId = Guid.NewGuid();
        var episode = new EpisodeWorkspaceViewModel(Client(HttpStatusCode.OK));
        await episode.LoadAsync(episodeId);
        Assert.Single(episode.Orders);

        var note = new ClinicalNoteEditorViewModel(Client(HttpStatusCode.OK)) { EpisodeId = episodeId, ProfessionalId = Guid.NewGuid(), Content = "Evolution" };
        await note.SaveCommand.ExecuteAsync(null);
        Assert.Contains("Nota", note.ErrorMessage);
        note.CanEdit = false;
        await note.SaveCommand.ExecuteAsync(null);
        Assert.Contains("cerrado", note.ErrorMessage);

        var order = new OrderEntryViewModel(Client(HttpStatusCode.OK)) { EpisodeId = episodeId, EncounterId = Guid.NewGuid(), ProfessionalId = Guid.NewGuid(), Item = "CBC" };
        await order.SaveCommand.ExecuteAsync(null);
        Assert.Contains("Orden", order.ErrorMessage);
        order.CanEdit = false;
        await order.SaveCommand.ExecuteAsync(null);

        var closure = new EpisodeClosureViewModel(Client(HttpStatusCode.OK)) { EpisodeId = episodeId };
        await closure.CloseCommand.ExecuteAsync(null);
        Assert.Contains("requisitos", closure.ErrorMessage);
        closure.RequirementsSatisfied = true;
        await closure.CloseCommand.ExecuteAsync(null);
        Assert.Contains("cerrado", closure.ErrorMessage);

        var diagnostic = new DiagnosticOrderCreationViewModel(Client(HttpStatusCode.OK)) { PatientId = Guid.NewGuid(), EpisodeId = episodeId, ProfessionalId = Guid.NewGuid() };
        await diagnostic.LoadStudyAsync(Guid.NewGuid());
        await diagnostic.CreateCommand.ExecuteAsync(null);
        Assert.Contains("creada", diagnostic.ErrorMessage);

        var lab = new LabTechResultRegistrationViewModel(Client(HttpStatusCode.OK)) { OrderId = Guid.NewGuid(), RecordedBy = Guid.NewGuid(), Value = "10" };
        await lab.RegisterCommand.ExecuteAsync(null);
        Assert.Contains("registrado", lab.ErrorMessage);
        var doctor = new DoctorValidationViewModel(Client(HttpStatusCode.OK)) { ResultId = Guid.NewGuid(), ProfessionalId = Guid.NewGuid() };
        await doctor.ValidateCommand.ExecuteAsync(null);
        Assert.Contains("validado", doctor.ErrorMessage);
    }

    private static HmsApiClient Client(HttpStatusCode status) => new(new HttpClient(new ViewModelHandler(status)) { BaseAddress = new Uri("https://desktop.test/") });

    private sealed class ViewModelHandler(HttpStatusCode status) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (status != HttpStatusCode.OK)
            {
                return Task.FromResult(new HttpResponseMessage(status) { Content = JsonContent.Create(new ProblemDetails(null, "Request failed", (int)status, "Request failed", null)) });
            }

            var path = request.RequestUri?.AbsolutePath ?? string.Empty;
            object body = path.Contains("auth/login") ? new AuthResponse(Guid.NewGuid(), "user", "access", "refresh", DateTime.UtcNow.AddMinutes(5), Guid.NewGuid())
                : path.Contains("patients/search") ? new[] { new PatientResponse(Guid.NewGuid(), "Ana", "Perez", new(1990, 1, 1)) }
                : path.Contains("/notes", StringComparison.Ordinal) ? Guid.NewGuid()
                : path.Contains("episodes/", StringComparison.Ordinal) && path.EndsWith("/orders", StringComparison.Ordinal) && request.Method == HttpMethod.Get ? new[] { new OrderResponse(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "Study", "Activa") }
                : path.Contains("episodes/", StringComparison.Ordinal) && path.EndsWith("/orders", StringComparison.Ordinal) ? new OrderResponse(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "Study", "Activa")
                : path.Contains("episodes/") ? new EpisodeResponse(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "Activo", DateTime.UtcNow)
                : path.Contains("studies") ? new[] { new StudyResponse(Guid.NewGuid(), "L1", "CBC", StudyModality.Laboratory, 1, "Fasting") }
                : path.Contains("turns") ? new[] { new TurnResponse(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), DateTime.UtcNow, "Reservado", "Presencial", Guid.NewGuid(), null) }
                : path.Contains("patients") ? new PatientResponse(Guid.NewGuid(), "Ana", "Perez", new(1990, 1, 1))
                : new { id = Guid.NewGuid() };
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = JsonContent.Create(body) });
        }
    }
}
