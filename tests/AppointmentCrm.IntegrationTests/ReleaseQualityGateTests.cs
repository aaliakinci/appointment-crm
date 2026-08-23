using System.Diagnostics;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using AppointmentCrm.Contracts;
using AppointmentCrm.Infrastructure;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Npgsql;

namespace AppointmentCrm.IntegrationTests;

public sealed class ReleaseQualityGateTests : IClassFixture<ApiFactory>, IAsyncLifetime
{
    private static readonly Guid AtlasTenantId =
        Guid.Parse("10000000-0000-0000-0000-000000000001");
    private static readonly Guid AtlasCustomerId =
        Guid.Parse("40000000-0000-0000-0000-000000000001");
    private static readonly Guid AtlasServiceId =
        Guid.Parse("50000000-0000-0000-0000-000000000001");
    private static readonly Guid AtlasEmployeeId =
        Guid.Parse("60000000-0000-0000-0000-000000000001");
    private static readonly Guid NorthwindCustomerId =
        Guid.Parse("40000000-0000-0000-0000-000000000002");
    private static readonly Guid NorthwindServiceId =
        Guid.Parse("50000000-0000-0000-0000-000000000002");
    private static readonly Guid NorthwindEmployeeId =
        Guid.Parse("60000000-0000-0000-0000-000000000002");
    private static readonly Guid NorthwindOwnerMembershipId =
        Guid.Parse("30000000-0000-0000-0000-000000000006");

    private readonly ApiFactory _factory;

    public ReleaseQualityGateTests(ApiFactory factory)
    {
        _factory = factory;
    }

