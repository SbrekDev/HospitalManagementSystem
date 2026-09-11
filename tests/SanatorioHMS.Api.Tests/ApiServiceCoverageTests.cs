using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using SanatorioHMS.Api;
using SanatorioHMS.Application.Auth;
using SanatorioHMS.Domain.Auth.Entities;
using SanatorioHMS.Domain.ClinicalCare.Entities;
using SanatorioHMS.Domain.Diagnostics.Entities;
using SanatorioHMS.Domain.PatientRegistry.Entities;
using SanatorioHMS.Domain.Scheduling.Entities;

namespace SanatorioHMS.Api.Tests;

public sealed class ApiServiceCoverageTests
{
    [Fact]
    public async Task InMemoryStoreCoversRepositoriesAndSearches()
    {
        var store = new InMemoryStore();
        var patient = Patient.Create("Ana", "Perez", new(1990, 1, 1));
        patient.Documents.Add(new PatientDocument { DocumentType = DocumentType.DNI, DocumentNumber = "123" });
        patient.Contacts.Add(new PatientContact { Value = "555" });
        await store.AddAsync(patient);
        Assert.True(await store.HasDocumentAsync(DocumentType.DNI, " 123 "));
        Assert.Single(await store.SearchAsync("555", null));
        ((SanatorioHMS.Domain.Core.IRepository<Patient>)store).Update(patient);
        ((SanatorioHMS.Domain.Core.IRepository<Patient>)store).Remove(patient);
        Assert.Empty(await store.SearchAsync(null, null));

        var agenda = new Agenda { Id = Guid.NewGuid(), Fecha = DateOnly.FromDateTime(DateTime.UtcNow), HoraInicio = new(8, 0), HoraFin = new(18, 0), Activo = true };
        await store.AddAsync(agenda);
        Assert.Empty(await store.GetAvailableSlotsAsync(agenda.Id));
        var turn = new Turno { Id = Guid.NewGuid(), AgendaId = agenda.Id, FechaHora = DateTime.UtcNow, Estado = "Reservado" };
        Assert.Null(await store.FindSlotAsync(agenda.Id, turn.FechaHora));
        await store.AddAsync(turn);
        Assert.NotNull(await store.FindSlotAsync(agenda.Id, turn.FechaHora));
        store.Remove(turn);

        var episode = new Episode { Id = Guid.NewGuid() };
        await ((SanatorioHMS.Domain.Core.IRepository<Episode>)store).AddAsync(episode);
        Assert.Empty(await store.GetEncountersAsync(episode.Id));
        Assert.Empty(await store.GetOrdersAsync(episode.Id));
        Assert.False(await store.HasIncompleteRequiredOrdersAsync(episode.Id));
        ((SanatorioHMS.Domain.Core.IRepository<Episode>)store).Remove(episode);

        var order = DiagnosticOrder.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Study.Create("L1", "CBC", StudyModality.Laboratory));
        await ((SanatorioHMS.Domain.Core.IRepository<DiagnosticOrder>)store).AddAsync(order);
        Assert.Single(await store.GetPendingOrdersAsync(order.PatientId));
        var result = DiagnosticResult.Register(order.Id, "10", Guid.NewGuid());
        await store.AddResultAsync(result);
        Assert.NotNull(await store.GetResultAsync(result.Id));
        store.UpdateResult(result);
        ((SanatorioHMS.Domain.Core.IRepository<DiagnosticOrder>)store).Remove(order);

        var user = new User(Guid.NewGuid(), "operator");
        await ((SanatorioHMS.Domain.Core.IRepository<User>)store).AddAsync(user);
        Assert.NotNull(await store.FindByUsernameAsync("OPERATOR"));
        Assert.Single(await store.GetPermissionsAsync(user.Id));
        store.UpdateUser(user);
        ((SanatorioHMS.Domain.Core.IRepository<User>)store).Remove(user);
    }

    [Fact]
    public async Task ApiAuthenticationServicesCoverTokenAndCredentialBranches()
    {
        var options = Options.Create(new ApiJwtOptions());
        var user = new User(Guid.NewGuid(), "admin");
        user.SetPasswordHash(InMemoryStore.Hash("secret"));
        var credentials = new ApiCredentialService();
        Assert.True(credentials.Verify(user, "secret"));
        Assert.False(credentials.Verify(user, "wrong"));
        Assert.False(await credentials.ResetAsync(user, "token", "new"));
        var token = new ApiTokenService(options).Issue(user);
        Assert.NotEmpty(token.AccessToken);
        Assert.Null(await new ApiTokenService(options).RotateAsync(token.SessionId, token.RefreshToken));
        await new ApiTokenService(options).RevokeAsync(token.SessionId);

        var context = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity("test")) };
        var accessor = new HttpContextAccessor { HttpContext = context };
        Assert.True(await new RequestAuthorization(accessor).AuthorizeAsync("anything"));
        context.User = new ClaimsPrincipal(new ClaimsIdentity());
        Assert.False(await new RequestAuthorization(accessor).AuthorizeAsync("anything"));
        accessor.HttpContext = null;
        Assert.False(await new RequestAuthorization(accessor).AuthorizeAsync("anything"));
    }
}
