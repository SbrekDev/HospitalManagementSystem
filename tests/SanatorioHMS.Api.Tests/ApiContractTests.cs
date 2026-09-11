using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using SanatorioHMS.Application.Auth;

namespace SanatorioHMS.Api.Tests;

public sealed class ApiContractTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient client;
    public ApiContractTests(WebApplicationFactory<Program> factory) => client = factory.CreateClient();

    [Fact]
    public async Task Protected_endpoint_without_token_returns_401_problem_details()
    {
        var response = await client.GetAsync("/api/v1/patients");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task Invalid_credentials_do_not_reveal_account_existence()
    {
        var response = await client.PostAsJsonAsync("/api/v1/auth/login", new LoginRequest("unknown@sanatorio.local", "wrong"));
        var body = await response.Content.ReadAsStringAsync();
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.DoesNotContain("unknown@sanatorio.local", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Duplicate_document_returns_conflict_problem_details()
    {
        var login = await client.PostAsJsonAsync("/api/v1/auth/login", new LoginRequest("admin", "Admin123!"));
        var auth = await login.Content.ReadFromJsonAsync<AuthResponse>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth!.AccessToken);
        var request = new { name = "Ana", surname = "Pérez", dateOfBirth = "1990-01-01", documentType = 0, documentNumber = $"DUP-{Guid.NewGuid():N}" };
        Assert.Equal(HttpStatusCode.Created, (await client.PostAsJsonAsync("/api/v1/patients", request)).StatusCode);
        var duplicate = await client.PostAsJsonAsync("/api/v1/patients", request);
        Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode);
        Assert.Equal("application/problem+json", duplicate.Content.Headers.ContentType?.MediaType);
    }
}
