using AppointmentCrm.Domain.Identity;
using AppointmentCrm.Infrastructure.Persistence;
using AppointmentCrm.Infrastructure.Tenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace AppointmentCrm.Infrastructure.Identity;

internal sealed class DemoDataSeeder(
    AppointmentCrmDbContext dbContext,
    TenantContext tenantContext,
    PasswordHashService passwordHashService,
    IOptions<DemoSeedOptions> options,
    TimeProvider timeProvider)
{
    public static readonly Guid AtlasTenantId = Guid.Parse("10000000-0000-0000-0000-000000000001");
    public static readonly Guid NorthwindTenantId = Guid.Parse("10000000-0000-0000-0000-000000000002");
    public static readonly Guid OwnerUserId = Guid.Parse("20000000-0000-0000-0000-000000000001");
    public static readonly Guid ManagerUserId = Guid.Parse("20000000-0000-0000-0000-000000000002");
    public static readonly Guid ReceptionistUserId = Guid.Parse("20000000-0000-0000-0000-000000000003");
    public static readonly Guid EmployeeUserId = Guid.Parse("20000000-0000-0000-0000-000000000004");
    public static readonly Guid NorthwindOwnerUserId = Guid.Parse("20000000-0000-0000-0000-000000000005");

    public async Task SeedAsync(CancellationToken cancellationToken)
    {
        if (!options.Value.Enabled)
        {
            return;
        }

        var now = timeProvider.GetUtcNow();
        var tenants = new[]
        {
            Tenant.Create(
                AtlasTenantId,
                "Atlas Salon",
                "atlas-salon",
                "Europe/Istanbul",
                "TRY",
                now),
            Tenant.Create(
                NorthwindTenantId,
                "Northwind Consulting",
                "northwind-consulting",
                "Europe/Istanbul",
                "TRY",
                now),
        };
        foreach (Tenant tenant in tenants)
        {
            if (!await dbContext.Tenants.IgnoreQueryFilters().AnyAsync(
                candidate => candidate.Id == tenant.Id,
                cancellationToken))
            {
                dbContext.Tenants.Add(tenant);
            }
        }

        var users = new[]
        {
            CreateUser(OwnerUserId, "owner@demo.local", "Demo Owner", now),
            CreateUser(ManagerUserId, "manager@demo.local", "Demo Manager", now),
            CreateUser(ReceptionistUserId, "receptionist@demo.local", "Demo Receptionist", now),
            CreateUser(EmployeeUserId, "employee@demo.local", "Demo Employee", now),
            CreateUser(NorthwindOwnerUserId, "north.owner@demo.local", "Northwind Owner", now),
        };
        foreach (User user in users)
        {
            if (!await dbContext.Users.AnyAsync(
                candidate => candidate.Id == user.Id,
                cancellationToken))
            {
                dbContext.Users.Add(user);
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        var memberships = new[]
        {
            Membership(
                "30000000-0000-0000-0000-000000000001",
                AtlasTenantId,
                OwnerUserId,
                TenantRoles.Owner,
                now),
            Membership(
                "30000000-0000-0000-0000-000000000002",
                NorthwindTenantId,
                OwnerUserId,
                TenantRoles.Owner,
                now),
            Membership(
                "30000000-0000-0000-0000-000000000003",
                AtlasTenantId,
                ManagerUserId,
                TenantRoles.Manager,
                now),
            Membership(
                "30000000-0000-0000-0000-000000000004",
                AtlasTenantId,
                ReceptionistUserId,
                TenantRoles.Receptionist,
                now),
            Membership(
                "30000000-0000-0000-0000-000000000005",
                AtlasTenantId,
                EmployeeUserId,
                TenantRoles.Employee,
                now),
            Membership(
                "30000000-0000-0000-0000-000000000006",
                NorthwindTenantId,
                NorthwindOwnerUserId,
                TenantRoles.Owner,
                now),
        };
        foreach (TenantMembership membership in memberships)
        {
            bool exists = await dbContext.TenantMemberships
                .IgnoreQueryFilters()
                .AnyAsync(candidate => candidate.Id == membership.Id, cancellationToken);
            if (exists)
            {
                continue;
            }

            tenantContext.SetTenant(membership.TenantId);
            dbContext.TenantMemberships.Add(membership);
            await dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    private User CreateUser(
        Guid id,
        string email,
        string displayName,
        DateTimeOffset now) =>
        User.Create(id, email, displayName, passwordHashService.Hash(options.Value.Password), now);

    private static TenantMembership Membership(
        string id,
        Guid tenantId,
        Guid userId,
        string role,
        DateTimeOffset now) =>
        TenantMembership.Create(Guid.Parse(id), tenantId, userId, role, now);
}
