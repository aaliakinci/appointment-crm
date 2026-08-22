using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using AppointmentCrm.Application.Tenancy;
using AppointmentCrm.Contracts;
using AppointmentCrm.Domain.Identity;
using AppointmentCrm.Infrastructure;
using AppointmentCrm.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace AppointmentCrm.IntegrationTests;

public sealed class IdentitySecurityTests : IClassFixture<ApiFactory>, IAsyncLifetime
{
    public const string DemoPassword = "Demo-local-2026!";

    private static readonly Guid AtlasTenantId =
        Guid.Parse("10000000-0000-0000-0000-000000000001");
    private static readonly Guid NorthwindTenantId =
        Guid.Parse("10000000-0000-0000-0000-000000000002");
    private static readonly Guid AtlasOwnerMembershipId =
        Guid.Parse("30000000-0000-0000-0000-000000000001");
    private static readonly Guid NorthwindOwnerMembershipId =
        Guid.Parse("30000000-0000-0000-0000-000000000006");
    private static readonly Guid ManagerMembershipId =
        Guid.Parse("30000000-0000-0000-0000-000000000003");
    private static readonly Guid EmployeeUserId =
        Guid.Parse("20000000-0000-0000-0000-000000000004");

    private readonly ApiFactory _factory;

    public IdentitySecurityTests(ApiFactory factory)
    {
        _factory = factory;
    }