    public async Task InitializeAsync()
    {
        await MigrationRunner.RunAsync(_factory.Services);
        await using var connection = new NpgsqlConnection(_factory.ConnectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(
            """
            TRUNCATE TABLE
                notification_deliveries,
                appointment_status_history,
                appointments,
                outbox_messages,
                audit_entries,
                user_sessions;
            """,
            connection);
        await command.ExecuteNonQueryAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task AuthorizationMatrix_EnforcesEveryWorkspaceRoleServerSide()
    {
        using HttpClient client = CreateClient(_factory);
        DateOnly today = TenantToday();
        DateOnly bookingDate = NextBusinessDate(today);
        var surfaces = new[]
        {
            new Surface("customers", "/api/v1/customers?page=1&pageSize=1", true, true, true, false),
            new Surface("services", "/api/v1/services?page=1&pageSize=1", true, true, true, true),
            new Surface("employees", "/api/v1/employees?page=1&pageSize=1", true, true, true, false),
            new Surface(
                "appointments",
                $"/api/v1/appointments?fromDate={today:yyyy-MM-dd}&toDate={today:yyyy-MM-dd}",
                true,
                true,
                true,
                false),
            new Surface(
                "own appointments",
                $"/api/v1/my/appointments?fromDate={today:yyyy-MM-dd}&toDate={today:yyyy-MM-dd}",
                true,
                false,
                false,
                true),
            new Surface(
                "availability",
                $"/api/v1/availability?date={bookingDate:yyyy-MM-dd}&employeeId={AtlasEmployeeId}&serviceId={AtlasServiceId}",
                true,
                true,
                true,
                true),
            new Surface(
                "scheduling",
                "/api/v1/scheduling/working-hours/tenant",
                true,
                true,
                false,
                false),
            new Surface("memberships", "/api/v1/memberships", true, true, false, false),
            new Surface(
                "reporting",
                $"/api/v1/reporting/dashboard?fromDate={today:yyyy-MM-dd}&toDate={today:yyyy-MM-dd}",
                true,
                true,
                false,
                false),
        };
        var roles = new[]
        {
            new RoleLogin("owner", "owner@demo.local", 0),
            new RoleLogin("manager", "manager@demo.local", 1),
            new RoleLogin("receptionist", "receptionist@demo.local", 2),
            new RoleLogin("employee", "employee@demo.local", 3),
        };

        foreach (RoleLogin role in roles)
        {
            string token = await LoginAsync(client, role.Email);
            foreach (Surface surface in surfaces)
            {
                using HttpResponseMessage response = await client.SendAsync(
                    Authorized(HttpMethod.Get, surface.Path, token));
                bool allowed = surface.Expected[role.ExpectationIndex];
                HttpStatusCode expectedStatus = allowed
                    ? HttpStatusCode.OK
                    : HttpStatusCode.Forbidden;
                if (surface.Name == "own appointments" && role.Name == "owner")
                {
                    expectedStatus = HttpStatusCode.NotFound;
                }

                Assert.Equal(
                    expectedStatus,
                    response.StatusCode);
            }
        }

        using HttpResponseMessage anonymous = await client.GetAsync(
            $"/api/v1/appointments?fromDate={today:yyyy-MM-dd}&toDate={today:yyyy-MM-dd}");
        Assert.Equal(HttpStatusCode.Unauthorized, anonymous.StatusCode);
    }

    [Fact]
    public async Task KnownForeignTenantIdentifiers_RemainNonDisclosingAcrossFeatures()
    {
        using HttpClient client = CreateClient(_factory);
        string token = await LoginAsync(client, "owner@demo.local");
        string[] paths =
        [
            $"/api/v1/customers/{NorthwindCustomerId}",
            $"/api/v1/services/{NorthwindServiceId}",
            $"/api/v1/employees/{NorthwindEmployeeId}",
            $"/api/v1/memberships/{NorthwindOwnerMembershipId}",
        ];

        foreach (string path in paths)
        {
            using HttpResponseMessage response = await client.SendAsync(
                Authorized(HttpMethod.Get, path, token));
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        using HttpResponseMessage rejected = await client.SendAsync(AuthorizedWrite(
            HttpMethod.Post,
            "/api/v1/appointments",
            token,
            new CreateAppointmentRequest(
                NorthwindCustomerId,
                AtlasEmployeeId,
                AtlasServiceId,
                NextBookingStartUtc(),
                null)));
        Assert.Equal(HttpStatusCode.NotFound, rejected.StatusCode);
        await AssertNoAppointmentSideEffectsAsync();
    }

    [Fact]
    public async Task EightParallelRequests_CommitExactlyOneAppointmentGraph()
    {
        using WebApplicationFactory<Program> factory = _factory.WithWebHostBuilder(builder =>
            builder.ConfigureAppConfiguration((_, configuration) =>
                configuration.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Security:WritePermitLimit"] = "10000",
                })));
        using HttpClient client = CreateClient(factory);
        string token = await LoginAsync(client, "receptionist@demo.local");
        DateTimeOffset startsAtUtc = NextBookingStartUtc();
        var input = new CreateAppointmentRequest(
            AtlasCustomerId,
            AtlasEmployeeId,
            AtlasServiceId,
            startsAtUtc,
            "Release concurrency gate");

        Task<HttpResponseMessage>[] requests = Enumerable.Range(0, 8)
            .Select(_ => client.SendAsync(AuthorizedWrite(
                HttpMethod.Post,
                "/api/v1/appointments",
                token,
                input)))
            .ToArray();
        HttpResponseMessage[] responses = await Task.WhenAll(requests);
        try
        {
            Assert.Single(responses, response => response.StatusCode == HttpStatusCode.Created);
            int conflictCount = responses.Count(
                response => response.StatusCode == HttpStatusCode.Conflict);
            if (conflictCount != 7)
            {
                string statusSummary = string.Join(
                    ", ",
                    responses
                        .GroupBy(response => response.StatusCode)
                        .OrderBy(group => group.Key)
                        .Select(group => $"{(int)group.Key}={group.Count()}"));
                string[] unexpectedBodies = await Task.WhenAll(responses
                    .Where(response => response.StatusCode is not HttpStatusCode.Created
                        and not HttpStatusCode.Conflict)
                    .Select(response => response.Content.ReadAsStringAsync()));
                Assert.Fail(
                    $"Expected seven conflicts. Statuses: {statusSummary}. "
                    + $"Unexpected bodies: {string.Join(" | ", unexpectedBodies)}");
            }

            foreach (HttpResponseMessage conflict in responses.Where(
                         response => response.StatusCode == HttpStatusCode.Conflict))
            {
                Assert.Contains(
                    await ReadProblemCodeAsync(conflict),
                    new[] { "appointments.slot_unavailable", "appointments.time_conflict" });
            }
        }
        finally
        {
            foreach (HttpResponseMessage response in responses)
            {
                response.Dispose();
            }
        }

        await AssertSingleAppointmentGraphAsync(startsAtUtc);
    }

    [Fact]
    public async Task BoundedListAndReportLoad_CompletesTwentyConcurrentReads()
    {
        DateOnly toDate = TenantToday();
        DateOnly fromDate = toDate.AddDays(-29);
        await InsertCancelledAppointmentsAsync(fromDate, 1_000);
        using HttpClient client = CreateClient(_factory);
        string token = await LoginAsync(client, "manager@demo.local");
        string listPath =
            $"/api/v1/appointments?fromDate={fromDate:yyyy-MM-dd}&toDate={toDate:yyyy-MM-dd}&status=cancelled&page=1&pageSize=100";
        string reportPath =
            $"/api/v1/reporting/dashboard?fromDate={fromDate:yyyy-MM-dd}&toDate={toDate:yyyy-MM-dd}&employeeId={AtlasEmployeeId}&status=cancelled";

        var stopwatch = Stopwatch.StartNew();
        Task<HttpResponseMessage>[] requests = Enumerable.Range(0, 10)
            .SelectMany(_ => new[]
            {
                client.SendAsync(Authorized(HttpMethod.Get, listPath, token)),
                client.SendAsync(Authorized(HttpMethod.Get, reportPath, token)),
            })
            .ToArray();
        HttpResponseMessage[] responses = await Task.WhenAll(requests);
        stopwatch.Stop();
        try
        {
            Assert.All(responses, response => Assert.Equal(HttpStatusCode.OK, response.StatusCode));
            PagedResponse<AppointmentSummaryResponse> page =
                await ReadRequiredAsync<PagedResponse<AppointmentSummaryResponse>>(responses[0]);
            ReportingDashboardResponse dashboard =
                await ReadRequiredAsync<ReportingDashboardResponse>(responses[1]);
            Assert.True(page.TotalCount >= 1_000);
            Assert.True(dashboard.Range.TotalAppointments >= 1_000);
            Assert.True(
                stopwatch.Elapsed < TimeSpan.FromSeconds(15),
                $"Bounded list/report load took {stopwatch.Elapsed}.");
        }
        finally
        {
            foreach (HttpResponseMessage response in responses)
            {
                response.Dispose();
            }
        }
    }

    [Fact]
    public async Task CriticalJourney_CreatesCompletesAndReportsAppointmentDeterministically()
    {
        var clock = new ManualTimeProvider(TimeProvider.System.GetUtcNow());
        using WebApplicationFactory<Program> factory = _factory.WithWebHostBuilder(builder =>
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<TimeProvider>();
                services.AddSingleton<TimeProvider>(clock);
            }));
        await MigrationRunner.RunAsync(factory.Services);
        using HttpClient client = CreateClient(factory);
        string token = await LoginAsync(client, "owner@demo.local");
        string suffix = Guid.NewGuid().ToString("N")[..8];

