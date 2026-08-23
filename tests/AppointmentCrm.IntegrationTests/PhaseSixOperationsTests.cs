using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using AppointmentCrm.Contracts;
using AppointmentCrm.Infrastructure;
using AppointmentCrm.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace AppointmentCrm.IntegrationTests;

public sealed class PhaseSixOperationsTests : IClassFixture<ApiFactory>, IAsyncLifetime
{
    private static readonly Guid AtlasTenantId =
        Guid.Parse("10000000-0000-0000-0000-000000000001");
    private static readonly Guid NorthwindTenantId =
        Guid.Parse("10000000-0000-0000-0000-000000000002");
    private static readonly Guid OwnerUserId =
        Guid.Parse("20000000-0000-0000-0000-000000000001");
    private static readonly Guid ReceptionistMembershipId =
        Guid.Parse("30000000-0000-0000-0000-000000000004");
    private static readonly Guid CustomerId =
        Guid.Parse("40000000-0000-0000-0000-000000000001");
    private static readonly Guid EmployeeId =
        Guid.Parse("60000000-0000-0000-0000-000000000001");
    private static readonly Guid ServiceId =
        Guid.Parse("50000000-0000-0000-0000-000000000001");

    private readonly ApiFactory _factory;

    public PhaseSixOperationsTests(ApiFactory factory)
    {
        _factory = factory;
    }

    public async Task InitializeAsync()
    {
        await MigrationRunner.RunAsync(_factory.Services);
        await using AsyncServiceScope scope = _factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppointmentCrmDbContext>();
        await dbContext.Database.ExecuteSqlRawAsync("""
            TRUNCATE TABLE
                appointment_status_history,
                appointments,
                notification_deliveries,
                outbox_messages,
                audit_entries,
                user_sessions;

            UPDATE users
            SET display_name = 'Demo Owner',
                updated_at_utc = created_at_utc
            WHERE id = '20000000-0000-0000-0000-000000000001'::uuid;

            UPDATE tenant_memberships
            SET role = 'Receptionist',
                is_active = true,
                authorization_version = 1,
                updated_at_utc = created_at_utc
            WHERE id = '30000000-0000-0000-0000-000000000004'::uuid;
            """);
    }

