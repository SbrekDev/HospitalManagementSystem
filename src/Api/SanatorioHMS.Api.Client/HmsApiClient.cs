using System.Net.Http.Json;
using System.Text.Json;
using System.Diagnostics;
using SanatorioHMS.Application.Auth;
using SanatorioHMS.Application.ClinicalCare;
using SanatorioHMS.Application.Diagnostics;
using SanatorioHMS.Application.PatientRegistry;
using SanatorioHMS.Application.Scheduling;

namespace SanatorioHMS.Api.Client;

public sealed class HmsApiClient(HttpClient http)
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public Task<AuthResponse?> LoginAsync(LoginRequest request, CancellationToken ct = default) => PostAsync<LoginRequest, AuthResponse>("api/v1/auth/login", request, ct);
    public Task<IReadOnlyList<PatientResponse>?> SearchPatientsAsync(string? query = null, CancellationToken ct = default) => http.GetFromJsonAsync<IReadOnlyList<PatientResponse>>($"api/v1/patients/search?q={Uri.EscapeDataString(query ?? string.Empty)}", JsonOptions, ct);
    public Task<PatientResponse?> CreatePatientAsync(CreatePatientRequest request, CancellationToken ct = default) => PostAsync<CreatePatientRequest, PatientResponse>("api/v1/patients", request, ct);
    public Task<PatientResponse?> AddCoverageAsync(CoverageRequest request, CancellationToken ct = default) => PostAsync<CoverageRequest, PatientResponse>($"api/v1/patients/{request.PatientId}/coverage", request, ct);
    public Task<PatientResponse?> AddGuardianAsync(GuardianRequest request, CancellationToken ct = default) => PostAsync<GuardianRequest, PatientResponse>($"api/v1/patients/{request.PatientId}/guardians", request, ct);
    public Task<IReadOnlyList<TurnResponse>?> GetTurnsAsync(CancellationToken ct = default) => http.GetFromJsonAsync<IReadOnlyList<TurnResponse>>("api/v1/turns", JsonOptions, ct);
    public Task<TurnResponse?> ReserveTurnAsync(ReserveTurnRequest request, CancellationToken ct = default) => PostAsync<ReserveTurnRequest, TurnResponse>("api/v1/turns", request, ct);
    public Task<HttpResponseMessage> ChangeTurnStatusAsync(Guid id, ChangeTurnStatusRequest request, CancellationToken ct = default) => PutAsync($"api/v1/turns/{id}/status", request, ct);
    public Task<EpisodeResponse?> GetEpisodeAsync(Guid id, CancellationToken ct = default) => GetAsync<EpisodeResponse>($"api/v1/episodes/{id}", ct);
    public Task<IReadOnlyList<OrderResponse>?> GetEpisodeOrdersAsync(Guid id, CancellationToken ct = default) => GetAsync<IReadOnlyList<OrderResponse>>($"api/v1/episodes/{id}/orders", ct);
    public Task<EpisodeResponse?> OpenEpisodeAsync(OpenEpisodeRequest request, CancellationToken ct = default) => PostAsync<OpenEpisodeRequest, EpisodeResponse>("api/v1/episodes", request, ct);
    public async Task<Guid?> AppendClinicalNoteAsync(Guid episodeId, AppendClinicalNoteRequest request, CancellationToken ct = default) => await PostAsync<AppendClinicalNoteRequest, Guid>($"api/v1/episodes/{episodeId}/notes", request, ct);
    public Task<OrderResponse?> IssueOrderAsync(Guid episodeId, IssueOrderRequest request, CancellationToken ct = default) => PostAsync<IssueOrderRequest, OrderResponse>($"api/v1/episodes/{episodeId}/orders", request, ct);
    public Task<HttpResponseMessage> CloseEpisodeAsync(Guid id, CancellationToken ct = default) => PutAsync($"api/v1/episodes/{id}/close", new { }, ct);
    public Task<IReadOnlyList<StudyResponse>?> GetStudiesAsync(string? search = null, CancellationToken ct = default) => GetAsync<IReadOnlyList<StudyResponse>>($"api/v1/studies?q={Uri.EscapeDataString(search ?? string.Empty)}", ct);
    public Task<DiagnosticOrderResponse?> CreateDiagnosticOrderAsync(CreateDiagnosticOrderRequest request, CancellationToken ct = default) => PostAsync<CreateDiagnosticOrderRequest, DiagnosticOrderResponse>("api/v1/diagnostic-orders", request, ct);
    public Task<DiagnosticResultResponse?> RegisterDiagnosticResultAsync(RegisterDiagnosticResultRequest request, CancellationToken ct = default) => PostAsync<RegisterDiagnosticResultRequest, DiagnosticResultResponse>("api/v1/results", request, ct);
    public Task<HttpResponseMessage> ValidateDiagnosticResultAsync(Guid id, ValidateDiagnosticResultRequest request, CancellationToken ct = default) => PutAsync($"api/v1/results/{id}/validate", request, ct);
    public Task<T?> GetAsync<T>(string path, CancellationToken ct = default) => http.GetFromJsonAsync<T>(path, JsonOptions, ct);
    public async Task<HttpResponseMessage> PutAsync<T>(string path, T request, CancellationToken ct = default) => await http.PutAsJsonAsync(path, request, JsonOptions, ct);
    private async Task<TResponse?> PostAsync<TRequest, TResponse>(string path, TRequest request, CancellationToken ct)
    {
        using var response = await http.PostAsJsonAsync(path, request, JsonOptions, ct);
        Debug.WriteLine($"HMS API POST {http.BaseAddress}{path} -> {(int)response.StatusCode} {response.ReasonPhrase}");
        if (response.IsSuccessStatusCode) return await response.Content.ReadFromJsonAsync<TResponse>(JsonOptions, ct);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>(JsonOptions, ct);
        throw new ApiProblemException((int)response.StatusCode, problem?.Detail ?? response.ReasonPhrase ?? "La operación no fue aceptada.");
    }
}

public sealed record ProblemDetails(string? Type, string? Title, int? Status, string? Detail, string? Instance);
public sealed class ApiProblemException(int statusCode, string detail) : Exception(detail) { public int StatusCode { get; } = statusCode; }
