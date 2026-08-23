using AppointmentCrm.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace AppointmentCrm.Infrastructure.Identity;

internal sealed class DemoDataResetter(
    AppointmentCrmDbContext dbContext,
    DemoDataSeeder demoDataSeeder,
    IOptions<DemoSeedOptions> options)
{
    public async Task ResetAsync(CancellationToken cancellationToken)
    {
        if (!options.Value.Enabled
            || !options.Value.PublicMode
            || !options.Value.ResetEnabled)
        {
            throw new InvalidOperationException(
                "Demo reset requires DemoSeed:Enabled, PublicMode, and ResetEnabled.");
        }

        var executionStrategy = dbContext.Database.CreateExecutionStrategy();
        await executionStrategy.ExecuteAsync(async () =>
        {
            await using var transaction = await dbContext.Database.BeginTransactionAsync(
                cancellationToken);
            Guid tenantId = DemoDataSeeder.AtlasTenantId;
            const string lockResource = "appointment-crm:demo-reset";
            _ = await dbContext.Database.ExecuteSqlInterpolatedAsync(
                $"SELECT pg_advisory_xact_lock(hashtextextended({lockResource}, 0));",
                cancellationToken);
            _ = await dbContext.Database.ExecuteSqlInterpolatedAsync(
                $"SELECT set_config('appointment_crm.demo_reset_tenant', {tenantId.ToString()}, true);",
                cancellationToken);
            _ = await dbContext.Database.ExecuteSqlInterpolatedAsync($$"""
                DELETE FROM notification_deliveries WHERE tenant_id = {{tenantId}};
                DELETE FROM appointment_status_history WHERE tenant_id = {{tenantId}};
                DELETE FROM appointments WHERE tenant_id = {{tenantId}};
                DELETE FROM audit_entries WHERE tenant_id = {{tenantId}};
                DELETE FROM outbox_messages WHERE tenant_id = {{tenantId}};
                DELETE FROM user_sessions WHERE tenant_id = {{tenantId}};
                DELETE FROM date_schedule_override_periods WHERE tenant_id = {{tenantId}};
                DELETE FROM date_schedule_overrides WHERE tenant_id = {{tenantId}};
                DELETE FROM employee_time_offs WHERE tenant_id = {{tenantId}};
                DELETE FROM weekly_schedules WHERE tenant_id = {{tenantId}};
                DELETE FROM employee_services WHERE tenant_id = {{tenantId}};
                DELETE FROM employees WHERE tenant_id = {{tenantId}};
                DELETE FROM services WHERE tenant_id = {{tenantId}};
                DELETE FROM customers WHERE tenant_id = {{tenantId}};
                DELETE FROM tenant_memberships WHERE tenant_id = {{tenantId}};
                DELETE FROM tenants WHERE id = {{tenantId}};
                DELETE FROM users
                WHERE id IN (
                    {{DemoDataSeeder.OwnerUserId}},
                    {{DemoDataSeeder.ManagerUserId}},
                    {{DemoDataSeeder.ReceptionistUserId}},
                    {{DemoDataSeeder.EmployeeUserId}})
                  AND NOT EXISTS (
                    SELECT 1 FROM tenant_memberships membership
                    WHERE membership.user_id = users.id);
                """, cancellationToken);

            dbContext.ChangeTracker.Clear();
            await demoDataSeeder.SeedAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        });
    }
}
