using System.Net;
using System.Net.Http.Json;
using AppointmentCrm.Contracts;
using AppointmentCrm.Infrastructure;

namespace AppointmentCrm.IntegrationTests;

public sealed class LoginRateLimitTests : IClassFixture<ApiFactory>, IAsyncLifetime
{
    private readonly ApiFactory _factory;

    public LoginRateLimitTests(ApiFactory factory)
    {
        _factory = factory;
    }

    public Task InitializeAsync() => MigrationRunner.RunAsync(_factory.Services);

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task LoginEndpoint_RejectsRequestsBeyondTheFixedWindowLimit()
    {
        using HttpClient client = _factory.CreateClient();
        HttpStatusCode lastStatus = HttpStatusCode.OK;
        for (int attempt = 0; attempt < 31; attempt++)
        {
            using HttpResponseMessage response = await client.PostAsJsonAsync(
                "/api/v1/auth/login",
                new LoginRequest("missing@demo.local", "invalid-password", null));
            lastStatus = response.StatusCode;
        }

        Assert.Equal(HttpStatusCode.TooManyRequests, lastStatus);
    }
}
