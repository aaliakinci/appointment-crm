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

public sealed class SchedulingTests : IClassFixture<ApiFactory>, IAsyncLifetime
{
    private static readonly Guid AtlasTenantId =
        Guid.Parse("10000000-0000-0000-0000-000000000001");
    private static readonly Guid NorthwindTenantId =
        Guid.Parse("10000000-0000-0000-0000-000000000002");
    private static readonly Guid AtlasServiceId =
        Guid.Parse("50000000-0000-0000-0000-000000000001");
    private static readonly Guid AtlasEmployeeId =
        Guid.Parse("60000000-0000-0000-0000-000000000001");

    private readonly ApiFactory _factory;

    public SchedulingTests(ApiFactory factory)
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
                date_schedule_override_periods,
                date_schedule_overrides,
                employee_time_offs,
                weekly_schedule_version_periods,
                weekly_schedule_versions,
                weekly_schedules,
                user_sessions;
            """);
        await MigrationRunner.RunAsync(_factory.Services);
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Availability_AppliesHalfOpenTimeOffAndOverridePrecedence()
    {
        using HttpClient client = CreateClient();
        string token = await LoginAsync(client, "owner@demo.local", AtlasTenantId);
        var date = new DateOnly(2026, 8, 24);

        using HttpResponseMessage weeklyResponse = await client.SendAsync(AuthorizedWrite(
            HttpMethod.Put,
            "/api/v1/scheduling/working-hours/tenant",
            token,
            new PutWeeklyScheduleRequest(
                1,
                [new WeeklySchedulePeriodRequest(1, 10 * 60, 12 * 60)],
                "Monday availability")));
        weeklyResponse.EnsureSuccessStatusCode();

        AvailabilityResponse before = await GetAvailabilityAsync(client, token, date);
        Assert.Contains(before.Slots, slot => LocalTime(slot.LocalStart) == new TimeOnly(10, 0));
        Assert.Contains(before.Slots, slot => LocalTime(slot.LocalStart) == new TimeOnly(11, 30));
        Assert.All(before.Slots, slot => Assert.Equal(TimeSpan.FromHours(3), slot.LocalStart.Offset));

        using HttpResponseMessage timeOffResponse = await client.SendAsync(AuthorizedWrite(
            HttpMethod.Post,
            "/api/v1/scheduling/time-off",
            token,
            new CreateTimeOffRequest(
                AtlasEmployeeId,
                date,
                new TimeOnly(10, 30),
                date,
                new TimeOnly(11, 0),
                "Europe/Istanbul",
                "Training")));
        Assert.Equal(HttpStatusCode.Created, timeOffResponse.StatusCode);

        AvailabilityResponse after = await GetAvailabilityAsync(client, token, date);
        Assert.Contains(after.Slots, slot => LocalTime(slot.LocalStart) == new TimeOnly(10, 0));
        Assert.DoesNotContain(after.Slots, slot => LocalTime(slot.LocalStart) == new TimeOnly(10, 5));
        Assert.Contains(after.Slots, slot => LocalTime(slot.LocalStart) == new TimeOnly(11, 0));

        using HttpResponseMessage adjacent = await client.SendAsync(AuthorizedWrite(
            HttpMethod.Post,
            "/api/v1/scheduling/time-off",
            token,
            new CreateTimeOffRequest(
                AtlasEmployeeId,
                date,
                new TimeOnly(11, 0),
                date,
                new TimeOnly(11, 30),
                "Europe/Istanbul",
                null)));
        Assert.Equal(HttpStatusCode.Created, adjacent.StatusCode);

        using HttpResponseMessage overlap = await client.SendAsync(AuthorizedWrite(
            HttpMethod.Post,
            "/api/v1/scheduling/time-off",
            token,
            new CreateTimeOffRequest(
                AtlasEmployeeId,
                date,
                new TimeOnly(10, 45),
                date,
                new TimeOnly(11, 15),
                "Europe/Istanbul",
                null)));
        Assert.Equal(HttpStatusCode.Conflict, overlap.StatusCode);
        await AssertProblemCodeAsync(overlap, "scheduling.time_off_overlap");

        using HttpResponseMessage tenantClosed = await client.SendAsync(AuthorizedWrite(
            HttpMethod.Put,
            $"/api/v1/scheduling/date-overrides/tenant/{date:yyyy-MM-dd}",
            token,
            new PutDateOverrideRequest(true, [])));
        tenantClosed.EnsureSuccessStatusCode();
        Assert.Empty((await GetAvailabilityAsync(client, token, date)).Slots);

        using HttpResponseMessage employeeOpen = await client.SendAsync(AuthorizedWrite(
            HttpMethod.Put,
            $"/api/v1/scheduling/date-overrides/employees/{AtlasEmployeeId}/{date:yyyy-MM-dd}",
            token,
            new PutDateOverrideRequest(
                false,
                [new DateSchedulePeriodRequest(14 * 60, 15 * 60)])));
        employeeOpen.EnsureSuccessStatusCode();
        AvailabilityResponse employeeOverride = await GetAvailabilityAsync(client, token, date);
        Assert.Contains(
            employeeOverride.Slots,
            slot => LocalTime(slot.LocalStart) == new TimeOnly(14, 0));
    }

    [Fact]
    public async Task EmployeeWeeklySchedule_CanReplaceAndRestoreTenantInheritance()
    {
        using HttpClient client = CreateClient();
        string token = await LoginAsync(client, "manager@demo.local", AtlasTenantId);

        using HttpResponseMessage inheritedResponse = await client.SendAsync(Authorized(
            HttpMethod.Get,
            $"/api/v1/scheduling/working-hours/employees/{AtlasEmployeeId}",
            token));
        WeeklyScheduleResponse? inherited = await inheritedResponse.Content
            .ReadFromJsonAsync<WeeklyScheduleResponse>();
        Assert.NotNull(inherited);
        Assert.Equal("inherited", inherited.State);
        Assert.Equal("tenant", inherited.Source);

        using HttpResponseMessage replaceResponse = await client.SendAsync(AuthorizedWrite(
            HttpMethod.Put,
            $"/api/v1/scheduling/working-hours/employees/{AtlasEmployeeId}",
            token,
            new PutWeeklyScheduleRequest(
                inherited.Revision,
                [new WeeklySchedulePeriodRequest(2, 9 * 60, 10 * 60)],
                "Employee-specific Tuesday")));
        replaceResponse.EnsureSuccessStatusCode();
        WeeklyScheduleResponse? replaced = await replaceResponse.Content
            .ReadFromJsonAsync<WeeklyScheduleResponse>();
        Assert.NotNull(replaced);

        using HttpResponseMessage deleteResponse = await client.SendAsync(AuthorizedWrite(
            HttpMethod.Delete,
            $"/api/v1/scheduling/working-hours/employees/{AtlasEmployeeId}?expectedRevision={replaced.Revision}",
            token));
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        using HttpResponseMessage restoredResponse = await client.SendAsync(Authorized(
            HttpMethod.Get,
            $"/api/v1/scheduling/working-hours/employees/{AtlasEmployeeId}",
            token));
        WeeklyScheduleResponse? restored = await restoredResponse.Content
            .ReadFromJsonAsync<WeeklyScheduleResponse>();
        Assert.NotNull(restored);
        Assert.Equal("inherited", restored.State);
        Assert.Equal("tenant", restored.Source);
        Assert.Equal(2, restored.Revision);
    }

    [Fact]
    public async Task WeeklyScheduleHistory_IsAppendOnlyAndRestorePublishesNewVersion()
    {
        using HttpClient client = CreateClient();
        string token = await LoginAsync(client, "owner@demo.local", AtlasTenantId);
        WeeklyScheduleResponse current = await GetWeeklyScheduleAsync(client, token);

        using HttpResponseMessage publishResponse = await client.SendAsync(AuthorizedWrite(
            HttpMethod.Put,
            "/api/v1/scheduling/working-hours/tenant",
            token,
            new PutWeeklyScheduleRequest(
                current.Revision,
                [new WeeklySchedulePeriodRequest(1, 8 * 60, 12 * 60)],
                "Morning-only program")));
        publishResponse.EnsureSuccessStatusCode();
        WeeklyScheduleResponse published = await ReadRequiredAsync<WeeklyScheduleResponse>(
            publishResponse);
        Assert.Equal(2, published.Revision);

        PagedResponse<WeeklyScheduleVersionResponse> history =
            await GetWeeklyScheduleHistoryAsync(client, token);
        Assert.Equal(2, history.TotalCount);
        WeeklyScheduleVersionResponse original = Assert.Single(
            history.Items,
            version => version.VersionNumber == 1);
        Assert.Equal(5, original.Periods.Count);
        Assert.Equal("custom", original.Mode);

        using HttpResponseMessage restoreResponse = await client.SendAsync(AuthorizedWrite(
            HttpMethod.Post,
            $"/api/v1/scheduling/working-hours/tenant/versions/{original.Id}/restore",
            token,
            new RestoreWeeklyScheduleVersionRequest(published.Revision, "Restore business hours")));
        restoreResponse.EnsureSuccessStatusCode();
        WeeklyScheduleResponse restored = await ReadRequiredAsync<WeeklyScheduleResponse>(
            restoreResponse);
        Assert.Equal(3, restored.Revision);
        Assert.Equal(5, restored.Periods.Count);

        history = await GetWeeklyScheduleHistoryAsync(client, token);
        WeeklyScheduleVersionResponse newest = Assert.Single(
            history.Items,
            version => version.VersionNumber == 3);
        Assert.Equal(original.Id, newest.RestoredFromVersionId);
        Assert.Equal(original.VersionNumber, newest.RestoredFromVersionNumber);
        Assert.Equal(5, original.Periods.Count);

        await using var connection = new NpgsqlConnection(_factory.ConnectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(
            "UPDATE weekly_schedule_versions SET change_note = 'mutated' WHERE id = @id",
            connection);
        command.Parameters.AddWithValue("id", original.Id);
        PostgresException exception = await Assert.ThrowsAsync<PostgresException>(
            () => command.ExecuteNonQueryAsync());
        Assert.Equal("55000", exception.SqlState);

        await using var periodCommand = new NpgsqlCommand(
            "DELETE FROM weekly_schedule_version_periods WHERE version_id = @versionId",
            connection);
        periodCommand.Parameters.AddWithValue("versionId", original.Id);
        exception = await Assert.ThrowsAsync<PostgresException>(
            () => periodCommand.ExecuteNonQueryAsync());
        Assert.Equal("55000", exception.SqlState);
    }

    [Fact]
    public async Task WeeklySchedulePublish_RejectsStaleParallelRevision()
    {
        using HttpClient client = CreateClient();
        string token = await LoginAsync(client, "manager@demo.local", AtlasTenantId);
        WeeklyScheduleResponse current = await GetWeeklyScheduleAsync(client, token);
        var request = new PutWeeklyScheduleRequest(
            current.Revision,
            [new WeeklySchedulePeriodRequest(1, 9 * 60, 12 * 60)],
            "Concurrent publish");

        Task<HttpResponseMessage> first = client.SendAsync(AuthorizedWrite(
            HttpMethod.Put,
            "/api/v1/scheduling/working-hours/tenant",
            token,
            request));
        Task<HttpResponseMessage> second = client.SendAsync(AuthorizedWrite(
            HttpMethod.Put,
            "/api/v1/scheduling/working-hours/tenant",
            token,
            request));
        HttpResponseMessage[] responses = await Task.WhenAll(first, second);
        using HttpResponseMessage success = Assert.Single(
            responses,
            response => response.StatusCode == HttpStatusCode.OK);
        using HttpResponseMessage conflict = Assert.Single(
            responses,
            response => response.StatusCode == HttpStatusCode.Conflict);
        await AssertProblemCodeAsync(conflict, "scheduling.schedule_version_conflict");
    }

    [Fact]
    public async Task WeeklyScheduleHistory_DoesNotRevealAnotherTenantVersion()
    {
        using HttpClient client = CreateClient();
        string atlasToken = await LoginAsync(client, "owner@demo.local", AtlasTenantId);
        PagedResponse<WeeklyScheduleVersionResponse> atlasHistory =
            await GetWeeklyScheduleHistoryAsync(client, atlasToken);
        Guid atlasVersionId = Assert.Single(atlasHistory.Items).Id;
        string northwindToken = await LoginAsync(client, "owner@demo.local", NorthwindTenantId);

        using HttpResponseMessage response = await client.SendAsync(Authorized(
            HttpMethod.Get,
            $"/api/v1/scheduling/working-hours/tenant/versions/{atlasVersionId}",
            northwindToken));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        await AssertProblemCodeAsync(response, "scheduling.schedule_version_not_found");
    }

    [Fact]
    public async Task SchedulingPermissions_SeparateManagementFromAvailabilityRead()
    {
        using HttpClient client = CreateClient();
        string token = await LoginAsync(client, "receptionist@demo.local", AtlasTenantId);

        using HttpResponseMessage forbidden = await client.SendAsync(Authorized(
            HttpMethod.Get,
            "/api/v1/scheduling/working-hours/tenant",
            token));
        Assert.Equal(HttpStatusCode.Forbidden, forbidden.StatusCode);

        using HttpResponseMessage availability = await client.SendAsync(Authorized(
            HttpMethod.Get,
            $"/api/v1/availability?date=2026-08-24&employeeId={AtlasEmployeeId}&serviceId={AtlasServiceId}",
            token));
        Assert.Equal(HttpStatusCode.OK, availability.StatusCode);
    }

    [Fact]
    public async Task SchedulingEndpoints_DoNotRevealAnotherTenantEmployee()
    {
        using HttpClient client = CreateClient();
        string token = await LoginAsync(client, "owner@demo.local", NorthwindTenantId);

        using HttpResponseMessage schedule = await client.SendAsync(Authorized(
            HttpMethod.Get,
            $"/api/v1/scheduling/working-hours/employees/{AtlasEmployeeId}",
            token));
        Assert.Equal(HttpStatusCode.NotFound, schedule.StatusCode);
        await AssertProblemCodeAsync(schedule, "scheduling.employee_not_found");

        using HttpResponseMessage availability = await client.SendAsync(Authorized(
            HttpMethod.Get,
            $"/api/v1/availability?date=2026-08-24&employeeId={AtlasEmployeeId}&serviceId={AtlasServiceId}",
            token));
        Assert.Equal(HttpStatusCode.NotFound, availability.StatusCode);
        await AssertProblemCodeAsync(availability, "scheduling.employee_not_found");
    }

    private HttpClient CreateClient() => _factory.CreateClient(new WebApplicationFactoryClientOptions
    {
        BaseAddress = new Uri("https://localhost"),
        HandleCookies = false,
    });

    private static async Task<AvailabilityResponse> GetAvailabilityAsync(
        HttpClient client,
        string token,
        DateOnly date)
    {
        using HttpResponseMessage response = await client.SendAsync(Authorized(
            HttpMethod.Get,
            $"/api/v1/availability?date={date:yyyy-MM-dd}&employeeId={AtlasEmployeeId}&serviceId={AtlasServiceId}",
            token));
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<AvailabilityResponse>()
            ?? throw new InvalidOperationException("Availability response was empty.");
    }

    private static async Task<WeeklyScheduleResponse> GetWeeklyScheduleAsync(
        HttpClient client,
        string token)
    {
        using HttpResponseMessage response = await client.SendAsync(Authorized(
            HttpMethod.Get,
            "/api/v1/scheduling/working-hours/tenant",
            token));
        response.EnsureSuccessStatusCode();
        return await ReadRequiredAsync<WeeklyScheduleResponse>(response);
    }

    private static async Task<PagedResponse<WeeklyScheduleVersionResponse>>
        GetWeeklyScheduleHistoryAsync(
            HttpClient client,
            string token)
    {
        using HttpResponseMessage response = await client.SendAsync(Authorized(
            HttpMethod.Get,
            "/api/v1/scheduling/working-hours/tenant/versions?page=1&pageSize=10",
            token));
        response.EnsureSuccessStatusCode();
        return await ReadRequiredAsync<PagedResponse<WeeklyScheduleVersionResponse>>(response);
    }

    private static async Task<T> ReadRequiredAsync<T>(HttpResponseMessage response) =>
        await response.Content.ReadFromJsonAsync<T>()
        ?? throw new InvalidOperationException("The API response body was empty.");

    private static TimeOnly LocalTime(DateTimeOffset value) =>
        TimeOnly.FromDateTime(value.DateTime);

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
        AuthenticationResponse? payload = await response.Content
            .ReadFromJsonAsync<AuthenticationResponse>();
        return payload?.AccessToken
            ?? throw new InvalidOperationException("Login response did not contain an access token.");
    }

    private static async Task AssertProblemCodeAsync(
        HttpResponseMessage response,
        string expectedCode)
    {
        using JsonDocument problem = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(expectedCode, problem.RootElement.GetProperty("code").GetString());
    }
}
