using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace AppointmentCrm.Infrastructure.Persistence;

public sealed class AppointmentCrmDesignTimeDbContextFactory
    : IDesignTimeDbContextFactory<AppointmentCrmDbContext>
{
    private const string LocalConnectionString =
        "Host=127.0.0.1;Port=5432;Database=appointment_crm;Username=appointment_crm;Password=change-me-local-only";

    public AppointmentCrmDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__Postgres")
            ?? LocalConnectionString;
        var options = new DbContextOptionsBuilder<AppointmentCrmDbContext>()
            .UseNpgsql(
                connectionString,
                npgsql => npgsql.MigrationsAssembly(typeof(AppointmentCrmDbContext).Assembly.FullName))
            .Options;

        return new AppointmentCrmDbContext(options);
    }
}
