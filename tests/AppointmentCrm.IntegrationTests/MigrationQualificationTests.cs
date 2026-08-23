using AppointmentCrm.Infrastructure;
using AppointmentCrm.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.Configuration;
using Npgsql;

namespace AppointmentCrm.IntegrationTests;

public sealed class MigrationQualificationTests : IClassFixture<ApiFactory>
{
    private const string PreviousReleaseMigration = "20260823001133_AppointmentLifecycle";
    private readonly ApiFactory _factory;

    public MigrationQualificationTests(ApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task LatestMigrations_ApplyToAnEmptyDatabase()
    {
        await using TemporaryPostgresDatabase database = await TemporaryPostgresDatabase.CreateAsync(
            _factory.ConnectionString);
        await using AppointmentCrmDbContext dbContext = database.CreateContext();

        await dbContext.Database.MigrateAsync();

        string[] expected = dbContext.Database.GetMigrations().ToArray();
        string[] applied = (await dbContext.Database.GetAppliedMigrationsAsync()).ToArray();
        Assert.Equal(expected, applied);
        Assert.True(await TableExistsAsync(database.ConnectionString, "notification_deliveries"));
        Assert.True(await TableExistsAsync(database.ConnectionString, "appointments"));
    }

    [Fact]
    public async Task PreviousRelease_UpgradesForwardWithoutLosingOutboxData()
    {
        await using TemporaryPostgresDatabase database = await TemporaryPostgresDatabase.CreateAsync(
            _factory.ConnectionString);
        await using AppointmentCrmDbContext dbContext = database.CreateContext();
        IMigrator migrator = dbContext.Database.GetService<IMigrator>();
        await migrator.MigrateAsync(PreviousReleaseMigration);
        Guid tenantId = Guid.NewGuid();
        Guid messageId = Guid.NewGuid();
        await InsertPreviousReleaseOutboxAsync(database.ConnectionString, tenantId, messageId);

        await migrator.MigrateAsync();

        await using var connection = new NpgsqlConnection(database.ConnectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(
            """
            SELECT payload_json::text, failed_at_utc, lease_id, trace_parent
            FROM outbox_messages
            WHERE id = @messageId;
            """,
            connection);
        command.Parameters.AddWithValue("messageId", messageId);
        await using NpgsqlDataReader reader = await command.ExecuteReaderAsync();
        Assert.True(await reader.ReadAsync());
        Assert.Contains("appointmentId", reader.GetString(0), StringComparison.Ordinal);
        Assert.True(reader.IsDBNull(1));
        Assert.True(reader.IsDBNull(2));
        Assert.True(reader.IsDBNull(3));
        Assert.True(await TableExistsAsync(database.ConnectionString, "notification_deliveries"));
        Assert.Empty(await dbContext.Database.GetPendingMigrationsAsync());
    }

    [Fact]
    public async Task DemoSeed_IsIdempotentWhenEnabledAndEmptyWhenDisabled()
    {
        await using TemporaryPostgresDatabase enabledDatabase =
            await TemporaryPostgresDatabase.CreateAsync(_factory.ConnectionString);
        using (WebApplicationFactory<Program> enabledFactory = SeedFactory(
                   enabledDatabase.ConnectionString,
                   enabled: true))
        {
            await MigrationRunner.RunAsync(enabledFactory.Services);
            await MigrationRunner.RunAsync(enabledFactory.Services);
            Assert.Equal(
                new long[] { 2, 5, 6, 2, 2, 2, 2, 2 },
                await ReadSeedCountsAsync(enabledDatabase.ConnectionString));
        }

        await using TemporaryPostgresDatabase disabledDatabase =
            await TemporaryPostgresDatabase.CreateAsync(_factory.ConnectionString);
        using (WebApplicationFactory<Program> disabledFactory = SeedFactory(
                   disabledDatabase.ConnectionString,
                   enabled: false))
        {
            await MigrationRunner.RunAsync(disabledFactory.Services);
            await MigrationRunner.RunAsync(disabledFactory.Services);
            Assert.Equal(
                new long[] { 0, 0, 0, 0, 0, 0, 0, 0 },
                await ReadSeedCountsAsync(disabledDatabase.ConnectionString));
        }
    }

    private WebApplicationFactory<Program> SeedFactory(string connectionString, bool enabled) =>
        _factory.WithWebHostBuilder(builder => builder.ConfigureAppConfiguration(
            (_, configuration) => configuration.AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    ["ConnectionStrings:Postgres"] = connectionString,
                    ["DemoSeed:Enabled"] = enabled.ToString(),
                    ["DemoSeed:Password"] = enabled ? IdentitySecurityTests.DemoPassword : "",
                    ["Outbox:WorkerEnabled"] = "false",
                    ["DashboardCache:Enabled"] = "false",
                })));

