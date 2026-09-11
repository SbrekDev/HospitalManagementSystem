using System.Net.Http.Json;
using SanatorioHMS.Application.Auth;
using SanatorioHMS.Application.PatientRegistry;
using SanatorioHMS.Application.Scheduling;

namespace SanatorioHMS.Api.Client;

public sealed class HmsApiClient(HttpClient http)
{
    public Task<AuthResponse?> LoginAsync(LoginRequest request, CancellationToken ct = default) => PostAsync<LoginRequest, AuthResponse>("api/v1/auth/login", request, ct);
    public Task<IReadOnlyList<PatientResponse>?> SearchPatientsAsync(string? query = null, CancellationToken ct = default) => http.GetFromJsonAsync<IReadOnlyList<PatientResponse>>($"api/v1/patients/search?q={Uri.EscapeDataString(query ?? string.Empty)}", ct);
    public Task<PatientResponse?> CreatePatientAsync(CreatePatientRequest request, CancellationToken ct = default) => PostAsync<CreatePatientRequest, PatientResponse>("api/v1/patients", request, ct);
    public Task<PatientResponse?> AddCoverageAsync(CoverageRequest request, CancellationToken ct = default) => PostAsync<CoverageRequest, PatientResponse>($"api/v1/patients/{request.PatientId}/coverage", request, ct);
    public Task<PatientResponse?> AddGuardianAsync(GuardianRequest request, CancellationToken ct = default) => PostAsync<GuardianRequest, PatientResponse>($"api/v1/patients/{request.PatientId}/guardians", request, ct);
    public Task<IReadOnlyList<TurnResponse>?> GetTurnsAsync(CancellationToken ct = default) => http.GetFromJsonAsync<IReadOnlyList<TurnResponse>>("api/v1/turns", ct);
    public Task<TurnResponse?> ReserveTurnAsync(ReserveTurnRequest request, CancellationToken ct = default) => PostAsync<ReserveTurnRequest, TurnResponse>("api/v1/turns", request, ct);
    public Task<HttpResponseMessage> ChangeTurnStatusAsync(Guid id, ChangeTurnStatusRequest request, CancellationToken ct = default) => PutAsync($"api/v1/turns/{id}/status", request, ct);
    public Task<T?> GetAsync<T>(string path, CancellationToken ct = default) => http.GetFromJsonAsync<T>(path, ct);
    public async Task<HttpResponseMessage> PutAsync<T>(string path, T request, CancellationToken ct = default) => await http.PutAsJsonAsync(path, request, ct);
    private async Task<TResponse?> PostAsync<TRequest, TResponse>(string path, TRequest request, CancellationToken ct)
    {
        using var response = await http.PostAsJsonAsync(path, request, ct);
        if (response.IsSuccessStatusCode) return await response.Content.ReadFromJsonAsync<TResponse>(cancellationToken: ct);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>(cancellationToken: ct);
        throw new ApiProblemException((int)response.StatusCode, problem?.Detail ?? response.ReasonPhrase ?? "La operación no fue aceptada.");
    }
}

public sealed record ProblemDetails(string? Type, string? Title, int? Status, string? Detail, string? Instance);
public sealed class ApiProblemException(int statusCode, string detail) : Exception(detail) { public int StatusCode { get; } = statusCode; }
