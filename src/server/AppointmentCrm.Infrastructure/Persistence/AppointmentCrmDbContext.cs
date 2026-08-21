using AppointmentCrm.Application.Identity;
using AppointmentCrm.Application.Tenancy;
using AppointmentCrm.Domain.Common;
using AppointmentCrm.Domain.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace AppointmentCrm.Infrastructure.Persistence;

public sealed class AppointmentCrmDbContext : DbContext
{
    private readonly ITenantContext _tenantContext;

    public AppointmentCrmDbContext(
        DbContextOptions<AppointmentCrmDbContext> options,
        ITenantContext? tenantContext = null)
        : base(options)
    {
        _tenantContext = tenantContext ?? UnavailableTenantContext.Instance;
    }

    public DbSet<User> Users => Set<User>();

    public DbSet<Tenant> Tenants => Set<Tenant>();

    public DbSet<TenantMembership> TenantMemberships => Set<TenantMembership>();

    public DbSet<UserSession> UserSessions => Set<UserSession>();

    public DbSet<RoleDefinition> Roles => Set<RoleDefinition>();

    public DbSet<PermissionDefinition> Permissions => Set<PermissionDefinition>();

    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        GuardTenantWrites();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(
        bool acceptAllChangesOnSuccess,
        CancellationToken cancellationToken = default)
    {
        GuardTenantWrites();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ConfigureUsers(modelBuilder);
        ConfigureTenants(modelBuilder);
        ConfigureMemberships(modelBuilder);
        ConfigureSessions(modelBuilder);
        ConfigureAuthorizationDefinitions(modelBuilder);
    }

