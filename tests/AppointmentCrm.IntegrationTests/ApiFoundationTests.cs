using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using AppointmentCrm.Contracts;
using AppointmentCrm.Infrastructure;
using AppointmentCrm.Infrastructure.Persistence;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
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
        Assert.Equal(
            "Healthy",
            payload.RootElement
                .GetProperty("checks")
                .GetProperty("tenant-time-zones")
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
    public async Task ApiResponses_IncludeSecurityHeaders()
    {
        using var client = _factory.CreateClient();
        using HttpResponseMessage response = await client.GetAsync("/api/v1/system/status");

        response.EnsureSuccessStatusCode();
        Assert.Equal("nosniff", response.Headers.GetValues("X-Content-Type-Options").Single());
        Assert.Equal("DENY", response.Headers.GetValues("X-Frame-Options").Single());
        Assert.Equal("no-referrer", response.Headers.GetValues("Referrer-Policy").Single());
        Assert.Contains(
            "frame-ancestors 'none'",
            response.Headers.GetValues("Content-Security-Policy").Single(),
            StringComparison.Ordinal);
        Assert.Contains(
            "camera=()",
            response.Headers.GetValues("Permissions-Policy").Single(),
            StringComparison.Ordinal);
    }

    [Fact]
    public async Task CorsPreflight_AllowsOnlyConfiguredOrigin()
    {
        using var client = _factory.CreateClient();
        using var trusted = new HttpRequestMessage(HttpMethod.Options, "/api/v1/system/status");
        trusted.Headers.Add("Origin", "http://localhost:5173");
        trusted.Headers.Add("Access-Control-Request-Method", "GET");
        using HttpResponseMessage trustedResponse = await client.SendAsync(trusted);

        Assert.Equal(HttpStatusCode.NoContent, trustedResponse.StatusCode);
        Assert.Equal(
            "http://localhost:5173",
            trustedResponse.Headers.GetValues("Access-Control-Allow-Origin").Single());

        using var untrusted = new HttpRequestMessage(HttpMethod.Options, "/api/v1/system/status");
        untrusted.Headers.Add("Origin", "https://attacker.example");
        untrusted.Headers.Add("Access-Control-Request-Method", "GET");
        using HttpResponseMessage untrustedResponse = await client.SendAsync(untrusted);
        Assert.False(untrustedResponse.Headers.Contains("Access-Control-Allow-Origin"));
    }

    [Fact]
    public async Task OversizedRequest_ReturnsProblemDetailsWithoutReadingTheBody()
    {
        using var client = _factory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/auth/login")
        {
            Content = new StringContent(new string('x', 1_048_577)),
        };
        using HttpResponseMessage response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.RequestEntityTooLarge, response.StatusCode);
        using JsonDocument problem = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(
            "common.payload_too_large",
            problem.RootElement.GetProperty("code").GetString());
        Assert.True(problem.RootElement.TryGetProperty("traceId", out _));
    }

    [Fact]
    public async Task UnsafeRequests_AreCoveredByTheGlobalWriteRateLimit()
    {
        using WebApplicationFactory<Program> factory = _factory.WithWebHostBuilder(builder =>
            builder.ConfigureAppConfiguration((_, configuration) =>
                configuration.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Security:WritePermitLimit"] = "2",
                    ["Security:WriteWindowSeconds"] = "3600",
                })));
        using HttpClient client = factory.CreateClient();
        HttpStatusCode[] statuses = new HttpStatusCode[3];
        for (int index = 0; index < statuses.Length; index++)
        {
            using HttpResponseMessage response = await client.PostAsJsonAsync(
                "/api/v1/customers",
                new CustomerCreateRequestProbe());
            statuses[index] = response.StatusCode;
        }

        Assert.Equal(HttpStatusCode.Unauthorized, statuses[0]);
        Assert.Equal(HttpStatusCode.Unauthorized, statuses[1]);
        Assert.Equal(HttpStatusCode.TooManyRequests, statuses[2]);
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

    private sealed record CustomerCreateRequestProbe;
}
