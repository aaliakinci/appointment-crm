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
        await dbContext.Database.MigrateAsync(cancellationToken);
        var demoDataSeeder = scope.ServiceProvider.GetRequiredService<DemoDataSeeder>();
        await demoDataSeeder.SeedAsync(cancellationToken);
    }
}
