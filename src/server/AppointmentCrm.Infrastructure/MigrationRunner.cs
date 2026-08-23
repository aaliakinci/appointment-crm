using AppointmentCrm.Infrastructure.Identity;
using AppointmentCrm.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace AppointmentCrm.Infrastructure;

public static class MigrationRunner
{
    public static async Task RunAsync(
        IServiceProvider services,
        CancellationToken cancellationToken = default)
    {
        await using var scope = services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppointmentCrmDbContext>();
        await dbContext.Database.OpenConnectionAsync(cancellationToken);
        try
        {
            const string migrationLock = "appointment-crm:migration";
            _ = await dbContext.Database.ExecuteSqlInterpolatedAsync(
                $"SELECT pg_advisory_lock(hashtextextended({migrationLock}, 0));",
                cancellationToken);
            await dbContext.Database.MigrateAsync(cancellationToken);
            var demoDataSeeder = scope.ServiceProvider.GetRequiredService<DemoDataSeeder>();
            await demoDataSeeder.SeedAsync(cancellationToken);
        }
        finally
        {
            if (dbContext.Database.GetDbConnection().State == System.Data.ConnectionState.Open)
            {
                const string migrationLock = "appointment-crm:migration";
                _ = await dbContext.Database.ExecuteSqlInterpolatedAsync(
                    $"SELECT pg_advisory_unlock(hashtextextended({migrationLock}, 0));",
                    CancellationToken.None);
                await dbContext.Database.CloseConnectionAsync();
            }
        }
    }

    public static async Task ResetDemoAsync(
        IServiceProvider services,
        CancellationToken cancellationToken = default)
    {
        await using var scope = services.CreateAsyncScope();
        var resetter = scope.ServiceProvider.GetRequiredService<DemoDataResetter>();
        await resetter.ResetAsync(cancellationToken);
    }
}
