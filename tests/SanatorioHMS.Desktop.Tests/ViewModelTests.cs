using System.Net;
using System.Net.Http.Json;
using SanatorioHMS.Api.Client;
using SanatorioHMS.Application.PatientRegistry;
using SanatorioHMS.Application.Scheduling;
using SanatorioHMS.Desktop.ViewModels;
using SanatorioHMS.Application.ClinicalCare;
using SanatorioHMS.Domain.Diagnostics.Entities;
using Xunit;

namespace SanatorioHMS.Desktop.Tests;

public sealed class ViewModelTests
{
    [Fact]
    public void NavigationItemsReflectGrantedPermissions()
    {
        var items = NavigationService.BuildMenu(["Patient.Read", "Scheduling.Read"]);
        Assert.Collection(items, item => Assert.Equal("Pacientes", item.Title), item => Assert.Equal("Agenda", item.Title));
    }

    [Fact]
    public async Task DuplicateDocumentShowsProblemDetail()
    {
        using var http = new HttpClient(new StubHandler(HttpStatusCode.Conflict, "El documento ya existe.")) { BaseAddress = new Uri("https://test/") };
        var vm = new PatientRegistrationViewModel(new HmsApiClient(http)) { Name = "Ana", Surname = "Pérez", DocumentNumber = "123" };
        await vm.RegisterCommand.ExecuteAsync(null);
        Assert.Equal("El documento ya existe.", vm.ErrorMessage);
    }

    [Fact]
    public async Task FullSlotDisablesReserveAndPromptsReschedule()
    {
        using var http = new HttpClient(new StubHandler(HttpStatusCode.Conflict, "Slot conflict.")) { BaseAddress = new Uri("https://test/") };
        var vm = new BookingDialogViewModel(new HmsApiClient(http)) { AgendaId = Guid.NewGuid(), PatientId = Guid.NewGuid() };
        await vm.ReserveCommand.ExecuteAsync(null);
        Assert.False(vm.CanReserve);
        Assert.Contains("ocupado", vm.ErrorMessage);
    }

    [Fact]
    public void ClosedEpisodeDisablesClinicalEditing()
    {
        using var http = new HttpClient(new StubHandler(HttpStatusCode.OK, "{}")) { BaseAddress = new Uri("https://test/") };
        var vm = new EpisodeWorkspaceViewModel(new HmsApiClient(http)) { Episode = new EpisodeResponse(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "Cerrado", DateTime.UtcNow) };
        Assert.False(vm.CanEdit);
    }

    [Theory]
    [InlineData(AuthorizationStatus.Pending)]
    [InlineData(AuthorizationStatus.Denied)]
    public void PendingOrDeniedAuthorizationDisablesFulfillment(AuthorizationStatus authorization)
    {
        using var http = new HttpClient(new StubHandler(HttpStatusCode.OK, "{}")) { BaseAddress = new Uri("https://test/") };
        var vm = new LabTechResultRegistrationViewModel(new HmsApiClient(http)) { Authorization = authorization };
        Assert.False(vm.CanFulfill);
        Assert.Contains(authorization.ToString(), vm.FulfillmentReason);
    }

    private sealed class StubHandler(HttpStatusCode status, string detail) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) => Task.FromResult(new HttpResponseMessage(status) { Content = JsonContent.Create(new ProblemDetails(null, "Conflict", (int)status, detail, null)) });
    }
}