        CustomerResponse customer = await SendAndReadAsync<CustomerResponse>(client, AuthorizedWrite(
            HttpMethod.Post,
            "/api/v1/customers",
            token,
            new CreateCustomerRequest(
                $"Release Customer {suffix}",
                $"release-{suffix}@example.test",
                null,
                null)));
        ServiceResponse service = await SendAndReadAsync<ServiceResponse>(client, AuthorizedWrite(
            HttpMethod.Post,
            "/api/v1/services",
            token,
            new CreateServiceRequest($"Release Service {suffix}", 30, 321.25m, "TRY")));
        EmployeeResponse employee = await SendAndReadAsync<EmployeeResponse>(client, AuthorizedWrite(
            HttpMethod.Post,
            "/api/v1/employees",
            token,
            new CreateEmployeeRequest(
                null,
                $"Release Employee {suffix}",
                null,
                null,
                [service.Id])));
        DateTimeOffset startsAtUtc = NextBookingStartUtc(clock.GetUtcNow());
        AppointmentResponse created = await SendAndReadAsync<AppointmentResponse>(client, AuthorizedWrite(
            HttpMethod.Post,
            "/api/v1/appointments",
            token,
            new CreateAppointmentRequest(
                customer.Id,
                employee.Id,
                service.Id,
                startsAtUtc,
                "Deterministic release journey")));
        AppointmentResponse confirmed = await SendAndReadAsync<AppointmentResponse>(client, AuthorizedWrite(
            HttpMethod.Post,
            $"/api/v1/appointments/{created.Appointment.Id}/confirm",
            token,
            new TransitionAppointmentRequest(created.Appointment.Revision, null)));

