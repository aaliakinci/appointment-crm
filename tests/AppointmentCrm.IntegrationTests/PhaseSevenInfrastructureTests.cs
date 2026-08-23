using System.Collections.Concurrent;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using AppointmentCrm.Application.Reporting;
using AppointmentCrm.Contracts;
using AppointmentCrm.Domain.Outbox;
using AppointmentCrm.Infrastructure;
using AppointmentCrm.Infrastructure.Outbox;
using AppointmentCrm.Infrastructure.Persistence;
using AppointmentCrm.Infrastructure.Reporting;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace AppointmentCrm.IntegrationTests;

public sealed class PhaseSevenInfrastructureTests : IClassFixture<ApiFactory>, IAsyncLifetime
{
    private const string DemoPassword = IdentitySecurityTests.DemoPassword;
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

    private readonly ApiFactory _factory;

    public PhaseSevenInfrastructureTests(ApiFactory factory)
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
    public async Task AppointmentOutbox_PreservesTraceAndCreatesOneLogicalDelivery()
    {
        using HttpClient client = CreateClient(_factory);
        string token = await LoginAsync(client, "manager@demo.local", AtlasTenantId);
        using HttpResponseMessage createResponse = await client.SendAsync(AuthorizedWrite(
            HttpMethod.Post,
            "/api/v1/appointments",
            token,
            new CreateAppointmentRequest(
                AtlasCustomerId,
                AtlasEmployeeId,
                AtlasServiceId,
                NextBookingStartUtc(),
                "Traceable appointment")));
        createResponse.EnsureSuccessStatusCode();
        AppointmentResponse appointment = await ReadRequiredAsync<AppointmentResponse>(
            createResponse);
        string correlationId = createResponse.Headers.GetValues("X-Correlation-ID").Single();
        OutboxRow outbox = await ReadOutboxAsync(appointment.Appointment.Id);

        Assert.False(string.IsNullOrWhiteSpace(outbox.TraceParent));
        Assert.Equal(correlationId, outbox.CorrelationId);

        OutboxProcessor processor = _factory.Services.GetRequiredService<OutboxProcessor>();
        OutboxBatchResult first = await processor.ProcessBatchAsync(CancellationToken.None);

        Assert.Equal(1, first.Processed);
        DeliveryRow delivery = await ReadDeliveryAsync(outbox.Id);
        Assert.Equal(correlationId, delivery.CorrelationId);
        Assert.NotNull(delivery.TraceId);
        Assert.Contains(delivery.TraceId, outbox.TraceParent!, StringComparison.Ordinal);

        await ResetOutboxForDuplicateAsync(outbox.Id);
        OutboxBatchResult duplicate = await processor.ProcessBatchAsync(CancellationToken.None);

        Assert.Equal(1, duplicate.Processed);
        Assert.Equal(1L, await CountDeliveriesAsync(outbox.Id));
    }

