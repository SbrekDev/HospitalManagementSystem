using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SanatorioHMS.Api;
using SanatorioHMS.Application.Auth;
using SanatorioHMS.Domain.Scheduling.Entities;
using SanatorioHMS.Infrastructure.Data;

namespace SanatorioHMS.Api.Tests;

public sealed class ApiMatrixTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> factory;

    public ApiMatrixTests(WebApplicationFactory<Program> factory) => this.factory = factory;

    [Fact]
    public async Task AuthAndValidationMatrixReturnsExpectedStatusCodes()
    {
        using var client = factory.CreateClient();
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/v1/episodes")).StatusCode);

        var login = await client.PostAsJsonAsync("/api/v1/auth/login", new LoginRequest("admin", "Admin123!"));
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        var auth = await login.Content.ReadFromJsonAsync<AuthResponse>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth!.AccessToken);

        using var invalidContent = new StringContent("{", System.Text.Encoding.UTF8, "application/json");
        var invalid = await client.PostAsync("/api/v1/patients", invalidContent);
        Assert.Equal(HttpStatusCode.BadRequest, invalid.StatusCode);
        var missing = await client.GetAsync($"/api/v1/episodes/{Guid.NewGuid()}");
        Assert.Equal(HttpStatusCode.NotFound, missing.StatusCode);
        var refresh = await client.PostAsJsonAsync("/api/v1/auth/refresh", new { sessionId = Guid.NewGuid(), refreshToken = "invalid" });
        Assert.Equal(HttpStatusCode.Unauthorized, refresh.StatusCode);
    }

    [Fact]
    public async Task ConcurrentBookingReturnsOneCreatedAndRemainingConflicts()
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SchedulingDbContext>();
        var professionalId = await db.Set<Professional>().Select(x => x.Id).FirstAsync();
        var specialtyId = await db.Set<Specialty>().Select(x => x.Id).FirstAsync();
        var agenda = new Agenda
        {
            Id = Guid.NewGuid(),
            ProfesionalId = professionalId,
            EspecialidadId = specialtyId,
            Fecha = DateOnly.FromDateTime(DateTime.UtcNow),
            HoraInicio = new(8, 0),
            HoraFin = new(18, 0),
            DuracionTurnoMinutos = 30,
            Activo = true
        };
        db.Set<Agenda>().Add(agenda);
        await db.SaveChangesAsync();

        using var loginClient = factory.CreateClient();
        var login = await loginClient.PostAsJsonAsync("/api/v1/auth/login", new LoginRequest("admin", "Admin123!"));
        var auth = await login.Content.ReadFromJsonAsync<AuthResponse>();
        var requests = Enumerable.Range(0, 4).Select(_ =>
        {
            var client = factory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth!.AccessToken);
            return client.PostAsJsonAsync("/api/v1/turns", new
            {
                agendaId = agenda.Id,
                patientId = Guid.NewGuid(),
                start = DateTime.UtcNow.Date.AddHours(9),
                modality = "Presencial",
                roomId = Guid.NewGuid(),
                teleconsultationDestination = (string?)null,
                reason = "Control"
            });
        }).ToArray();

        var responses = await Task.WhenAll(requests);
        Assert.Equal(1, responses.Count(x => x.StatusCode == HttpStatusCode.Created));
        Assert.Equal(3, responses.Count(x => x.StatusCode == HttpStatusCode.Conflict));
    }

    [Theory]
    [InlineData("admin", "Admin123!")]
    public async Task AuthenticatedRolesCanReadProtectedResource(string username, string password)
    {
        using var client = factory.CreateClient();
        var login = await client.PostAsJsonAsync("/api/v1/auth/login", new LoginRequest(username, password));
        var auth = await login.Content.ReadFromJsonAsync<AuthResponse>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth!.AccessToken);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/v1/episodes")).StatusCode);
    }
}