    private static async Task InsertPreviousReleaseOutboxAsync(
        string connectionString,
        Guid tenantId,
        Guid messageId)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(
            """
            INSERT INTO tenants (
                id, name, slug, time_zone, currency, is_active, created_at_utc, updated_at_utc)
            VALUES (
                @tenantId, 'Upgrade tenant', @slug, 'Europe/Istanbul', 'TRY', true, now(), now());

            INSERT INTO outbox_messages (
                id, tenant_id, type, aggregate_type, aggregate_id, payload_json,
                occurred_at_utc, processed_at_utc, attempts, next_attempt_at_utc, last_error)
            VALUES (
                @messageId, @tenantId, 'appointment.created', 'appointment', @aggregateId,
                '{"appointmentId":"upgrade-fixture"}'::jsonb,
                now(), NULL, 0, NULL, NULL);
            """,
            connection);
        command.Parameters.AddWithValue("tenantId", tenantId);
        command.Parameters.AddWithValue("slug", $"upgrade-{tenantId:N}");
        command.Parameters.AddWithValue("messageId", messageId);
        command.Parameters.AddWithValue("aggregateId", Guid.NewGuid());
        Assert.Equal(2, await command.ExecuteNonQueryAsync());
    }

    private static async Task<bool> TableExistsAsync(string connectionString, string table)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(
            "SELECT to_regclass('public.' || @table) IS NOT NULL;",
            connection);
        command.Parameters.AddWithValue("table", table);
        return (bool)(await command.ExecuteScalarAsync())!;
    }

    private static async Task<long[]> ReadSeedCountsAsync(string connectionString)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(
            """
            SELECT
                (SELECT count(*) FROM tenants),
                (SELECT count(*) FROM users),
                (SELECT count(*) FROM tenant_memberships),
                (SELECT count(*) FROM customers),
                (SELECT count(*) FROM services),
                (SELECT count(*) FROM employees),
                (SELECT count(*) FROM employee_services),
                (SELECT count(*) FROM weekly_schedules);
            """,
            connection);
        await using NpgsqlDataReader reader = await command.ExecuteReaderAsync();
        Assert.True(await reader.ReadAsync());
        return Enumerable.Range(0, 8).Select(reader.GetInt64).ToArray();
    }

    private sealed class TemporaryPostgresDatabase : IAsyncDisposable
    {
        private const string NamePrefix = "appointment_crm_phase8_";
        private readonly string _adminConnectionString;

        private TemporaryPostgresDatabase(
            string databaseName,
            string adminConnectionString,
            string connectionString)
        {
            DatabaseName = databaseName;
            _adminConnectionString = adminConnectionString;
            ConnectionString = connectionString;
        }

        public string DatabaseName { get; }

        public string ConnectionString { get; }

        public static async Task<TemporaryPostgresDatabase> CreateAsync(
            string baseConnectionString)
        {
            string databaseName = $"{NamePrefix}{Guid.NewGuid():N}";
            var adminBuilder = new NpgsqlConnectionStringBuilder(baseConnectionString)
            {
                Database = "postgres",
                Pooling = false,
            };
            var databaseBuilder = new NpgsqlConnectionStringBuilder(baseConnectionString)
            {
                Database = databaseName,
                Pooling = false,
            };
            await using var connection = new NpgsqlConnection(adminBuilder.ConnectionString);
            await connection.OpenAsync();
            await using var command = new NpgsqlCommand(
                $"CREATE DATABASE \"{databaseName}\";",
                connection);
            await command.ExecuteNonQueryAsync();
            return new TemporaryPostgresDatabase(
                databaseName,
                adminBuilder.ConnectionString,
                databaseBuilder.ConnectionString);
        }

        public AppointmentCrmDbContext CreateContext()
        {
            var options = new DbContextOptionsBuilder<AppointmentCrmDbContext>()
                .UseNpgsql(
                    ConnectionString,
                    npgsql => npgsql.MigrationsAssembly(
                        typeof(AppointmentCrmDbContext).Assembly.FullName))
                .Options;
            return new AppointmentCrmDbContext(options);
        }

        public async ValueTask DisposeAsync()
        {
            if (!DatabaseName.StartsWith(NamePrefix, StringComparison.Ordinal)
                || DatabaseName.Length != NamePrefix.Length + 32)
            {
                throw new InvalidOperationException("Refusing to drop an unexpected database.");
            }

            await using var connection = new NpgsqlConnection(_adminConnectionString);
            await connection.OpenAsync();
            await using var command = new NpgsqlCommand(
                $"DROP DATABASE IF EXISTS \"{DatabaseName}\" WITH (FORCE);",
                connection);
            await command.ExecuteNonQueryAsync();
        }
    }
}
