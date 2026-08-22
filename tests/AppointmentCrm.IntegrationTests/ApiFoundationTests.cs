using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using AppointmentCrm.Contracts;
using AppointmentCrm.Infrastructure;
using AppointmentCrm.Infrastructure.Persistence;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Routing;
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
        Assert.Contains("/api/v1/customers", document, StringComparison.Ordinal);
        Assert.Contains("/api/v1/services", document, StringComparison.Ordinal);
        Assert.Contains("/api/v1/employees", document, StringComparison.Ordinal);
    }

    [Fact]
    public void VersionedProductEndpoints_AreControllerActions()
    {
        RouteEndpoint[] endpoints = _factory.Services
            .GetServices<EndpointDataSource>()
            .SelectMany(source => source.Endpoints)
            .OfType<RouteEndpoint>()
            .Where(endpoint => endpoint.RoutePattern.RawText?
                .TrimStart('/')
                .StartsWith("api/v1/", StringComparison.OrdinalIgnoreCase) == true)
            .ToArray();

        Assert.NotEmpty(endpoints);
        Assert.All(
            endpoints,
            endpoint => Assert.NotNull(
                endpoint.Metadata.GetMetadata<ControllerActionDescriptor>()));
    }

    [Fact]
    public async Task OpenApi_ExposesTheStablePagingQueryContract()
    {
        using var client = _factory.CreateClient();
        string document = await client.GetStringAsync("/openapi/v1.json");
        using JsonDocument payload = JsonDocument.Parse(document);

        string[] parameterNames = payload.RootElement
            .GetProperty("paths")
            .GetProperty("/api/v1/customers")
            .GetProperty("get")
            .GetProperty("parameters")
            .EnumerateArray()
            .Select(parameter => parameter.GetProperty("name").GetString())
            .OfType<string>()
            .ToArray();

        Assert.Contains("page", parameterNames);
        Assert.Contains("pageSize", parameterNames);
        Assert.Contains("search", parameterNames);
        Assert.Contains("sortBy", parameterNames);
        Assert.Contains("sortDirection", parameterNames);
        Assert.Contains("includeArchived", parameterNames);
        Assert.DoesNotContain("descending", parameterNames);
    }

    [Fact]
    public async Task OpenApi_ExposesTypedSuccessAndProblemResponseContracts()
    {
        using var client = _factory.CreateClient();
        string document = await client.GetStringAsync("/openapi/v1.json");
        using JsonDocument payload = JsonDocument.Parse(document);
        JsonElement paths = payload.RootElement.GetProperty("paths");

        AssertResponseContent(
            paths.GetProperty("/api/v1/customers").GetProperty("get"),
            "200",
            "application/json");
        AssertResponseContent(
            paths.GetProperty("/api/v1/customers").GetProperty("get"),
            "400",
            "application/problem+json");
        AssertResponseContent(
            paths.GetProperty("/api/v1/customers").GetProperty("get"),
            "401",
            "application/problem+json");
        AssertResponseContent(
            paths.GetProperty("/api/v1/customers").GetProperty("get"),
            "403",
            "application/problem+json");
        AssertResponseContent(
            paths.GetProperty("/api/v1/customers").GetProperty("get"),
            "500",
            "application/problem+json");
        AssertResponseContent(
            paths.GetProperty("/api/v1/customers").GetProperty("post"),
            "201",
            "application/json");
        AssertResponseContent(
            paths.GetProperty("/api/v1/customers/{customerId}").GetProperty("get"),
            "404",
            "application/problem+json");
        AssertResponseContent(
            paths.GetProperty("/api/v1/auth/login").GetProperty("post"),
            "200",
            "application/json");

        JsonElement archiveResponses = paths
            .GetProperty("/api/v1/customers/{customerId}")
            .GetProperty("delete")
            .GetProperty("responses");
        Assert.True(archiveResponses.TryGetProperty("204", out JsonElement noContent));
        Assert.False(noContent.TryGetProperty("content", out _));
    }

    [Fact]
    public async Task UnknownEndpoint_ReturnsProblemDetailsWithTraceId()
    {
        using var client = _factory.CreateClient();
        using var response = await client.GetAsync("/api/v1/not-found");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        using var payload = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(404, payload.RootElement.GetProperty("status").GetInt32());
        Assert.Equal("common.not_found", payload.RootElement.GetProperty("code").GetString());
        Assert.Equal("/api/v1/not-found", payload.RootElement.GetProperty("instance").GetString());
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
        Assert.Contains(appliedMigrations, migration => migration.EndsWith("_BusinessMasterData"));
    }

    [Fact]
    public void Testing_UsesEphemeralDataProtectionKeys()
    {
        var provider = _factory.Services.GetRequiredService<IDataProtectionProvider>();

        Assert.IsType<EphemeralDataProtectionProvider>(provider);
    }

    private static void AssertResponseContent(
        JsonElement operation,
        string statusCode,
        string contentType)
    {
        Assert.True(
            operation.GetProperty("responses").TryGetProperty(statusCode, out JsonElement response),
            $"OpenAPI response '{statusCode}' was not found.");
        Assert.True(
            response.GetProperty("content").TryGetProperty(contentType, out JsonElement content),
            $"OpenAPI content type '{contentType}' was not found for response '{statusCode}'.");
        Assert.Equal(JsonValueKind.Object, content.GetProperty("schema").ValueKind);
    }
}
