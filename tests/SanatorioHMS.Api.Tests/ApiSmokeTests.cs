using Microsoft.AspNetCore.Mvc.Testing;
using System.Net;

namespace SanatorioHMS.Api.Tests;

public sealed class ApiSmokeTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient client;
    public ApiSmokeTests(WebApplicationFactory<Program> factory) => client = factory.CreateClient();

    [Fact]
    public async Task HealthEndpointIsPublic() => Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/health")).StatusCode);

    [Fact]
    public async Task ProtectedEndpointRequiresAuthentication() => Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/v1/episodes")).StatusCode);
}
