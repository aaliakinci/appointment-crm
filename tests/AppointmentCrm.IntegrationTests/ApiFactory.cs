using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace AppointmentCrm.IntegrationTests;

public sealed class ApiFactory : WebApplicationFactory<Program>
{
    public const string DefaultConnectionString =
        "Host=127.0.0.1;Port=5432;Database=appointment_crm;Username=appointment_crm;Password=change-me-local-only";

    public string ConnectionString { get; } =
        Environment.GetEnvironmentVariable("APPOINTMENTCRM_TEST_POSTGRES")
        ?? DefaultConnectionString;

    public string RedisConnectionString { get; } =
        Environment.GetEnvironmentVariable("APPOINTMENTCRM_TEST_REDIS")
        ?? "127.0.0.1:6379,abortConnect=false,connectTimeout=1000,syncTimeout=1000";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureAppConfiguration((_, configuration) =>
        {
            configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Postgres"] = ConnectionString,
                ["DemoSeed:Enabled"] = "true",
                ["DemoSeed:Password"] = IdentitySecurityTests.DemoPassword,
                ["Identity:RequireSecureCookie"] = "true",
            });
        });
    }
}