    [Fact]
    public async Task OutboxFailures_BackoffThenRecordSanitizedTerminalFailure()
    {
        using WebApplicationFactory<Program> factory = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration((_, configuration) =>
                configuration.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Outbox:MaximumAttempts"] = "3",
                    ["Outbox:BaseRetryDelaySeconds"] = "1",
                    ["Outbox:MaximumRetryDelaySeconds"] = "1",
                    ["Outbox:WorkerEnabled"] = "false",
                }));
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<INotificationProvider>();
                services.AddSingleton<INotificationProvider, FailingNotificationProvider>();
            });
        });
        await MigrationRunner.RunAsync(factory.Services);
        Guid messageId = await InsertOutboxAsync();
        OutboxProcessor processor = factory.Services.GetRequiredService<OutboxProcessor>();

        for (int attempt = 0; attempt < 3; attempt++)
        {
            OutboxBatchResult result = await processor.ProcessBatchAsync(CancellationToken.None);
            Assert.Equal(1, result.Failed);
            if (attempt < 2)
            {
                await MakeOutboxDueAsync(messageId);
            }
        }

        FailedOutboxRow failed = await ReadFailedOutboxAsync(messageId);
        Assert.Equal(3, failed.Attempts);
        Assert.NotNull(failed.FailedAtUtc);
        Assert.Equal(typeof(InvalidOperationException).FullName, failed.LastError);
        Assert.DoesNotContain("password", failed.LastError!, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("token", failed.LastError!, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(0L, await CountDeliveriesAsync(messageId));
    }

    [Fact]
    public async Task DashboardCache_UsesSeparateRedisKeysForTwoTenants()
    {
        string prefix = $"appointment-crm:test:{Guid.NewGuid():N}:";
        using WebApplicationFactory<Program> factory = CacheFactory(
            _factory.RedisConnectionString,
            prefix);
        await MigrationRunner.RunAsync(factory.Services);
        using HttpClient client = CreateClient(factory);
        DateOnly today = TenantToday();
        var filter = new ReportingFilter(today, today, null, null);

        await GetDashboardAsync(
            client,
            await LoginAsync(client, "owner@demo.local", AtlasTenantId),
            today);
        await GetDashboardAsync(
            client,
            await LoginAsync(client, "north.owner@demo.local", NorthwindTenantId),
            today);

        var cache = factory.Services.GetRequiredService<IDistributedCache>();
        string atlasKey = DashboardCacheKey.Create(prefix, AtlasTenantId, filter);
        string northwindKey = DashboardCacheKey.Create(prefix, NorthwindTenantId, filter);
        Assert.NotEqual(atlasKey, northwindKey);
        Assert.NotNull(await cache.GetAsync(atlasKey));
        Assert.NotNull(await cache.GetAsync(northwindKey));
    }

    [Fact]
    public async Task RedisUnavailable_PreservesDashboardCorrectnessAndReadiness()
    {
        using WebApplicationFactory<Program> factory = CacheFactory(
            "127.0.0.1:1,abortConnect=false,connectRetry=0,connectTimeout=100,asyncTimeout=200,syncTimeout=200",
            $"appointment-crm:unavailable:{Guid.NewGuid():N}:");
        await MigrationRunner.RunAsync(factory.Services);
        using HttpClient client = CreateClient(factory);
        DateOnly today = TenantToday();
        string token = await LoginAsync(client, "owner@demo.local", AtlasTenantId);

        ReportingDashboardResponse dashboard = await GetDashboardAsync(client, token, today);
        Assert.Equal(today, dashboard.FromDate);
        Assert.Equal(today, dashboard.ToDate);

        using HttpResponseMessage readiness = await client.GetAsync("/health/ready");
        readiness.EnsureSuccessStatusCode();
        using HttpResponseMessage dependencies = await client.GetAsync("/health/dependencies");
        Assert.Equal(HttpStatusCode.ServiceUnavailable, dependencies.StatusCode);
    }

    [Fact]
    public async Task StructuredLogs_DoNotContainCredentialsOrIssuedTokens()
    {
        using var logProvider = new CapturingLoggerProvider();
        using WebApplicationFactory<Program> factory = _factory.WithWebHostBuilder(builder =>
            builder.ConfigureLogging(logging => logging.AddProvider(logProvider)));
        await MigrationRunner.RunAsync(factory.Services);
        using HttpClient client = CreateClient(factory);

        string accessToken = await LoginAsync(client, "owner@demo.local", AtlasTenantId);
        string logs = string.Join('\n', logProvider.Messages);

        Assert.DoesNotContain(DemoPassword, logs, StringComparison.Ordinal);
        Assert.DoesNotContain(accessToken, logs, StringComparison.Ordinal);
        Assert.DoesNotContain("refresh_token", logs, StringComparison.OrdinalIgnoreCase);
    }

    private WebApplicationFactory<Program> CacheFactory(string connectionString, string prefix) =>
        _factory.WithWebHostBuilder(builder => builder.ConfigureAppConfiguration(
            (_, configuration) => configuration.AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    ["ConnectionStrings:Redis"] = connectionString,
                    ["DashboardCache:Enabled"] = "true",
                    ["DashboardCache:TimeToLiveSeconds"] = "30",
                    ["DashboardCache:KeyPrefix"] = prefix,
                    ["Outbox:WorkerEnabled"] = "false",
                })));

    private static HttpClient CreateClient(WebApplicationFactory<Program> factory) =>
        factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost"),
            AllowAutoRedirect = false,
        });

    private static async Task<string> LoginAsync(
        HttpClient client,
        string email,
        Guid tenantId)
    {
        using HttpResponseMessage response = await client.PostAsJsonAsync(
            "/api/v1/auth/login",
            new LoginRequest(email, DemoPassword, tenantId));
        response.EnsureSuccessStatusCode();
        AuthenticationResponse authentication = await ReadRequiredAsync<AuthenticationResponse>(
            response);
        return authentication.AccessToken
            ?? throw new InvalidOperationException("Login did not return an access token.");
    }

    private static async Task<ReportingDashboardResponse> GetDashboardAsync(
        HttpClient client,
        string token,
        DateOnly date)
    {
        using HttpResponseMessage response = await client.SendAsync(Authorized(
            HttpMethod.Get,
            $"/api/v1/reporting/dashboard?fromDate={date:yyyy-MM-dd}&toDate={date:yyyy-MM-dd}",
            token));
        response.EnsureSuccessStatusCode();
        return await ReadRequiredAsync<ReportingDashboardResponse>(response);
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

    private static async Task<T> ReadRequiredAsync<T>(HttpResponseMessage response) =>
        await response.Content.ReadFromJsonAsync<T>()
        ?? throw new InvalidOperationException($"Response did not contain {typeof(T).Name}.");

    private static DateTimeOffset NextBookingStartUtc()
    {
        TimeZoneInfo timeZone = TimeZoneInfo.FindSystemTimeZoneById("Europe/Istanbul");
        DateOnly date = TenantToday().AddDays(1);
        while (date.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday)
        {
            date = date.AddDays(1);
        }

        DateTime local = DateTime.SpecifyKind(
            date.ToDateTime(new TimeOnly(10, 0)),
            DateTimeKind.Unspecified);
        return new DateTimeOffset(TimeZoneInfo.ConvertTimeToUtc(local, timeZone), TimeSpan.Zero);
    }

    private static DateOnly TenantToday()
    {
        TimeZoneInfo timeZone = TimeZoneInfo.FindSystemTimeZoneById("Europe/Istanbul");
        return DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, timeZone).DateTime);
    }

    private async Task<OutboxRow> ReadOutboxAsync(Guid aggregateId)
    {
        await using var connection = new NpgsqlConnection(_factory.ConnectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(
            """
            SELECT id, trace_parent, correlation_id
            FROM outbox_messages
            WHERE aggregate_id = @aggregateId;
            """,
            connection);
        command.Parameters.AddWithValue("aggregateId", aggregateId);
        await using NpgsqlDataReader reader = await command.ExecuteReaderAsync();
        Assert.True(await reader.ReadAsync());
        return new OutboxRow(
            reader.GetGuid(0),
            reader.IsDBNull(1) ? null : reader.GetString(1),
            reader.IsDBNull(2) ? null : reader.GetString(2));
    }

    private async Task<DeliveryRow> ReadDeliveryAsync(Guid outboxMessageId)
    {
        await using var connection = new NpgsqlConnection(_factory.ConnectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(
            """
            SELECT trace_id, correlation_id
            FROM notification_deliveries
            WHERE outbox_message_id = @outboxMessageId;
            """,
            connection);
        command.Parameters.AddWithValue("outboxMessageId", outboxMessageId);
        await using NpgsqlDataReader reader = await command.ExecuteReaderAsync();
        Assert.True(await reader.ReadAsync());
        return new DeliveryRow(
            reader.IsDBNull(0) ? null : reader.GetString(0),
            reader.IsDBNull(1) ? null : reader.GetString(1));
    }

    private async Task ResetOutboxForDuplicateAsync(Guid messageId)
    {
        await using var connection = new NpgsqlConnection(_factory.ConnectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(
            """
            UPDATE outbox_messages
            SET processed_at_utc = NULL,
                attempts = 0,
                next_attempt_at_utc = NULL,
                last_error = NULL,
                failed_at_utc = NULL,
                lease_id = NULL,
                locked_until_utc = NULL
            WHERE id = @messageId;
            """,
            connection);
        command.Parameters.AddWithValue("messageId", messageId);
        Assert.Equal(1, await command.ExecuteNonQueryAsync());
    }

    private async Task<Guid> InsertOutboxAsync()
    {
        Guid messageId = Guid.NewGuid();
        await using var connection = new NpgsqlConnection(_factory.ConnectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(
            """
            INSERT INTO outbox_messages (
                id, tenant_id, type, aggregate_type, aggregate_id, payload_json,
                occurred_at_utc, processed_at_utc, attempts, next_attempt_at_utc,
                last_error, failed_at_utc, lease_id, locked_until_utc,
                trace_parent, trace_state, correlation_id)
            VALUES (
                @id, @tenantId, 'appointment.created', 'appointment', @aggregateId,
                '{"appointmentId":"redacted"}'::jsonb, now(), NULL, 0, NULL,
                NULL, NULL, NULL, NULL,
                '00-0123456789abcdef0123456789abcdef-0123456789abcdef-01', NULL,
                'phase-seven-retry');
            """,
            connection);
        command.Parameters.AddWithValue("id", messageId);
        command.Parameters.AddWithValue("tenantId", AtlasTenantId);
        command.Parameters.AddWithValue("aggregateId", Guid.NewGuid());
        Assert.Equal(1, await command.ExecuteNonQueryAsync());
        return messageId;
    }

    private async Task MakeOutboxDueAsync(Guid messageId)
    {
        await using var connection = new NpgsqlConnection(_factory.ConnectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(
            """
            UPDATE outbox_messages
            SET next_attempt_at_utc = now() - interval '1 second'
            WHERE id = @messageId;
            """,
            connection);
        command.Parameters.AddWithValue("messageId", messageId);
        Assert.Equal(1, await command.ExecuteNonQueryAsync());
    }

    private async Task<FailedOutboxRow> ReadFailedOutboxAsync(Guid messageId)
    {
        await using var connection = new NpgsqlConnection(_factory.ConnectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(
            """
            SELECT attempts, failed_at_utc, last_error
            FROM outbox_messages
            WHERE id = @messageId;
            """,
            connection);
        command.Parameters.AddWithValue("messageId", messageId);
        await using NpgsqlDataReader reader = await command.ExecuteReaderAsync();
        Assert.True(await reader.ReadAsync());
        return new FailedOutboxRow(
            reader.GetInt32(0),
            reader.IsDBNull(1) ? null : reader.GetFieldValue<DateTimeOffset>(1),
            reader.IsDBNull(2) ? null : reader.GetString(2));
    }

    private async Task<long> CountDeliveriesAsync(Guid messageId)
    {
        await using var connection = new NpgsqlConnection(_factory.ConnectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(
            "SELECT count(*) FROM notification_deliveries WHERE outbox_message_id = @messageId;",
            connection);
        command.Parameters.AddWithValue("messageId", messageId);
        return (long)(await command.ExecuteScalarAsync())!;
    }

    private sealed class FailingNotificationProvider : INotificationProvider
    {
        public ValueTask<NotificationDelivery> DeliverAsync(
            OutboxMessage message,
            CancellationToken cancellationToken) =>
            throw new InvalidOperationException(
                "password=never-store; access_token=never-store");
    }

    private sealed record OutboxRow(Guid Id, string? TraceParent, string? CorrelationId);

    private sealed record DeliveryRow(string? TraceId, string? CorrelationId);

    private sealed record FailedOutboxRow(
        int Attempts,
        DateTimeOffset? FailedAtUtc,
        string? LastError);

    private sealed class CapturingLoggerProvider : ILoggerProvider
    {
        public ConcurrentQueue<string> Messages { get; } = new();

        public ILogger CreateLogger(string categoryName) => new CapturingLogger(Messages);

        public void Dispose()
        {
        }
    }

    private sealed class CapturingLogger(ConcurrentQueue<string> messages) : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter) =>
            messages.Enqueue(formatter(state, exception));
    }
}
