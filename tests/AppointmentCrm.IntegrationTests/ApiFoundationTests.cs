using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using AppointmentCrm.Contracts;
using AppointmentCrm.Infrastructure;
using AppointmentCrm.Infrastructure.Persistence;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace AppointmentCrm.IntegrationTests;

public sealed class ApiFoundationTests : IClassFixture<ApiFactory>, IAsyncLifetime
{
    private readonly ApiFactory _factory;

    public ApiFoundationTests(ApiFactory factory)
    {
        _factory = factory;
    }

    public async Task InitializeAsync()
    {
        await MigrationRunner.RunAsync(_factory.Services);
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task SystemStatus_ReturnsVersionedContractAndCorrelationId()
    {
        using var client = _factory.CreateClient();
        using var response = await client.GetAsync("/api/v1/system/status");

        response.EnsureSuccessStatusCode();
        var payload = await response.Content.ReadFromJsonAsync<SystemStatusResponse>();

        Assert.NotNull(payload);
        Assert.Equal("appointment-crm-api", payload.Service);
        Assert.Equal("ready", payload.Status);
        Assert.True(Guid.TryParseExact(payload.TraceId, "N", out _));
        Assert.Equal(payload.TraceId, response.Headers.GetValues("X-Correlation-ID").Single());
    }

    [Fact]
    public async Task Readiness_UsesRealPostgreSqlConnection()
    {
        using var client = _factory.CreateClient();
        using var response = await client.GetAsync("/health/ready");

        response.EnsureSuccessStatusCode();
        using var payload = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

        Assert.Equal("Healthy", payload.RootElement.GetProperty("status").GetString());
        Assert.Equal(
            "Healthy",
            payload.RootElement
                .GetProperty("checks")
                .GetProperty("postgresql")
                .GetProperty("status")
                .GetString());
    }

    [Fact]
    public async Task OpenApi_ContainsTheSystemEndpoint()
    {
        using var client = _factory.CreateClient();
        var document = await client.GetStringAsync("/openapi/v1.json");

        Assert.Contains("/api/v1/system/status", document, StringComparison.Ordinal);
        Assert.Contains("/api/v1/auth/login", document, StringComparison.Ordinal);
        Assert.Contains("/api/v1/auth/refresh", document, StringComparison.Ordinal);
        Assert.Contains("/api/v1/memberships", document, StringComparison.Ordinal);
    }

    [Fact]
    public async Task UnknownEndpoint_ReturnsProblemDetailsWithTraceId()
    {
        using var client = _factory.CreateClient();
        using var response = await client.GetAsync("/api/v1/not-found");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        using var payload = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.True(payload.RootElement.TryGetProperty("traceId", out _));
    }

    [Fact]
    public async Task InitialMigration_IsAppliedToPostgreSql()
    {
        var options = new DbContextOptionsBuilder<AppointmentCrmDbContext>()
            .UseNpgsql(_factory.ConnectionString)
            .Options;
        await using var dbContext = new AppointmentCrmDbContext(options);

        var appliedMigrations = await dbContext.Database.GetAppliedMigrationsAsync();

        Assert.Contains(appliedMigrations, migration => migration.EndsWith("_InitialCreate"));
    }

    [Fact]
    public void Testing_UsesEphemeralDataProtectionKeys()
    {
        var provider = _factory.Services.GetRequiredService<IDataProtectionProvider>();

        Assert.IsType<EphemeralDataProtectionProvider>(provider);
    }
}