    public async Task InitializeAsync()
    {
        await MigrationRunner.RunAsync(_factory.Services);
        await ResetDemoStateAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task MultiTenantLogin_RequiresSelection_ThenIssuesProtectedSession()
    {
        using HttpClient client = CreateClient();
        using var selectionResponse = await client.PostAsJsonAsync(
            "/api/v1/auth/login",
            new LoginRequest("owner@demo.local", DemoPassword, null));

        selectionResponse.EnsureSuccessStatusCode();
        var selection = await selectionResponse.Content.ReadFromJsonAsync<AuthenticationResponse>();
        Assert.NotNull(selection);
        Assert.True(selection.RequiresTenantSelection);
        Assert.Null(selection.AccessToken);
        Assert.Equal(2, selection.Tenants.Count);
        Assert.DoesNotContain(
            selectionResponse.Headers,
            header => string.Equals(header.Key, "Set-Cookie", StringComparison.OrdinalIgnoreCase));

        LoginResult login = await LoginAsync(client, "owner@demo.local", AtlasTenantId);
        Assert.False(login.Payload.RequiresTenantSelection);
        Assert.NotNull(login.Payload.AccessToken);
        Assert.Equal(AtlasTenantId, login.Payload.ActiveTenant?.Id);
        Assert.Contains("HttpOnly", login.SetCookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Secure", login.SetCookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("SameSite=Strict", login.SetCookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Path=/api/v1/auth", login.SetCookie, StringComparison.OrdinalIgnoreCase);

        using var meRequest = Authorized(HttpMethod.Get, "/api/v1/auth/me", login.Payload.AccessToken!);
        using HttpResponseMessage meResponse = await client.SendAsync(meRequest);
        meResponse.EnsureSuccessStatusCode();
        var me = await meResponse.Content.ReadFromJsonAsync<CurrentIdentityResponse>();
        Assert.NotNull(me);
        Assert.Equal(AtlasTenantId, me.ActiveTenant.Id);
        Assert.Equal("Owner", me.ActiveTenant.Role);
    }

    [Fact]
    public async Task Refresh_RotatesOnce_AndReuseRevokesTheSessionFamily()
    {
        using HttpClient client = CreateClient();
        LoginResult login = await LoginAsync(client, "manager@demo.local");
        string firstCookie = CookieHeader(login.SetCookie);

        using var refreshRequest = new HttpRequestMessage(HttpMethod.Post, "/api/v1/auth/refresh");
        refreshRequest.Headers.Add("Cookie", firstCookie);
        using HttpResponseMessage refreshResponse = await client.SendAsync(refreshRequest);
        refreshResponse.EnsureSuccessStatusCode();
        var refreshed = await refreshResponse.Content.ReadFromJsonAsync<AuthenticationResponse>();
        Assert.NotNull(refreshed);
        Assert.NotEqual(login.Payload.AccessToken, refreshed.AccessToken);
        string secondSetCookie = refreshResponse.Headers.GetValues("Set-Cookie").Single();
        Assert.NotEqual(firstCookie, CookieHeader(secondSetCookie));

        using var reuseRequest = new HttpRequestMessage(HttpMethod.Post, "/api/v1/auth/refresh");
        reuseRequest.Headers.Add("Cookie", firstCookie);
        using HttpResponseMessage reuseResponse = await client.SendAsync(reuseRequest);
        Assert.Equal(HttpStatusCode.Unauthorized, reuseResponse.StatusCode);

        using var meRequest = Authorized(HttpMethod.Get, "/api/v1/auth/me", refreshed.AccessToken!);
        using HttpResponseMessage meResponse = await client.SendAsync(meRequest);
        Assert.Equal(HttpStatusCode.Unauthorized, meResponse.StatusCode);
    }

    [Fact]
    public async Task Refresh_RejectsUntrustedOrigin_WithoutConsumingTheToken()
    {
        using HttpClient client = CreateClient();
        LoginResult login = await LoginAsync(client, "manager@demo.local");
        string cookie = CookieHeader(login.SetCookie);

        using var untrustedRequest = new HttpRequestMessage(
            HttpMethod.Post,
            "/api/v1/auth/refresh");
        untrustedRequest.Headers.Add("Cookie", cookie);
        untrustedRequest.Headers.Add("Origin", "https://attacker.invalid");
        using HttpResponseMessage untrustedResponse = await client.SendAsync(untrustedRequest);

        Assert.Equal(HttpStatusCode.Forbidden, untrustedResponse.StatusCode);
        Assert.Equal(
            "application/problem+json",
            untrustedResponse.Content.Headers.ContentType?.MediaType);
        using JsonDocument problem = JsonDocument.Parse(
            await untrustedResponse.Content.ReadAsStringAsync());
        Assert.Equal(
            "Untrusted request origin.",
            problem.RootElement.GetProperty("detail").GetString());
        Assert.Equal(
            "security.untrusted_origin",
            problem.RootElement.GetProperty("code").GetString());
        Assert.True(problem.RootElement.TryGetProperty("traceId", out _));

        using var trustedRequest = new HttpRequestMessage(
            HttpMethod.Post,
            "/api/v1/auth/refresh");
        trustedRequest.Headers.Add("Cookie", cookie);
        trustedRequest.Headers.Add("Origin", "http://localhost:5173");
        using HttpResponseMessage trustedResponse = await client.SendAsync(trustedRequest);

        trustedResponse.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task InvalidLogin_ReturnsStableAuthenticationProblemCode()
    {
        using HttpClient client = CreateClient();
        using HttpResponseMessage response = await client.PostAsJsonAsync(
            "/api/v1/auth/login",
            new LoginRequest("owner@demo.local", "definitely-wrong", AtlasTenantId));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        using JsonDocument problem = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal("auth.invalid_credentials", problem.RootElement.GetProperty("code").GetString());
        Assert.Equal(401, problem.RootElement.GetProperty("status").GetInt32());
        Assert.True(problem.RootElement.TryGetProperty("traceId", out _));
    }

    [Fact]
    public async Task RefreshWithoutCookie_ReturnsInvalidSessionAndExpiresCookie()
    {
        using HttpClient client = CreateClient();
        using HttpResponseMessage response = await client.PostAsync(
            "/api/v1/auth/refresh",
            null);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        using JsonDocument problem = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal("auth.invalid_session", problem.RootElement.GetProperty("code").GetString());
        Assert.Contains(
            response.Headers.GetValues("Set-Cookie"),
            value => value.Contains("expires=", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task ApiControllerAutomaticValidation_ReturnsStableValidationProblemCode()
    {
        using HttpClient client = CreateClient();
        using HttpResponseMessage response = await client.PostAsJsonAsync(
            "/api/v1/auth/login",
            new { });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        using JsonDocument problem = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(
            "common.validation_failed",
            problem.RootElement.GetProperty("code").GetString());
        Assert.True(problem.RootElement.TryGetProperty("errors", out _));
        Assert.True(problem.RootElement.TryGetProperty("traceId", out _));
    }

    [Fact]
    public async Task ConcurrentRefresh_AllowsAtMostOneRotation_AndInvalidatesTheReusedFamily()
    {
        using HttpClient client = CreateClient();
        LoginResult login = await LoginAsync(client, "manager@demo.local");
        string cookie = CookieHeader(login.SetCookie);

        var firstRequest = new HttpRequestMessage(HttpMethod.Post, "/api/v1/auth/refresh");
        firstRequest.Headers.Add("Cookie", cookie);
        var secondRequest = new HttpRequestMessage(HttpMethod.Post, "/api/v1/auth/refresh");
        secondRequest.Headers.Add("Cookie", cookie);

        HttpResponseMessage[] responses = await Task.WhenAll(
            client.SendAsync(firstRequest),
            client.SendAsync(secondRequest));
        using HttpResponseMessage firstResponse = responses[0];
        using HttpResponseMessage secondResponse = responses[1];
        Assert.Contains(responses, response => response.StatusCode == HttpStatusCode.OK);
        Assert.Contains(responses, response => response.StatusCode == HttpStatusCode.Unauthorized);

        HttpResponseMessage successfulResponse = responses.Single(
            response => response.StatusCode == HttpStatusCode.OK);
        var successfulRotation = await successfulResponse.Content
            .ReadFromJsonAsync<AuthenticationResponse>();
        Assert.NotNull(successfulRotation);

        using HttpResponseMessage meResponse = await client.SendAsync(Authorized(
            HttpMethod.Get,
            "/api/v1/auth/me",
            successfulRotation.AccessToken!));
        Assert.Equal(HttpStatusCode.Unauthorized, meResponse.StatusCode);
    }

    [Fact]
    public async Task Logout_RevokesServerSession_AndClearsRefreshCookie()
    {
        using HttpClient client = CreateClient();
        LoginResult login = await LoginAsync(client, "employee@demo.local");

        using var logoutRequest = Authorized(
            HttpMethod.Post,
            "/api/v1/auth/logout",
            login.Payload.AccessToken!);
        logoutRequest.Headers.Add("Cookie", CookieHeader(login.SetCookie));
        using HttpResponseMessage logoutResponse = await client.SendAsync(logoutRequest);
        Assert.Equal(HttpStatusCode.NoContent, logoutResponse.StatusCode);
        Assert.Contains(
            logoutResponse.Headers.GetValues("Set-Cookie"),
            value => value.Contains("expires=", StringComparison.OrdinalIgnoreCase));

        using var meRequest = Authorized(HttpMethod.Get, "/api/v1/auth/me", login.Payload.AccessToken!);
        using HttpResponseMessage meResponse = await client.SendAsync(meRequest);
        Assert.Equal(HttpStatusCode.Unauthorized, meResponse.StatusCode);
    }

    [Fact]
    public async Task RevokeAll_InvalidatesEverySessionForTheUser()
    {
        using HttpClient client = CreateClient();
        LoginResult first = await LoginAsync(client, "manager@demo.local");
        LoginResult second = await LoginAsync(client, "manager@demo.local");

        using HttpResponseMessage revokeResponse = await client.SendAsync(Authorized(
            HttpMethod.Post,
            "/api/v1/auth/revoke-all",
            first.Payload.AccessToken!));
        Assert.Equal(HttpStatusCode.NoContent, revokeResponse.StatusCode);

        using HttpResponseMessage firstMe = await client.SendAsync(Authorized(
            HttpMethod.Get,
            "/api/v1/auth/me",
            first.Payload.AccessToken!));
        using HttpResponseMessage secondMe = await client.SendAsync(Authorized(
            HttpMethod.Get,
            "/api/v1/auth/me",
            second.Payload.AccessToken!));
        Assert.Equal(HttpStatusCode.Unauthorized, firstMe.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, secondMe.StatusCode);

        using var refreshRequest = new HttpRequestMessage(HttpMethod.Post, "/api/v1/auth/refresh");
        refreshRequest.Headers.Add("Cookie", CookieHeader(second.SetCookie));
        using HttpResponseMessage refreshResponse = await client.SendAsync(refreshRequest);
        Assert.Equal(HttpStatusCode.Unauthorized, refreshResponse.StatusCode);
    }

    [Fact]
    public async Task TenantSwitch_RequiresMembership_AndRevokesPreviousSession()
    {
        using HttpClient client = CreateClient();
        LoginResult atlasLogin = await LoginAsync(client, "owner@demo.local", AtlasTenantId);

        using var emptyTenantRequest = Authorized(
            HttpMethod.Post,
            "/api/v1/auth/switch-tenant",
            atlasLogin.Payload.AccessToken!,
            new SwitchTenantRequest(Guid.Empty));
        using HttpResponseMessage emptyTenantResponse = await client.SendAsync(emptyTenantRequest);
        Assert.Equal(HttpStatusCode.BadRequest, emptyTenantResponse.StatusCode);
        await AssertProblemCodeAsync(emptyTenantResponse, "common.validation_failed");

        using var switchRequest = Authorized(
            HttpMethod.Post,
            "/api/v1/auth/switch-tenant",
            atlasLogin.Payload.AccessToken!,
            new SwitchTenantRequest(NorthwindTenantId));
        using HttpResponseMessage switchResponse = await client.SendAsync(switchRequest);
        switchResponse.EnsureSuccessStatusCode();
        var northwind = await switchResponse.Content.ReadFromJsonAsync<AuthenticationResponse>();
        Assert.NotNull(northwind);
        Assert.Equal(NorthwindTenantId, northwind.ActiveTenant?.Id);

        using var oldSessionRequest = Authorized(
            HttpMethod.Get,
            "/api/v1/auth/me",
            atlasLogin.Payload.AccessToken!);
        using HttpResponseMessage oldSessionResponse = await client.SendAsync(oldSessionRequest);
        Assert.Equal(HttpStatusCode.Unauthorized, oldSessionResponse.StatusCode);

        using var invalidSwitchRequest = Authorized(
            HttpMethod.Post,
            "/api/v1/auth/switch-tenant",
            northwind.AccessToken!,
            new SwitchTenantRequest(Guid.NewGuid()));
        using HttpResponseMessage invalidSwitchResponse = await client.SendAsync(invalidSwitchRequest);
        Assert.Equal(HttpStatusCode.NotFound, invalidSwitchResponse.StatusCode);
    }

    [Fact]
    public async Task TenantScopedMembershipEndpoints_DoNotEnumerateAnotherTenant()
    {
        using HttpClient client = CreateClient();
        LoginResult login = await LoginAsync(client, "owner@demo.local", AtlasTenantId);

        using HttpResponseMessage invalidRoleResponse = await client.SendAsync(Authorized(
            HttpMethod.Patch,
            $"/api/v1/memberships/{AtlasOwnerMembershipId}",
            login.Payload.AccessToken!,
            new UpdateMembershipRequest("Unknown", true)));
        Assert.Equal(HttpStatusCode.BadRequest, invalidRoleResponse.StatusCode);
        await AssertProblemCodeAsync(invalidRoleResponse, "common.validation_failed");

        using HttpResponseMessage listResponse = await client.SendAsync(Authorized(
            HttpMethod.Get,
            "/api/v1/memberships",
            login.Payload.AccessToken!));
        listResponse.EnsureSuccessStatusCode();
        var memberships = await listResponse.Content.ReadFromJsonAsync<List<MembershipResponse>>();
        Assert.NotNull(memberships);
        Assert.Equal(4, memberships.Count);
        Assert.DoesNotContain(memberships, membership => membership.Id == NorthwindOwnerMembershipId);

        using HttpResponseMessage getResponse = await client.SendAsync(Authorized(
            HttpMethod.Get,
            $"/api/v1/memberships/{NorthwindOwnerMembershipId}",
            login.Payload.AccessToken!));
        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);

        using HttpResponseMessage patchResponse = await client.SendAsync(Authorized(
            HttpMethod.Patch,
            $"/api/v1/memberships/{NorthwindOwnerMembershipId}",
            login.Payload.AccessToken!,
            new UpdateMembershipRequest(TenantRoles.Manager, true)));
        Assert.Equal(HttpStatusCode.NotFound, patchResponse.StatusCode);

        using HttpResponseMessage deleteResponse = await client.SendAsync(Authorized(
            HttpMethod.Delete,
            $"/api/v1/memberships/{NorthwindOwnerMembershipId}",
            login.Payload.AccessToken!));
        Assert.Equal(HttpStatusCode.NotFound, deleteResponse.StatusCode);

        using HttpResponseMessage reportResponse = await client.SendAsync(Authorized(
            HttpMethod.Get,
            "/api/v1/memberships/report",
            login.Payload.AccessToken!));
        reportResponse.EnsureSuccessStatusCode();
        var report = await reportResponse.Content.ReadFromJsonAsync<MembershipReportResponse>();
        Assert.NotNull(report);
        Assert.Equal(4, report.Total);
    }

    [Fact]
    public async Task LastActiveOwnerRule_ReturnsStableMembershipConflictCode()
    {
        using HttpClient client = CreateClient();
        LoginResult owner = await LoginAsync(client, "owner@demo.local", AtlasTenantId);

        using HttpResponseMessage response = await client.SendAsync(Authorized(
            HttpMethod.Patch,
            $"/api/v1/memberships/{AtlasOwnerMembershipId}",
            owner.Payload.AccessToken!,
            new UpdateMembershipRequest(TenantRoles.Manager, true)));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        using JsonDocument problem = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(
            "memberships.last_active_owner",
            problem.RootElement.GetProperty("code").GetString());
        Assert.True(problem.RootElement.TryGetProperty("traceId", out _));
    }

    [Theory]
    [InlineData("receptionist@demo.local")]
    [InlineData("employee@demo.local")]
    public async Task OperationalRoles_CannotReadOrChangeMemberships(string email)
    {
        using HttpClient client = CreateClient();
        LoginResult login = await LoginAsync(client, email);

        using HttpResponseMessage listResponse = await client.SendAsync(Authorized(
            HttpMethod.Get,
            "/api/v1/memberships",
            login.Payload.AccessToken!));
        Assert.Equal(HttpStatusCode.Forbidden, listResponse.StatusCode);

        using HttpResponseMessage patchResponse = await client.SendAsync(Authorized(
            HttpMethod.Patch,
            $"/api/v1/memberships/{AtlasOwnerMembershipId}",
            login.Payload.AccessToken!,
            new UpdateMembershipRequest(TenantRoles.Manager, true)));
        Assert.Equal(HttpStatusCode.Forbidden, patchResponse.StatusCode);
    }

    [Fact]
    public async Task RoleChange_ImmediatelyInvalidatesAccessAndRefreshTokens()
    {
        using HttpClient client = CreateClient();
        LoginResult manager = await LoginAsync(client, "manager@demo.local");
        LoginResult owner = await LoginAsync(client, "owner@demo.local", AtlasTenantId);

        using HttpResponseMessage updateResponse = await client.SendAsync(Authorized(
            HttpMethod.Patch,
            $"/api/v1/memberships/{ManagerMembershipId}",
            owner.Payload.AccessToken!,
            new UpdateMembershipRequest(TenantRoles.Receptionist, true)));
        updateResponse.EnsureSuccessStatusCode();

        using HttpResponseMessage meResponse = await client.SendAsync(Authorized(
            HttpMethod.Get,
            "/api/v1/auth/me",
            manager.Payload.AccessToken!));
        Assert.Equal(HttpStatusCode.Unauthorized, meResponse.StatusCode);

        using var refreshRequest = new HttpRequestMessage(HttpMethod.Post, "/api/v1/auth/refresh");
        refreshRequest.Headers.Add("Cookie", CookieHeader(manager.SetCookie));
        using HttpResponseMessage refreshResponse = await client.SendAsync(refreshRequest);
        Assert.Equal(HttpStatusCode.Unauthorized, refreshResponse.StatusCode);
    }

    [Fact]
    public async Task DisabledUser_LosesAccessWithoutWaitingForTokenExpiry()
    {
        using HttpClient client = CreateClient();
        LoginResult employee = await LoginAsync(client, "employee@demo.local");

        await using (AsyncServiceScope scope = _factory.Services.CreateAsyncScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<AppointmentCrmDbContext>();
            var user = await dbContext.Users.SingleAsync(candidate => candidate.Id == EmployeeUserId);
            user.SetActive(false, DateTimeOffset.UtcNow);
            await dbContext.SaveChangesAsync();
        }

        using HttpResponseMessage response = await client.SendAsync(Authorized(
            HttpMethod.Get,
            "/api/v1/auth/me",
            employee.Payload.AccessToken!));
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task QueryFilter_WriteGuard_AndCompositeForeignKey_EnforceTenantBoundary()
    {
        var tenantContext = new TestTenantContext(AtlasTenantId);
        var options = new DbContextOptionsBuilder<AppointmentCrmDbContext>()
            .UseNpgsql(_factory.ConnectionString)
            .Options;
        await using var dbContext = new AppointmentCrmDbContext(options, tenantContext);

        List<Guid> visibleTenantIds = await dbContext.TenantMemberships
            .Select(membership => membership.TenantId)
            .Distinct()
            .ToListAsync();
        Assert.Equal([AtlasTenantId], visibleTenantIds);

        dbContext.TenantMemberships.Add(TenantMembership.Create(
            Guid.NewGuid(),
            NorthwindTenantId,
            Guid.Parse("20000000-0000-0000-0000-000000000005"),
            TenantRoles.Manager,
            DateTimeOffset.UtcNow));
        var guardException = await Assert.ThrowsAsync<InvalidOperationException>(
            () => dbContext.SaveChangesAsync());
        Assert.Contains("outside the active tenant", guardException.Message, StringComparison.Ordinal);
        dbContext.ChangeTracker.Clear();

        string tokenHash = new('a', 64);
        var databaseException = await Assert.ThrowsAsync<PostgresException>(() =>
            dbContext.Database.ExecuteSqlInterpolatedAsync($"""
                INSERT INTO user_sessions
                    (id, tenant_id, membership_id, user_id, family_id, token_hash,
                     created_at_utc, expires_at_utc)
                VALUES
                    ({Guid.NewGuid()}, {AtlasTenantId}, {NorthwindOwnerMembershipId},
                     {Guid.Parse("20000000-0000-0000-0000-000000000005")}, {Guid.NewGuid()},
                     {tokenHash}, {DateTimeOffset.UtcNow}, {DateTimeOffset.UtcNow.AddDays(1)})
                """));
        Assert.Equal(PostgresErrorCodes.ForeignKeyViolation, databaseException.SqlState);
    }

    private HttpClient CreateClient() => _factory.CreateClient(
        new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            HandleCookies = false,
        });

    private static HttpRequestMessage Authorized(
        HttpMethod method,
        string uri,
        string accessToken,
        object? body = null)
    {
        var request = new HttpRequestMessage(method, uri);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        if (body is not null)
        {
            request.Content = JsonContent.Create(body);
        }

        return request;
    }

    private static async Task<LoginResult> LoginAsync(
        HttpClient client,
        string email,
        Guid? tenantId = null)
    {
        using HttpResponseMessage response = await client.PostAsJsonAsync(
            "/api/v1/auth/login",
            new LoginRequest(email, DemoPassword, tenantId));
        response.EnsureSuccessStatusCode();
        var payload = await response.Content.ReadFromJsonAsync<AuthenticationResponse>();
        Assert.NotNull(payload);
        string setCookie = response.Headers.GetValues("Set-Cookie").Single();
        return new LoginResult(payload, setCookie);
    }

    private static string CookieHeader(string setCookie) => setCookie.Split(';')[0];

    private static async Task AssertProblemCodeAsync(
        HttpResponseMessage response,
        string expectedCode)
    {
        using JsonDocument problem = JsonDocument.Parse(
            await response.Content.ReadAsStringAsync());
        Assert.Equal(expectedCode, problem.RootElement.GetProperty("code").GetString());
        Assert.True(problem.RootElement.TryGetProperty("traceId", out _));
    }

    private async Task ResetDemoStateAsync()
    {
        await using AsyncServiceScope scope = _factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppointmentCrmDbContext>();
        await dbContext.Database.ExecuteSqlRawAsync("TRUNCATE TABLE user_sessions;");
        await dbContext.Database.ExecuteSqlRawAsync("""
            UPDATE users
            SET is_active = TRUE, security_version = 1;

            UPDATE tenant_memberships
            SET is_active = TRUE,
                authorization_version = 1,
                role = CASE id
                    WHEN '30000000-0000-0000-0000-000000000001'::uuid THEN 'Owner'
                    WHEN '30000000-0000-0000-0000-000000000002'::uuid THEN 'Owner'
                    WHEN '30000000-0000-0000-0000-000000000003'::uuid THEN 'Manager'
                    WHEN '30000000-0000-0000-0000-000000000004'::uuid THEN 'Receptionist'
                    WHEN '30000000-0000-0000-0000-000000000005'::uuid THEN 'Employee'
                    WHEN '30000000-0000-0000-0000-000000000006'::uuid THEN 'Owner'
                    ELSE role
                END;
            """);
    }

    private sealed record LoginResult(AuthenticationResponse Payload, string SetCookie);

    private sealed class TestTenantContext(Guid tenantId) : ITenantContext
    {
        public bool IsAvailable => true;

        public Guid TenantId { get; } = tenantId;
    }
}