    private void ConfigureUsers(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("users");
            entity.HasKey(user => user.Id);
            entity.Property(user => user.Id).HasColumnName("id");
            entity.Property(user => user.Email).HasColumnName("email").HasMaxLength(320);
            entity.Property(user => user.NormalizedEmail)
                .HasColumnName("normalized_email")
                .HasMaxLength(320);
            entity.Property(user => user.DisplayName)
                .HasColumnName("display_name")
                .HasMaxLength(160);
            entity.Property(user => user.PasswordHash)
                .HasColumnName("password_hash")
                .HasMaxLength(512);
            entity.Property(user => user.IsActive).HasColumnName("is_active");
            entity.Property(user => user.SecurityVersion).HasColumnName("security_version");
            entity.Property(user => user.CreatedAtUtc).HasColumnName("created_at_utc");
            entity.Property(user => user.UpdatedAtUtc).HasColumnName("updated_at_utc");
            entity.HasIndex(user => user.NormalizedEmail)
                .IsUnique()
                .HasDatabaseName("ux_users_normalized_email");
        });
    }

    private void ConfigureTenants(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Tenant>(entity =>
        {
            entity.ToTable("tenants");
            entity.HasKey(tenant => tenant.Id);
            entity.Property(tenant => tenant.Id).HasColumnName("id");
            entity.Property(tenant => tenant.Name).HasColumnName("name").HasMaxLength(160);
            entity.Property(tenant => tenant.Slug).HasColumnName("slug").HasMaxLength(80);
            entity.Property(tenant => tenant.TimeZone)
                .HasColumnName("time_zone")
                .HasMaxLength(100);
            entity.Property(tenant => tenant.Currency)
                .HasColumnName("currency")
                .HasMaxLength(3);
            entity.Property(tenant => tenant.IsActive).HasColumnName("is_active");
            entity.Property(tenant => tenant.CreatedAtUtc).HasColumnName("created_at_utc");
            entity.Property(tenant => tenant.UpdatedAtUtc).HasColumnName("updated_at_utc");
            entity.HasIndex(tenant => tenant.Slug)
                .IsUnique()
                .HasDatabaseName("ux_tenants_slug");
            entity.HasQueryFilter(
                tenant => _tenantContext.IsAvailable && tenant.Id == _tenantContext.TenantId);
        });
    }

    private void ConfigureMemberships(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<TenantMembership>(entity =>
        {
            entity.ToTable(
                "tenant_memberships",
                table => table.HasCheckConstraint(
                    "ck_tenant_memberships_role",
                    "role IN ('Owner', 'Manager', 'Receptionist', 'Employee')"));
            entity.HasKey(membership => membership.Id);
            entity.HasAlternateKey(membership => new
            {
                membership.TenantId,
                membership.Id,
                membership.UserId,
            });
            entity.Property(membership => membership.Id).HasColumnName("id");
            entity.Property(membership => membership.TenantId).HasColumnName("tenant_id");
            entity.Property(membership => membership.UserId).HasColumnName("user_id");
            entity.Property(membership => membership.Role).HasColumnName("role").HasMaxLength(32);
            entity.Property(membership => membership.IsActive).HasColumnName("is_active");
            entity.Property(membership => membership.AuthorizationVersion)
                .HasColumnName("authorization_version");
            entity.Property(membership => membership.CreatedAtUtc)
                .HasColumnName("created_at_utc");
            entity.Property(membership => membership.UpdatedAtUtc)
                .HasColumnName("updated_at_utc");
            entity.HasIndex(membership => new { membership.TenantId, membership.UserId })
                .IsUnique()
                .HasDatabaseName("ux_tenant_memberships_tenant_user");
            entity.HasIndex(membership => new { membership.TenantId, membership.Role })
                .HasDatabaseName("ix_tenant_memberships_tenant_role");
            entity.HasOne(membership => membership.Tenant)
                .WithMany(tenant => tenant.Memberships)
                .HasForeignKey(membership => membership.TenantId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(membership => membership.User)
                .WithMany(user => user.Memberships)
                .HasForeignKey(membership => membership.UserId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasQueryFilter(
                membership => _tenantContext.IsAvailable
                    && membership.TenantId == _tenantContext.TenantId);
        });
    }

    private void ConfigureSessions(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<UserSession>(entity =>
        {
            entity.ToTable("user_sessions");
            entity.HasKey(session => session.Id);
            entity.Property(session => session.Id).HasColumnName("id");
            entity.Property(session => session.TenantId).HasColumnName("tenant_id");
            entity.Property(session => session.MembershipId).HasColumnName("membership_id");
            entity.Property(session => session.UserId).HasColumnName("user_id");
            entity.Property(session => session.FamilyId).HasColumnName("family_id");
            entity.Property(session => session.TokenHash)
                .HasColumnName("token_hash")
                .HasMaxLength(64);
            entity.Property(session => session.CreatedAtUtc).HasColumnName("created_at_utc");
            entity.Property(session => session.ExpiresAtUtc).HasColumnName("expires_at_utc");
            entity.Property(session => session.LastUsedAtUtc).HasColumnName("last_used_at_utc");
            entity.Property(session => session.RevokedAtUtc).HasColumnName("revoked_at_utc");
            entity.Property(session => session.RevocationReason)
                .HasColumnName("revocation_reason")
                .HasMaxLength(64);
            entity.Property(session => session.ReplacedBySessionId)
                .HasColumnName("replaced_by_session_id");
            entity.HasIndex(session => session.TokenHash)
                .IsUnique()
                .HasDatabaseName("ux_user_sessions_token_hash");
            entity.HasIndex(session => new { session.UserId, session.RevokedAtUtc })
                .HasDatabaseName("ix_user_sessions_user_active");
            entity.HasIndex(session => new { session.FamilyId, session.RevokedAtUtc })
                .HasDatabaseName("ix_user_sessions_family_active");
            entity.HasOne(session => session.Membership)
                .WithMany(membership => membership.Sessions)
                .HasForeignKey(session => new
                {
                    session.TenantId,
                    session.MembershipId,
                    session.UserId,
                })
                .HasPrincipalKey(membership => new
                {
                    membership.TenantId,
                    membership.Id,
                    membership.UserId,
                })
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<User>()
                .WithMany()
                .HasForeignKey(session => session.UserId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasQueryFilter(
                session => _tenantContext.IsAvailable
                    && session.TenantId == _tenantContext.TenantId);
        });
    }

    private static void ConfigureAuthorizationDefinitions(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<RoleDefinition>(entity =>
        {
            entity.ToTable("roles");
            entity.HasKey(role => role.Code);
            entity.Property(role => role.Code).HasColumnName("code").HasMaxLength(32);
            entity.Property(role => role.Name).HasColumnName("name").HasMaxLength(80);
            entity.HasData(TenantRoles.All.Select(role => new { Code = role, Name = role }));
        });
        modelBuilder.Entity<PermissionDefinition>(entity =>
        {
            entity.ToTable("permissions");
            entity.HasKey(permission => permission.Code);
            entity.Property(permission => permission.Code).HasColumnName("code").HasMaxLength(80);
            entity.Property(permission => permission.Name).HasColumnName("name").HasMaxLength(120);
            entity.HasData(Application.Identity.Permissions.All.Select(
                permission => new { Code = permission, Name = permission }));
        });
        modelBuilder.Entity<RolePermission>(entity =>
        {
            entity.ToTable("role_permissions");
            entity.HasKey(rolePermission => new
            {
                rolePermission.RoleCode,
                rolePermission.PermissionCode,
            });
            entity.Property(rolePermission => rolePermission.RoleCode)
                .HasColumnName("role_code")
                .HasMaxLength(32);
            entity.Property(rolePermission => rolePermission.PermissionCode)
                .HasColumnName("permission_code")
                .HasMaxLength(80);
            entity.HasOne<RoleDefinition>()
                .WithMany()
                .HasForeignKey(rolePermission => rolePermission.RoleCode)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne<PermissionDefinition>()
                .WithMany()
                .HasForeignKey(rolePermission => rolePermission.PermissionCode)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasData(
                TenantRoles.All.SelectMany(role => Application.Identity.Permissions.ForRole(role).Select(
                    permission => new { RoleCode = role, PermissionCode = permission })));
        });
    }

    private void GuardTenantWrites()
    {
        IReadOnlyList<EntityEntry> tenantWrites = ChangeTracker
            .Entries()
            .Where(entry => entry.Entity is ITenantOwnedEntity
                && entry.State is EntityState.Added
                    or EntityState.Modified
                    or EntityState.Deleted)
            .ToList();

        if (tenantWrites.Count == 0)
        {
            return;
        }

        if (!_tenantContext.IsAvailable)
        {
            throw new InvalidOperationException(
                "Tenant-owned writes require an authenticated server tenant context.");
        }

        foreach (EntityEntry entry in tenantWrites)
        {
            var tenantOwned = (ITenantOwnedEntity)entry.Entity;
            if (tenantOwned.TenantId != _tenantContext.TenantId)
            {
                throw new InvalidOperationException(
                    "A tenant-owned entity cannot be written outside the active tenant context.");
            }

            if (entry.State is EntityState.Modified or EntityState.Deleted)
            {
                Guid originalTenantId = (Guid)entry.Property(nameof(ITenantOwnedEntity.TenantId))
                    .OriginalValue!;
                if (originalTenantId != _tenantContext.TenantId)
                {
                    throw new InvalidOperationException(
                        "A tenant-owned entity cannot be moved between tenants.");
                }
            }
        }
    }

    private sealed class UnavailableTenantContext : ITenantContext
    {
        public static UnavailableTenantContext Instance { get; } = new();

        public bool IsAvailable => false;

        public Guid TenantId => throw new InvalidOperationException(
            "An authenticated tenant context is required.");
    }
}
