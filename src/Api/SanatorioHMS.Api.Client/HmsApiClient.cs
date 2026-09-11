using System.Net.Http.Json;
using SanatorioHMS.Application.Auth;
using SanatorioHMS.Application.PatientRegistry;

namespace SanatorioHMS.Api.Client;

public sealed class HmsApiClient(HttpClient http)
{
    public Task<AuthResponse?> LoginAsync(LoginRequest request, CancellationToken ct = default) => PostAsync<LoginRequest, AuthResponse>("api/v1/auth/login", request, ct);
    public Task<IReadOnlyList<PatientResponse>?> SearchPatientsAsync(string? query = null, CancellationToken ct = default) => http.GetFromJsonAsync<IReadOnlyList<PatientResponse>>($"api/v1/patients/search?q={Uri.EscapeDataString(query ?? string.Empty)}", ct);
    public Task<PatientResponse?> CreatePatientAsync(CreatePatientRequest request, CancellationToken ct = default) => PostAsync<CreatePatientRequest, PatientResponse>("api/v1/patients", request, ct);
    public Task<T?> GetAsync<T>(string path, CancellationToken ct = default) => http.GetFromJsonAsync<T>(path, ct);
    public async Task<HttpResponseMessage> PutAsync<T>(string path, T request, CancellationToken ct = default) => await http.PutAsJsonAsync(path, request, ct);
    private async Task<TResponse?> PostAsync<TRequest, TResponse>(string path, TRequest request, CancellationToken ct)
    {
        using var response = await http.PostAsJsonAsync(path, request, ct);
        return response.IsSuccessStatusCode ? await response.Content.ReadFromJsonAsync<TResponse>(cancellationToken: ct) : default;
    }
}