    public async Task DisposeAsync()
    {
        await using var connection = new NpgsqlConnection(_factory.ConnectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(
            """
            UPDATE users
            SET display_name = 'Demo Owner'
            WHERE id = '20000000-0000-0000-0000-000000000001'::uuid;

            UPDATE tenant_memberships
            SET role = 'Receptionist', is_active = true
            WHERE id = '30000000-0000-0000-0000-000000000004'::uuid;
            """,
            connection);
        await command.ExecuteNonQueryAsync();
    }

    [Fact]
    public async Task DashboardAndCustomerHistory_MatchTenantScopedDirectSql()
    {
        DateOnly today = TenantToday();
        DateOnly yesterday = today.AddDays(-1);
        await InsertAppointmentAsync(Guid.NewGuid(), yesterday, 10, "Completed", 750m);
        await InsertAppointmentAsync(Guid.NewGuid(), yesterday, 11, "Cancelled", 900m);
        await InsertAppointmentAsync(Guid.NewGuid(), today, 10, "Scheduled", 1_200m);

        using HttpClient client = CreateClient();
        string manager = await LoginAsync(client, "manager@demo.local", AtlasTenantId);
        using HttpResponseMessage dashboardResponse = await client.SendAsync(Authorized(
            HttpMethod.Get,
            $"/api/v1/reporting/dashboard?fromDate={yesterday:yyyy-MM-dd}&toDate={today:yyyy-MM-dd}",
            manager));
        dashboardResponse.EnsureSuccessStatusCode();
        ReportingDashboardResponse dashboard =
            await ReadRequiredAsync<ReportingDashboardResponse>(dashboardResponse);

        (long total, long completed, decimal revenue) = await ReadDirectReportAsync(
            yesterday,
            today);
        Assert.Equal(total, dashboard.Range.TotalAppointments);
        Assert.Equal(completed, dashboard.Range.CompletedAppointments);
        Assert.Equal(revenue, dashboard.Range.CompletedRevenue);
        Assert.Equal(1, dashboard.TodaySummary.TotalAppointments);
        Assert.Equal(3, dashboard.ByStatus.Sum(item => item.Count));

        using HttpResponseMessage employeeDashboardResponse = await client.SendAsync(Authorized(
            HttpMethod.Get,
            $"/api/v1/reporting/dashboard?fromDate={yesterday:yyyy-MM-dd}&toDate={today:yyyy-MM-dd}&employeeId={EmployeeId}",
            manager));
        employeeDashboardResponse.EnsureSuccessStatusCode();
        ReportingDashboardResponse employeeDashboard =
            await ReadRequiredAsync<ReportingDashboardResponse>(employeeDashboardResponse);
        Assert.Equal(dashboard.Range, employeeDashboard.Range);

        using HttpResponseMessage historyResponse = await client.SendAsync(Authorized(
            HttpMethod.Get,
            $"/api/v1/customers/{CustomerId}/appointments?page=1&pageSize=20",
            manager));
        historyResponse.EnsureSuccessStatusCode();
        PagedResponse<AppointmentSummaryResponse> history =
            await ReadRequiredAsync<PagedResponse<AppointmentSummaryResponse>>(historyResponse);
        Assert.Equal(3, history.TotalCount);
        Assert.All(history.Items, item => Assert.Equal(CustomerId, item.CustomerId));

        string northwindOwner = await LoginAsync(client, "north.owner@demo.local", NorthwindTenantId);
        using HttpResponseMessage foreignDashboard = await client.SendAsync(Authorized(
            HttpMethod.Get,
            $"/api/v1/reporting/dashboard?fromDate={yesterday:yyyy-MM-dd}&toDate={today:yyyy-MM-dd}",
            northwindOwner));
        foreignDashboard.EnsureSuccessStatusCode();
        ReportingDashboardResponse northwind =
            await ReadRequiredAsync<ReportingDashboardResponse>(foreignDashboard);
        Assert.Equal(0, northwind.Range.TotalAppointments);

        string receptionist = await LoginAsync(client, "receptionist@demo.local", AtlasTenantId);
        using HttpResponseMessage forbidden = await client.SendAsync(Authorized(
            HttpMethod.Get,
            $"/api/v1/reporting/dashboard?fromDate={yesterday:yyyy-MM-dd}&toDate={today:yyyy-MM-dd}",
            receptionist));
        Assert.Equal(HttpStatusCode.Forbidden, forbidden.StatusCode);
    }

    [Fact]
    public async Task ProfileSessionsMembershipAndAudit_FormOneOperationalFlow()
    {
        using HttpClient client = CreateClient();
        string firstToken = await LoginAsync(client, "owner@demo.local", AtlasTenantId);
        _ = await LoginAsync(client, "owner@demo.local", AtlasTenantId);

        using HttpResponseMessage sessionsBeforeResponse = await client.SendAsync(Authorized(
            HttpMethod.Get,
            "/api/v1/account/sessions",
            firstToken));
        sessionsBeforeResponse.EnsureSuccessStatusCode();
        IReadOnlyList<AccountSessionResponse> sessionsBefore =
            await ReadRequiredAsync<IReadOnlyList<AccountSessionResponse>>(sessionsBeforeResponse);
        AccountSessionResponse current = Assert.Single(sessionsBefore, session => session.IsCurrent);
        AccountSessionResponse other = Assert.Single(sessionsBefore, session => !session.IsCurrent);

        using HttpResponseMessage revoke = await client.SendAsync(AuthorizedWrite(
            HttpMethod.Delete,
            $"/api/v1/account/sessions/{other.Id}",
            firstToken));
        Assert.Equal(HttpStatusCode.NoContent, revoke.StatusCode);

        using HttpResponseMessage profileUpdate = await client.SendAsync(AuthorizedWrite(
            HttpMethod.Put,
            "/api/v1/account/profile",
            firstToken,
            new UpdateProfileRequest("Portfolio Owner")));
        profileUpdate.EnsureSuccessStatusCode();
        AccountProfileResponse profile = await ReadRequiredAsync<AccountProfileResponse>(
            profileUpdate);
        Assert.Equal("Portfolio Owner", profile.DisplayName);

        using HttpResponseMessage roleChange = await client.SendAsync(AuthorizedWrite(
            HttpMethod.Patch,
            $"/api/v1/memberships/{ReceptionistMembershipId}",
            firstToken,
            new UpdateMembershipRequest("Employee", true)));
        roleChange.EnsureSuccessStatusCode();
        using HttpResponseMessage roleRestore = await client.SendAsync(AuthorizedWrite(
            HttpMethod.Patch,
            $"/api/v1/memberships/{ReceptionistMembershipId}",
            firstToken,
            new UpdateMembershipRequest("Receptionist", true)));
        roleRestore.EnsureSuccessStatusCode();

        using HttpResponseMessage auditResponse = await client.SendAsync(Authorized(
            HttpMethod.Get,
            $"/api/v1/audit?action=membership.authorization-changed&actorUserId={OwnerUserId}&page=1&pageSize=20",
            firstToken));
        auditResponse.EnsureSuccessStatusCode();
        PagedResponse<AuditResponse> audit =
            await ReadRequiredAsync<PagedResponse<AuditResponse>>(auditResponse);
        Assert.Equal(2, audit.TotalCount);
        Assert.All(audit.Items, entry =>
        {
            Assert.Equal("Portfolio Owner", entry.ActorName);
            Assert.DoesNotContain("password", entry.Summary ?? string.Empty, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("token", entry.Summary ?? string.Empty, StringComparison.OrdinalIgnoreCase);
        });

        using HttpResponseMessage sessionsAfterResponse = await client.SendAsync(Authorized(
            HttpMethod.Get,
            "/api/v1/account/sessions",
            firstToken));
        sessionsAfterResponse.EnsureSuccessStatusCode();
        IReadOnlyList<AccountSessionResponse> sessionsAfter =
            await ReadRequiredAsync<IReadOnlyList<AccountSessionResponse>>(sessionsAfterResponse);
        Assert.Single(sessionsAfter);
        Assert.Equal(current.Id, sessionsAfter[0].Id);

        using HttpResponseMessage profileRestore = await client.SendAsync(AuthorizedWrite(
            HttpMethod.Put,
            "/api/v1/account/profile",
            firstToken,
            new UpdateProfileRequest("Demo Owner")));
        profileRestore.EnsureSuccessStatusCode();
    }

    private async Task InsertAppointmentAsync(
        Guid id,
        DateOnly date,
        int hour,
        string status,
        decimal price)
    {
        DateTimeOffset start = TenantLocalToUtc(date, hour);
        await using var connection = new NpgsqlConnection(_factory.ConnectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(
            """
            INSERT INTO appointments (
                id, tenant_id, customer_id, employee_id, service_id, status,
                starts_at_utc, ends_at_utc, service_name, service_duration_minutes,
                service_price, service_currency, notes, revision, created_at_utc, updated_at_utc)
            VALUES (
                @id, @tenantId, @customerId, @employeeId, @serviceId, @status,
                @startsAt, @endsAt, 'Historical consultation', 30,
                @price, 'TRY', NULL, 1, @createdAt, @createdAt);
            """,
            connection);
        command.Parameters.AddWithValue("id", id);
        command.Parameters.AddWithValue("tenantId", AtlasTenantId);
        command.Parameters.AddWithValue("customerId", CustomerId);
        command.Parameters.AddWithValue("employeeId", EmployeeId);
        command.Parameters.AddWithValue("serviceId", ServiceId);
        command.Parameters.AddWithValue("status", status);
        command.Parameters.AddWithValue("startsAt", start);
        command.Parameters.AddWithValue("endsAt", start.AddMinutes(30));
        command.Parameters.AddWithValue("price", price);
        command.Parameters.AddWithValue("createdAt", start.AddDays(-1));
        await command.ExecuteNonQueryAsync();
    }

    private async Task<(long Total, long Completed, decimal Revenue)> ReadDirectReportAsync(
        DateOnly fromDate,
        DateOnly toDate)
    {
        DateTimeOffset fromUtc = TenantLocalToUtc(fromDate, 0);
        DateTimeOffset toUtc = TenantLocalToUtc(toDate.AddDays(1), 0);
        await using var connection = new NpgsqlConnection(_factory.ConnectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(
            """
            SELECT
                count(*),
                count(*) FILTER (WHERE status = 'Completed'),
                COALESCE(sum(service_price) FILTER (WHERE status = 'Completed'), 0)
            FROM appointments
            WHERE tenant_id = @tenantId
              AND starts_at_utc >= @fromUtc
              AND starts_at_utc < @toUtc;
            """,
            connection);
        command.Parameters.AddWithValue("tenantId", AtlasTenantId);
        command.Parameters.AddWithValue("fromUtc", fromUtc);
        command.Parameters.AddWithValue("toUtc", toUtc);
        await using NpgsqlDataReader reader = await command.ExecuteReaderAsync();
        Assert.True(await reader.ReadAsync());
        return (reader.GetInt64(0), reader.GetInt64(1), reader.GetDecimal(2));
    }

    private HttpClient CreateClient() => _factory.CreateClient(new WebApplicationFactoryClientOptions
    {
        BaseAddress = new Uri("https://localhost"),
        HandleCookies = false,
    });

    private static HttpRequestMessage Authorized(
        HttpMethod method,
        string uri,
        string token,
        object? body = null)
    {
        var request = new HttpRequestMessage(method, uri);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        if (body is not null)
        {
            request.Content = JsonContent.Create(body);
        }

        return request;
    }

    private static HttpRequestMessage AuthorizedWrite(
        HttpMethod method,
        string uri,
        string token,
        object? body = null)
    {
        HttpRequestMessage request = Authorized(method, uri, token, body);
        request.Headers.Add("Origin", "http://localhost:5173");
        return request;
    }

    private static async Task<string> LoginAsync(
        HttpClient client,
        string email,
        Guid tenantId)
    {
        using HttpResponseMessage response = await client.PostAsJsonAsync(
            "/api/v1/auth/login",
            new LoginRequest(email, IdentitySecurityTests.DemoPassword, tenantId));
        response.EnsureSuccessStatusCode();
        AuthenticationResponse payload = await ReadRequiredAsync<AuthenticationResponse>(response);
        return payload.AccessToken
            ?? throw new InvalidOperationException("Login response did not contain an access token.");
    }

    private static async Task<T> ReadRequiredAsync<T>(HttpResponseMessage response) =>
        await response.Content.ReadFromJsonAsync<T>()
        ?? throw new InvalidOperationException("The API response body was empty.");

    private static DateOnly TenantToday()
    {
        TimeZoneInfo timeZone = TimeZoneInfo.FindSystemTimeZoneById("Europe/Istanbul");
        return DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, timeZone).Date);
    }

    private static DateTimeOffset TenantLocalToUtc(DateOnly date, int hour)
    {
        TimeZoneInfo timeZone = TimeZoneInfo.FindSystemTimeZoneById("Europe/Istanbul");
        var local = new DateTime(
            date.Year,
            date.Month,
            date.Day,
            hour,
            0,
            0,
            DateTimeKind.Unspecified);
        return new DateTimeOffset(TimeZoneInfo.ConvertTimeToUtc(local, timeZone), TimeSpan.Zero);
    }
}
