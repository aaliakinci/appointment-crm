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

public sealed class AppointmentTests : IClassFixture<ApiFactory>, IAsyncLifetime
{
    private static readonly Guid AtlasTenantId =
        Guid.Parse("10000000-0000-0000-0000-000000000001");
    private static readonly Guid NorthwindTenantId =
        Guid.Parse("10000000-0000-0000-0000-000000000002");
    private static readonly Guid AtlasCustomerId =
        Guid.Parse("40000000-0000-0000-0000-000000000001");
    private static readonly Guid AtlasServiceId =
        Guid.Parse("50000000-0000-0000-0000-000000000001");
    private static readonly Guid AtlasEmployeeId =
        Guid.Parse("60000000-0000-0000-0000-000000000001");
    private static DateTimeOffset BookingStartUtc => GetNextBusinessDayAtTenUtc();

    private readonly ApiFactory _factory;

    public AppointmentTests(ApiFactory factory)
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
                outbox_messages,
                audit_entries,
                date_schedule_override_periods,
                date_schedule_overrides,
                employee_time_offs,
                weekly_schedule_version_periods,
                weekly_schedule_versions,
                weekly_schedules,
                user_sessions;

            UPDATE services
            SET name = 'Consultation',
                normalized_name = 'CONSULTATION',
                duration_minutes = 30,
                price = 750.00,
                currency = 'TRY'
            WHERE id = '50000000-0000-0000-0000-000000000001'::uuid;
            """);
        await MigrationRunner.RunAsync(_factory.Services);
    }

    public async Task DisposeAsync()
    {
        await using AsyncServiceScope scope = _factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppointmentCrmDbContext>();
        await dbContext.Database.ExecuteSqlRawAsync("""
            UPDATE services
            SET name = 'Consultation',
                normalized_name = 'CONSULTATION',
                duration_minutes = 30,
                price = 750.00,
                currency = 'TRY'
            WHERE id = '50000000-0000-0000-0000-000000000001'::uuid;
            """);
    }

    [Fact]
    public async Task Create_PersistsSnapshotHistoryAuditAndOutboxAtomically()
    {
        using HttpClient client = CreateClient();
        string token = await LoginAsync(client, "receptionist@demo.local", AtlasTenantId);

        AppointmentResponse appointment = await CreateAppointmentAsync(
            client,
            token,
            BookingStartUtc);

        Assert.Equal("scheduled", appointment.Appointment.Status);
        Assert.Equal("Consultation", appointment.Appointment.ServiceName);
        Assert.Equal(30, appointment.Appointment.ServiceDurationMinutes);
        Assert.Equal(750m, appointment.Appointment.ServicePrice);
        Assert.Equal("TRY", appointment.Appointment.ServiceCurrency);
        Assert.Equal(new TimeSpan(3, 0, 0), appointment.Appointment.LocalStart.Offset);
        AppointmentStatusHistoryResponse created = Assert.Single(appointment.StatusHistory);
        Assert.Null(created.FromStatus);
        Assert.Equal("scheduled", created.ToStatus);

        await AssertTransactionalRowsAsync(appointment.Appointment.Id, 1, 1, 1);

        using HttpResponseMessage updateService = await client.SendAsync(AuthorizedWrite(
            HttpMethod.Put,
            $"/api/v1/services/{AtlasServiceId}",
            await LoginAsync(client, "manager@demo.local", AtlasTenantId),
            new UpdateServiceRequest("Consultation Plus", 60, 1_250m, "TRY")));
        updateService.EnsureSuccessStatusCode();

        using HttpResponseMessage get = await client.SendAsync(Authorized(
            HttpMethod.Get,
            $"/api/v1/appointments/{appointment.Appointment.Id}",
            token));
        get.EnsureSuccessStatusCode();
        AppointmentResponse persisted = await ReadRequiredAsync<AppointmentResponse>(get);
        Assert.Equal("Consultation", persisted.Appointment.ServiceName);
        Assert.Equal(30, persisted.Appointment.ServiceDurationMinutes);
        Assert.Equal(750m, persisted.Appointment.ServicePrice);
    }

    [Fact]
    public async Task ParallelOverlap_CommitsExactlyOneAndRollsBackFailedSideEffects()
    {
        using HttpClient client = CreateClient();
        string token = await LoginAsync(client, "receptionist@demo.local", AtlasTenantId);
        var request = new CreateAppointmentRequest(
            AtlasCustomerId,
            AtlasEmployeeId,
            AtlasServiceId,
            BookingStartUtc,
            "Concurrent booking");

        Task<HttpResponseMessage> first = client.SendAsync(AuthorizedWrite(
            HttpMethod.Post,
            "/api/v1/appointments",
            token,
            request));
        Task<HttpResponseMessage> second = client.SendAsync(AuthorizedWrite(
            HttpMethod.Post,
            "/api/v1/appointments",
            token,
            request));
        HttpResponseMessage[] responses = await Task.WhenAll(first, second);
        using HttpResponseMessage success = Assert.Single(
            responses,
            response => response.StatusCode == HttpStatusCode.Created);
        using HttpResponseMessage conflict = Assert.Single(
            responses,
            response => response.StatusCode == HttpStatusCode.Conflict);
        string conflictCode = await ReadProblemCodeAsync(conflict);
        Assert.Contains(
            conflictCode,
            new[] { "appointments.slot_unavailable", "appointments.time_conflict" });
        AppointmentResponse created = await ReadRequiredAsync<AppointmentResponse>(success);

        await AssertTransactionalRowsAsync(created.Appointment.Id, 1, 1, 1);
        await using var connection = new NpgsqlConnection(_factory.ConnectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(
            """
            SELECT
                (SELECT count(*) FROM appointments WHERE tenant_id = @tenantId),
                (SELECT count(*) FROM appointment_status_history WHERE tenant_id = @tenantId),
                (SELECT count(*) FROM audit_entries WHERE tenant_id = @tenantId AND action = 'appointment.created'),
                (SELECT count(*) FROM outbox_messages WHERE tenant_id = @tenantId AND type = 'appointment.created');
            """,
            connection);
        command.Parameters.AddWithValue("tenantId", AtlasTenantId);
        await using NpgsqlDataReader reader = await command.ExecuteReaderAsync();
        Assert.True(await reader.ReadAsync());
        Assert.Equal(1L, reader.GetInt64(0));
        Assert.Equal(1L, reader.GetInt64(1));
        Assert.Equal(1L, reader.GetInt64(2));
        Assert.Equal(1L, reader.GetInt64(3));
    }

    [Fact]
    public async Task Cancellation_ReleasesRangeAndOptimisticConcurrencyRejectsStaleTransition()
    {
        using HttpClient client = CreateClient();
        string token = await LoginAsync(client, "manager@demo.local", AtlasTenantId);
        AppointmentResponse first = await CreateAppointmentAsync(client, token, BookingStartUtc);

        using HttpResponseMessage cancel = await client.SendAsync(AuthorizedWrite(
            HttpMethod.Post,
            $"/api/v1/appointments/{first.Appointment.Id}/cancel",
            token,
            new TransitionAppointmentRequest(first.Appointment.Revision, "Customer request")));
        Assert.True(
            cancel.IsSuccessStatusCode,
            await cancel.Content.ReadAsStringAsync());
        AppointmentResponse cancelled = await ReadRequiredAsync<AppointmentResponse>(cancel);
        Assert.Equal("cancelled", cancelled.Appointment.Status);

        AppointmentResponse replacement = await CreateAppointmentAsync(client, token, BookingStartUtc);
        Assert.NotEqual(first.Appointment.Id, replacement.Appointment.Id);

        DateTimeOffset elevenUtc = BookingStartUtc.AddHours(1);
        AppointmentResponse transitionCandidate = await CreateAppointmentAsync(client, token, elevenUtc);
        var transition = new TransitionAppointmentRequest(
            transitionCandidate.Appointment.Revision,
            null);
        Task<HttpResponseMessage> confirm = client.SendAsync(AuthorizedWrite(
            HttpMethod.Post,
            $"/api/v1/appointments/{transitionCandidate.Appointment.Id}/confirm",
            token,
            transition));
        Task<HttpResponseMessage> cancelConcurrent = client.SendAsync(AuthorizedWrite(
            HttpMethod.Post,
            $"/api/v1/appointments/{transitionCandidate.Appointment.Id}/cancel",
            token,
            transition));
        HttpResponseMessage[] responses = await Task.WhenAll(confirm, cancelConcurrent);
        Assert.Single(responses, response => response.StatusCode == HttpStatusCode.OK);
        using HttpResponseMessage conflict = Assert.Single(
            responses,
            response => response.StatusCode == HttpStatusCode.Conflict);
        Assert.Equal("appointments.version_conflict", await ReadProblemCodeAsync(conflict));
    }

    [Fact]
    public async Task EmployeeOwnSurface_IsScopedAndForeignTenantIdRemainsHidden()
    {
        using HttpClient client = CreateClient();
        string manager = await LoginAsync(client, "manager@demo.local", AtlasTenantId);
        DateTimeOffset bookingStartUtc = BookingStartUtc;
        AppointmentResponse appointment = await CreateAppointmentAsync(client, manager, bookingStartUtc);
        string employee = await LoginAsync(client, "employee@demo.local", AtlasTenantId);

        using HttpResponseMessage ownList = await client.SendAsync(Authorized(
            HttpMethod.Get,
            $"/api/v1/my/appointments?fromDate={LocalDate(bookingStartUtc):yyyy-MM-dd}&toDate={LocalDate(bookingStartUtc):yyyy-MM-dd}&pageSize=20",
            employee));
        ownList.EnsureSuccessStatusCode();
        PagedResponse<AppointmentSummaryResponse> page =
            await ReadRequiredAsync<PagedResponse<AppointmentSummaryResponse>>(ownList);
        Assert.Equal(appointment.Appointment.Id, Assert.Single(page.Items).Id);

        using HttpResponseMessage confirm = await client.SendAsync(AuthorizedWrite(
            HttpMethod.Post,
            $"/api/v1/my/appointments/{appointment.Appointment.Id}/confirm",
            employee,
            new TransitionAppointmentRequest(appointment.Appointment.Revision, null)));
        Assert.True(
            confirm.IsSuccessStatusCode,
            await confirm.Content.ReadAsStringAsync());

        string northwind = await LoginAsync(client, "owner@demo.local", NorthwindTenantId);
        using HttpResponseMessage foreign = await client.SendAsync(Authorized(
            HttpMethod.Get,
            $"/api/v1/appointments/{appointment.Appointment.Id}",
            northwind));
        Assert.Equal(HttpStatusCode.NotFound, foreign.StatusCode);
        Assert.Equal("appointments.not_found", await ReadProblemCodeAsync(foreign));
    }

    [Fact]
    public async Task HalfOpenRanges_AllowTouchingAppointments()
    {
        using HttpClient client = CreateClient();
        string token = await LoginAsync(client, "receptionist@demo.local", AtlasTenantId);

        AppointmentResponse first = await CreateAppointmentAsync(client, token, BookingStartUtc);
        AppointmentResponse touching = await CreateAppointmentAsync(
            client,
            token,
            first.Appointment.EndsAtUtc);

        Assert.Equal(first.Appointment.EndsAtUtc, touching.Appointment.StartsAtUtc);
        Assert.NotEqual(first.Appointment.Id, touching.Appointment.Id);
    }

    [Fact]
    public async Task RescheduleConflictAndInvalidTransition_AreRejectedWithoutChangingState()
    {
        using HttpClient client = CreateClient();
        string token = await LoginAsync(client, "manager@demo.local", AtlasTenantId);
        AppointmentResponse occupied = await CreateAppointmentAsync(client, token, BookingStartUtc);
        AppointmentResponse candidate = await CreateAppointmentAsync(
            client,
            token,
            BookingStartUtc.AddHours(1));

        using HttpResponseMessage reschedule = await client.SendAsync(AuthorizedWrite(
            HttpMethod.Put,
            $"/api/v1/appointments/{candidate.Appointment.Id}/schedule",
            token,
            new RescheduleAppointmentRequest(
                occupied.Appointment.StartsAtUtc,
                candidate.Appointment.Revision)));
        Assert.Equal(HttpStatusCode.Conflict, reschedule.StatusCode);
        Assert.Contains(
            await ReadProblemCodeAsync(reschedule),
            new[] { "appointments.slot_unavailable", "appointments.time_conflict" });

        using HttpResponseMessage confirm = await client.SendAsync(AuthorizedWrite(
            HttpMethod.Post,
            $"/api/v1/appointments/{candidate.Appointment.Id}/confirm",
            token,
            new TransitionAppointmentRequest(candidate.Appointment.Revision, null)));
        confirm.EnsureSuccessStatusCode();
        AppointmentResponse confirmed = await ReadRequiredAsync<AppointmentResponse>(confirm);

        using HttpResponseMessage repeated = await client.SendAsync(AuthorizedWrite(
            HttpMethod.Post,
            $"/api/v1/appointments/{candidate.Appointment.Id}/confirm",
            token,
            new TransitionAppointmentRequest(confirmed.Appointment.Revision, null)));
        Assert.Equal(HttpStatusCode.Conflict, repeated.StatusCode);
        Assert.Equal("appointments.invalid_transition", await ReadProblemCodeAsync(repeated));

        using HttpResponseMessage current = await client.SendAsync(Authorized(
            HttpMethod.Get,
            $"/api/v1/appointments/{candidate.Appointment.Id}",
            token));
        current.EnsureSuccessStatusCode();
        AppointmentResponse persisted = await ReadRequiredAsync<AppointmentResponse>(current);
        Assert.Equal(BookingStartUtc.AddHours(1), persisted.Appointment.StartsAtUtc);
        Assert.Equal("confirmed", persisted.Appointment.Status);
    }

    private async Task AssertTransactionalRowsAsync(
        Guid appointmentId,
        long expectedHistory,
        long expectedAudit,
        long expectedOutbox)
    {
        await using var connection = new NpgsqlConnection(_factory.ConnectionString);
        await connection.OpenAsync();
        const string sql = """
            SELECT
                (SELECT count(*) FROM appointment_status_history WHERE appointment_id = @appointmentId),
                (SELECT count(*) FROM audit_entries WHERE target_id = @appointmentId),
                (SELECT count(*) FROM outbox_messages WHERE aggregate_id = @appointmentId);
            """;
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("appointmentId", appointmentId);
        await using NpgsqlDataReader reader = await command.ExecuteReaderAsync();
        Assert.True(await reader.ReadAsync());
        Assert.Equal(expectedHistory, reader.GetInt64(0));
        Assert.Equal(expectedAudit, reader.GetInt64(1));
        Assert.Equal(expectedOutbox, reader.GetInt64(2));
    }

    private HttpClient CreateClient() => _factory.CreateClient(new WebApplicationFactoryClientOptions
    {
        BaseAddress = new Uri("https://localhost"),
        HandleCookies = false,
    });

    private static async Task<AppointmentResponse> CreateAppointmentAsync(
        HttpClient client,
        string token,
        DateTimeOffset startUtc)
    {
        using HttpResponseMessage response = await client.SendAsync(AuthorizedWrite(
            HttpMethod.Post,
            "/api/v1/appointments",
            token,
            new CreateAppointmentRequest(
                AtlasCustomerId,
                AtlasEmployeeId,
                AtlasServiceId,
                startUtc,
                "Integration test")));
        response.EnsureSuccessStatusCode();
        return await ReadRequiredAsync<AppointmentResponse>(response);
    }

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

    private static async Task<string> ReadProblemCodeAsync(HttpResponseMessage response)
    {
        using JsonDocument problem = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return problem.RootElement.GetProperty("code").GetString()
            ?? throw new InvalidOperationException("Problem response did not contain an error code.");
    }

    private static DateTimeOffset GetNextBusinessDayAtTenUtc()
    {
        TimeZoneInfo timeZone = TimeZoneInfo.FindSystemTimeZoneById("Europe/Istanbul");
        DateOnly date = DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, timeZone).Date);
        do
        {
            date = date.AddDays(1);
        }
        while (date.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday);

        var local = new DateTime(
            date.Year,
            date.Month,
            date.Day,
            10,
            0,
            0,
            DateTimeKind.Unspecified);
        return new DateTimeOffset(TimeZoneInfo.ConvertTimeToUtc(local, timeZone), TimeSpan.Zero);
    }

    private static DateOnly LocalDate(DateTimeOffset instant)
    {
        TimeZoneInfo timeZone = TimeZoneInfo.FindSystemTimeZoneById("Europe/Istanbul");
        return DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(instant, timeZone).Date);
    }
}