        clock.SetUtcNow(startsAtUtc.AddMinutes(1));
        token = await LoginAsync(client, "owner@demo.local");
        AppointmentResponse completed = await SendAndReadAsync<AppointmentResponse>(client, AuthorizedWrite(
            HttpMethod.Post,
            $"/api/v1/appointments/{created.Appointment.Id}/complete",
            token,
            new TransitionAppointmentRequest(confirmed.Appointment.Revision, "Journey complete")));
        Assert.Equal("completed", completed.Appointment.Status);

        DateOnly localDate = LocalDate(startsAtUtc);
        using HttpResponseMessage reportResponse = await client.SendAsync(Authorized(
            HttpMethod.Get,
            $"/api/v1/reporting/dashboard?fromDate={localDate:yyyy-MM-dd}&toDate={localDate:yyyy-MM-dd}&employeeId={employee.Id}",
            token));
        reportResponse.EnsureSuccessStatusCode();
        ReportingDashboardResponse dashboard =
            await ReadRequiredAsync<ReportingDashboardResponse>(reportResponse);
        Assert.Equal(1, dashboard.Range.TotalAppointments);
        Assert.Equal(1, dashboard.Range.CompletedAppointments);
        Assert.Equal(321.25m, dashboard.Range.CompletedRevenue);
    }

    private async Task AssertNoAppointmentSideEffectsAsync()
    {
        await using var connection = new NpgsqlConnection(_factory.ConnectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(
            """
            SELECT
                (SELECT count(*) FROM appointments),
                (SELECT count(*) FROM appointment_status_history),
                (SELECT count(*) FROM audit_entries WHERE action LIKE 'appointment.%'),
                (SELECT count(*) FROM outbox_messages WHERE type LIKE 'appointment.%');
            """,
            connection);
        await using NpgsqlDataReader reader = await command.ExecuteReaderAsync();
        Assert.True(await reader.ReadAsync());
        Assert.Equal(0L, reader.GetInt64(0));
        Assert.Equal(0L, reader.GetInt64(1));
        Assert.Equal(0L, reader.GetInt64(2));
        Assert.Equal(0L, reader.GetInt64(3));
    }

    private async Task AssertSingleAppointmentGraphAsync(DateTimeOffset startsAtUtc)
    {
        await using var connection = new NpgsqlConnection(_factory.ConnectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(
            """
            SELECT
                (SELECT count(*) FROM appointments WHERE starts_at_utc = @startsAtUtc),
                (SELECT count(*) FROM appointment_status_history),
                (SELECT count(*) FROM audit_entries WHERE action = 'appointment.created'),
                (SELECT count(*) FROM outbox_messages WHERE type = 'appointment.created');
            """,
            connection);
        command.Parameters.AddWithValue("startsAtUtc", startsAtUtc);
        await using NpgsqlDataReader reader = await command.ExecuteReaderAsync();
        Assert.True(await reader.ReadAsync());
        Assert.Equal(1L, reader.GetInt64(0));
        Assert.Equal(1L, reader.GetInt64(1));
        Assert.Equal(1L, reader.GetInt64(2));
        Assert.Equal(1L, reader.GetInt64(3));
    }

    private async Task InsertCancelledAppointmentsAsync(DateOnly fromDate, int count)
    {
        TimeZoneInfo timeZone = TimeZoneInfo.FindSystemTimeZoneById("Europe/Istanbul");
        DateTime local = DateTime.SpecifyKind(
            fromDate.ToDateTime(new TimeOnly(9, 0)),
            DateTimeKind.Unspecified);
        DateTimeOffset startsAtUtc = new(
            TimeZoneInfo.ConvertTimeToUtc(local, timeZone),
            TimeSpan.Zero);
        await using var connection = new NpgsqlConnection(_factory.ConnectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(
            """
            INSERT INTO appointments (
                id, tenant_id, customer_id, employee_id, service_id, status,
                starts_at_utc, ends_at_utc, service_name, service_duration_minutes,
                service_price, service_currency, notes, revision, created_at_utc, updated_at_utc)
            SELECT
                gen_random_uuid(), @tenantId, @customerId, @employeeId, @serviceId, 'Cancelled',
                @startsAtUtc + ((item % 30) * interval '1 day') + ((item % 32) * interval '15 minutes'),
                @startsAtUtc + ((item % 30) * interval '1 day') + ((item % 32) * interval '15 minutes') + interval '30 minutes',
                'Load test service', 30, 50.00, 'TRY', NULL, 1, now(), now()
            FROM generate_series(0, @count - 1) AS item;
            """,
            connection);
        command.Parameters.AddWithValue("tenantId", AtlasTenantId);
        command.Parameters.AddWithValue("customerId", AtlasCustomerId);
        command.Parameters.AddWithValue("employeeId", AtlasEmployeeId);
        command.Parameters.AddWithValue("serviceId", AtlasServiceId);
        command.Parameters.AddWithValue("startsAtUtc", startsAtUtc);
        command.Parameters.AddWithValue("count", count);
        Assert.Equal(count, await command.ExecuteNonQueryAsync());
    }

    private static HttpClient CreateClient(WebApplicationFactory<Program> factory) =>
        factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost"),
            AllowAutoRedirect = false,
        });

    private static async Task<string> LoginAsync(HttpClient client, string email)
    {
        using HttpResponseMessage response = await client.PostAsJsonAsync(
            "/api/v1/auth/login",
            new LoginRequest(email, IdentitySecurityTests.DemoPassword, AtlasTenantId));
        response.EnsureSuccessStatusCode();
        AuthenticationResponse authentication =
            await ReadRequiredAsync<AuthenticationResponse>(response);
        return authentication.AccessToken
            ?? throw new InvalidOperationException("Login did not return an access token.");
    }

    private static HttpRequestMessage Authorized(
        HttpMethod method,
        string path,
        string token)
    {
        var request = new HttpRequestMessage(method, path);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return request;
    }

    private static HttpRequestMessage AuthorizedWrite<T>(
        HttpMethod method,
        string path,
        string token,
        T body)
    {
        HttpRequestMessage request = Authorized(method, path, token);
        request.Headers.Add("Origin", "http://localhost:5173");
        request.Content = JsonContent.Create(body);
        return request;
    }

    private static async Task<T> SendAndReadAsync<T>(
        HttpClient client,
        HttpRequestMessage request)
    {
        using (request)
        using (HttpResponseMessage response = await client.SendAsync(request))
        {
            response.EnsureSuccessStatusCode();
            return await ReadRequiredAsync<T>(response);
        }
    }

    private static async Task<T> ReadRequiredAsync<T>(HttpResponseMessage response) =>
        await response.Content.ReadFromJsonAsync<T>()
        ?? throw new InvalidOperationException($"Response did not contain {typeof(T).Name}.");

    private static async Task<string> ReadProblemCodeAsync(HttpResponseMessage response)
    {
        using JsonDocument problem = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return problem.RootElement.GetProperty("code").GetString()
            ?? throw new InvalidOperationException("Problem response did not contain a code.");
    }

    private static DateOnly TenantToday() => LocalDate(TimeProvider.System.GetUtcNow());

    private static DateOnly LocalDate(DateTimeOffset instant)
    {
        TimeZoneInfo timeZone = TimeZoneInfo.FindSystemTimeZoneById("Europe/Istanbul");
        return DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(instant, timeZone).DateTime);
    }

    private static DateTimeOffset NextBookingStartUtc() =>
        NextBookingStartUtc(TimeProvider.System.GetUtcNow());

    private static DateTimeOffset NextBookingStartUtc(DateTimeOffset now)
    {
        TimeZoneInfo timeZone = TimeZoneInfo.FindSystemTimeZoneById("Europe/Istanbul");
        DateOnly date = NextBusinessDate(LocalDate(now));
        DateTime local = DateTime.SpecifyKind(
            date.ToDateTime(new TimeOnly(10, 0)),
            DateTimeKind.Unspecified);
        return new DateTimeOffset(TimeZoneInfo.ConvertTimeToUtc(local, timeZone), TimeSpan.Zero);
    }

    private static DateOnly NextBusinessDate(DateOnly date)
    {
        do
        {
            date = date.AddDays(1);
        }
        while (date.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday);

        return date;
    }

    private sealed record RoleLogin(string Name, string Email, int ExpectationIndex);

    private sealed record Surface(
        string Name,
        string Path,
        bool Owner,
        bool Manager,
        bool Receptionist,
        bool Employee)
    {
        public bool[] Expected { get; } = [Owner, Manager, Receptionist, Employee];
    }

    private sealed class ManualTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        private DateTimeOffset _utcNow = utcNow;

        public override DateTimeOffset GetUtcNow() => _utcNow;

        public void SetUtcNow(DateTimeOffset value) => _utcNow = value.ToUniversalTime();
    }

}
