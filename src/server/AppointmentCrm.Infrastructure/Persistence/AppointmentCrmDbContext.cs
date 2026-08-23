using AppointmentCrm.Application.Identity;
using AppointmentCrm.Application.Tenancy;
using AppointmentCrm.Domain.Appointments;
using AppointmentCrm.Domain.Auditing;
using AppointmentCrm.Domain.Common;
using AppointmentCrm.Domain.Customers;
using AppointmentCrm.Domain.Identity;
using AppointmentCrm.Domain.Outbox;
using AppointmentCrm.Domain.Scheduling;
using AppointmentCrm.Domain.Services;
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

    public DbSet<Customer> Customers => Set<Customer>();

    public DbSet<ServiceOffering> Services => Set<ServiceOffering>();

    public DbSet<Employee> Employees => Set<Employee>();

    public DbSet<EmployeeService> EmployeeServices => Set<EmployeeService>();

    public DbSet<WeeklySchedule> WeeklySchedules => Set<WeeklySchedule>();

    public DbSet<WeeklyScheduleVersion> WeeklyScheduleVersions => Set<WeeklyScheduleVersion>();

    public DbSet<WeeklyScheduleVersionPeriod> WeeklyScheduleVersionPeriods =>
        Set<WeeklyScheduleVersionPeriod>();

    public DbSet<DateScheduleOverride> DateScheduleOverrides => Set<DateScheduleOverride>();

    public DbSet<DateScheduleOverridePeriod> DateScheduleOverridePeriods =>
        Set<DateScheduleOverridePeriod>();

    public DbSet<EmployeeTimeOff> EmployeeTimeOffs => Set<EmployeeTimeOff>();

    public DbSet<Appointment> Appointments => Set<Appointment>();

    public DbSet<AppointmentStatusHistory> AppointmentStatusHistory =>
        Set<AppointmentStatusHistory>();

    public DbSet<AuditEntry> AuditEntries => Set<AuditEntry>();

    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    public DbSet<NotificationDelivery> NotificationDeliveries => Set<NotificationDelivery>();

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        GuardImmutableScheduleHistory();
        GuardTenantWrites();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(
        bool acceptAllChangesOnSuccess,
        CancellationToken cancellationToken = default)
    {
        GuardImmutableScheduleHistory();
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
        ConfigureCustomers(modelBuilder);
        ConfigureServices(modelBuilder);
        ConfigureEmployees(modelBuilder);
        ConfigureScheduling(modelBuilder);
        ConfigureAppointments(modelBuilder);
        ConfigureAuditEntries(modelBuilder);
        ConfigureOutbox(modelBuilder);
        ConfigureNotificationDeliveries(modelBuilder);
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
            entity.HasAlternateKey(membership => new
            {
                membership.TenantId,
                membership.UserId,
            })
                .HasName("ak_tenant_memberships_tenant_user");
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

    private void ConfigureCustomers(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Customer>(entity =>
        {
            entity.ToTable("customers");
            entity.HasKey(customer => customer.Id);
            entity.HasAlternateKey(customer => new { customer.TenantId, customer.Id });
            entity.Property(customer => customer.Id).HasColumnName("id");
            entity.Property(customer => customer.TenantId).HasColumnName("tenant_id");
            entity.Property(customer => customer.Name).HasColumnName("name").HasMaxLength(160);
            entity.Property(customer => customer.NormalizedName)
                .HasColumnName("normalized_name")
                .HasMaxLength(160);
            entity.Property(customer => customer.Email).HasColumnName("email").HasMaxLength(320);
            entity.Property(customer => customer.NormalizedEmail)
                .HasColumnName("normalized_email")
                .HasMaxLength(320);
            entity.Property(customer => customer.Phone).HasColumnName("phone").HasMaxLength(40);
            entity.Property(customer => customer.NormalizedPhone)
                .HasColumnName("normalized_phone")
                .HasMaxLength(15);
            entity.Property(customer => customer.Notes).HasColumnName("notes").HasMaxLength(2_000);
            entity.Property(customer => customer.ArchivedAtUtc).HasColumnName("archived_at_utc");
            entity.Property(customer => customer.CreatedAtUtc).HasColumnName("created_at_utc");
            entity.Property(customer => customer.UpdatedAtUtc).HasColumnName("updated_at_utc");
            entity.HasIndex(customer => new { customer.TenantId, customer.NormalizedEmail })
                .IsUnique()
                .HasFilter("normalized_email IS NOT NULL")
                .HasDatabaseName("ux_customers_tenant_email");
            entity.HasIndex(customer => new { customer.TenantId, customer.NormalizedPhone })
                .IsUnique()
                .HasFilter("normalized_phone IS NOT NULL")
                .HasDatabaseName("ux_customers_tenant_phone");
            entity.HasIndex(customer => new
            {
                customer.TenantId,
                customer.ArchivedAtUtc,
                customer.NormalizedName,
            })
                .HasDatabaseName("ix_customers_tenant_active_name");
            entity.HasOne<Tenant>()
                .WithMany()
                .HasForeignKey(customer => customer.TenantId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasQueryFilter(
                customer => _tenantContext.IsAvailable
                    && customer.TenantId == _tenantContext.TenantId);
        });
    }

    private void ConfigureServices(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ServiceOffering>(entity =>
        {
            entity.ToTable(
                "services",
                table =>
                {
                    table.HasCheckConstraint(
                        "ck_services_duration",
                        "duration_minutes BETWEEN 5 AND 480 AND duration_minutes % 5 = 0");
                    table.HasCheckConstraint(
                        "ck_services_price",
                        "price >= 0 AND price <= 1000000");
                    table.HasCheckConstraint(
                        "ck_services_currency",
                        "currency ~ '^[A-Z]{3}$'");
                });
            entity.HasKey(service => service.Id);
            entity.HasAlternateKey(service => new { service.TenantId, service.Id });
            entity.Property(service => service.Id).HasColumnName("id");
            entity.Property(service => service.TenantId).HasColumnName("tenant_id");
            entity.Property(service => service.Name).HasColumnName("name").HasMaxLength(160);
            entity.Property(service => service.NormalizedName)
                .HasColumnName("normalized_name")
                .HasMaxLength(160);
            entity.Property(service => service.DurationMinutes).HasColumnName("duration_minutes");
            entity.Property(service => service.Price)
                .HasColumnName("price")
                .HasPrecision(12, 2);
            entity.Property(service => service.Currency)
                .HasColumnName("currency")
                .HasMaxLength(3);
            entity.Property(service => service.IsActive).HasColumnName("is_active");
            entity.Property(service => service.CreatedAtUtc).HasColumnName("created_at_utc");
            entity.Property(service => service.UpdatedAtUtc).HasColumnName("updated_at_utc");
            entity.HasIndex(service => new { service.TenantId, service.NormalizedName })
                .IsUnique()
                .HasDatabaseName("ux_services_tenant_name");
            entity.HasIndex(service => new
            {
                service.TenantId,
                service.IsActive,
                service.NormalizedName,
            })
                .HasDatabaseName("ix_services_tenant_active_name");
            entity.HasOne<Tenant>()
                .WithMany()
                .HasForeignKey(service => service.TenantId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasQueryFilter(
                service => _tenantContext.IsAvailable
                    && service.TenantId == _tenantContext.TenantId);
        });
    }

    private void ConfigureEmployees(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Employee>(entity =>
        {
            entity.ToTable("employees");
            entity.HasKey(employee => employee.Id);
            entity.HasAlternateKey(employee => new { employee.TenantId, employee.Id });
            entity.Property(employee => employee.Id).HasColumnName("id");
            entity.Property(employee => employee.TenantId).HasColumnName("tenant_id");
            entity.Property(employee => employee.UserId).HasColumnName("user_id");
            entity.Property(employee => employee.Name).HasColumnName("name").HasMaxLength(160);
            entity.Property(employee => employee.NormalizedName)
                .HasColumnName("normalized_name")
                .HasMaxLength(160);
            entity.Property(employee => employee.Email).HasColumnName("email").HasMaxLength(320);
            entity.Property(employee => employee.NormalizedEmail)
                .HasColumnName("normalized_email")
                .HasMaxLength(320);
            entity.Property(employee => employee.Phone).HasColumnName("phone").HasMaxLength(40);
            entity.Property(employee => employee.NormalizedPhone)
                .HasColumnName("normalized_phone")
                .HasMaxLength(15);
            entity.Property(employee => employee.IsActive).HasColumnName("is_active");
            entity.Property(employee => employee.CreatedAtUtc).HasColumnName("created_at_utc");
            entity.Property(employee => employee.UpdatedAtUtc).HasColumnName("updated_at_utc");
            entity.HasIndex(employee => new { employee.TenantId, employee.UserId })
                .IsUnique()
                .HasFilter("user_id IS NOT NULL")
                .HasDatabaseName("ux_employees_tenant_user");
            entity.HasIndex(employee => new
            {
                employee.TenantId,
                employee.IsActive,
                employee.NormalizedName,
            })
                .HasDatabaseName("ix_employees_tenant_active_name");
            entity.HasOne<Tenant>()
                .WithMany()
                .HasForeignKey(employee => employee.TenantId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(employee => employee.Membership)
                .WithMany()
                .HasForeignKey(employee => new { employee.TenantId, employee.UserId })
                .HasPrincipalKey(membership => new { membership.TenantId, membership.UserId })
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired(false);
            entity.HasQueryFilter(
                employee => _tenantContext.IsAvailable
                    && employee.TenantId == _tenantContext.TenantId);
        });

        modelBuilder.Entity<EmployeeService>(entity =>
        {
            entity.ToTable("employee_services");
            entity.HasKey(assignment => new
            {
                assignment.TenantId,
                assignment.EmployeeId,
                assignment.ServiceId,
            });
            entity.Property(assignment => assignment.TenantId).HasColumnName("tenant_id");
            entity.Property(assignment => assignment.EmployeeId).HasColumnName("employee_id");
            entity.Property(assignment => assignment.ServiceId).HasColumnName("service_id");
            entity.Property(assignment => assignment.AssignedAtUtc)
                .HasColumnName("assigned_at_utc");
            entity.HasIndex(assignment => new { assignment.TenantId, assignment.ServiceId })
                .HasDatabaseName("ix_employee_services_tenant_service");
            entity.HasOne(assignment => assignment.Employee)
                .WithMany(employee => employee.ServiceAssignments)
                .HasForeignKey(assignment => new
                {
                    assignment.TenantId,
                    assignment.EmployeeId,
                })
                .HasPrincipalKey(employee => new { employee.TenantId, employee.Id })
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(assignment => assignment.Service)
                .WithMany(service => service.EmployeeAssignments)
                .HasForeignKey(assignment => new
                {
                    assignment.TenantId,
                    assignment.ServiceId,
                })
                .HasPrincipalKey(service => new { service.TenantId, service.Id })
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasQueryFilter(
                assignment => _tenantContext.IsAvailable
                    && assignment.TenantId == _tenantContext.TenantId);
        });
    }

    private void ConfigureAuditEntries(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AuditEntry>(entity =>
        {
            entity.ToTable("audit_entries");
            entity.HasKey(entry => entry.Id);
            entity.Property(entry => entry.Id).HasColumnName("id");
            entity.Property(entry => entry.TenantId).HasColumnName("tenant_id");
            entity.Property(entry => entry.ActorUserId).HasColumnName("actor_user_id");
            entity.Property(entry => entry.ActorMembershipId)
                .HasColumnName("actor_membership_id");
            entity.Property(entry => entry.Action).HasColumnName("action").HasMaxLength(80);
            entity.Property(entry => entry.TargetType)
                .HasColumnName("target_type")
                .HasMaxLength(80);
            entity.Property(entry => entry.TargetId).HasColumnName("target_id");
            entity.Property(entry => entry.Summary).HasColumnName("summary").HasMaxLength(1_000);
            entity.Property(entry => entry.OccurredAtUtc).HasColumnName("occurred_at_utc");
            entity.HasIndex(entry => new { entry.TenantId, entry.OccurredAtUtc })
                .HasDatabaseName("ix_audit_entries_tenant_occurred");
            entity.HasIndex(entry => new
            {
                entry.TenantId,
                entry.TargetType,
                entry.TargetId,
            })
                .HasDatabaseName("ix_audit_entries_tenant_target");
            entity.HasOne<TenantMembership>()
                .WithMany()
                .HasForeignKey(entry => new
                {
                    entry.TenantId,
                    entry.ActorMembershipId,
                    entry.ActorUserId,
                })
                .HasPrincipalKey(membership => new
                {
                    membership.TenantId,
                    membership.Id,
                    membership.UserId,
                })
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasQueryFilter(
                entry => _tenantContext.IsAvailable
                    && entry.TenantId == _tenantContext.TenantId);
        });
    }

    private void ConfigureAppointments(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Appointment>(entity =>
        {
            entity.ToTable(
                "appointments",
                table =>
                {
                    table.HasCheckConstraint(
                        "ck_appointments_status",
                        "status IN ('Scheduled', 'Confirmed', 'Completed', 'Cancelled', 'NoShow')");
                    table.HasCheckConstraint(
                        "ck_appointments_range",
                        "starts_at_utc < ends_at_utc");
                    table.HasCheckConstraint(
                        "ck_appointments_snapshot_duration",
                        "service_duration_minutes BETWEEN 5 AND 480 AND service_duration_minutes % 5 = 0");
                    table.HasCheckConstraint(
                        "ck_appointments_snapshot_price",
                        "service_price >= 0 AND service_price <= 1000000");
                    table.HasCheckConstraint(
                        "ck_appointments_snapshot_currency",
                        "service_currency ~ '^[A-Z]{3}$'");
                    table.HasCheckConstraint(
                        "ck_appointments_revision",
                        "revision > 0");
                });
            entity.HasKey(appointment => appointment.Id);
            entity.HasAlternateKey(appointment => new { appointment.TenantId, appointment.Id });
            entity.Property(appointment => appointment.Id).HasColumnName("id");
            entity.Property(appointment => appointment.TenantId).HasColumnName("tenant_id");
            entity.Property(appointment => appointment.CustomerId).HasColumnName("customer_id");
            entity.Property(appointment => appointment.EmployeeId).HasColumnName("employee_id");
            entity.Property(appointment => appointment.ServiceId).HasColumnName("service_id");
            entity.Property(appointment => appointment.Status)
                .HasColumnName("status")
                .HasConversion<string>()
                .HasMaxLength(16);
            entity.Property(appointment => appointment.StartsAtUtc).HasColumnName("starts_at_utc");
            entity.Property(appointment => appointment.EndsAtUtc).HasColumnName("ends_at_utc");
            entity.Property(appointment => appointment.ServiceName)
                .HasColumnName("service_name")
                .HasMaxLength(160);
            entity.Property(appointment => appointment.ServiceDurationMinutes)
                .HasColumnName("service_duration_minutes");
            entity.Property(appointment => appointment.ServicePrice)
                .HasColumnName("service_price")
                .HasPrecision(12, 2);
            entity.Property(appointment => appointment.ServiceCurrency)
                .HasColumnName("service_currency")
                .HasMaxLength(3);
            entity.Property(appointment => appointment.Notes)
                .HasColumnName("notes")
                .HasMaxLength(1_000);
            entity.Property(appointment => appointment.Revision)
                .HasColumnName("revision")
                .IsConcurrencyToken();
            entity.Property(appointment => appointment.CreatedAtUtc).HasColumnName("created_at_utc");
            entity.Property(appointment => appointment.UpdatedAtUtc).HasColumnName("updated_at_utc");
            entity.HasIndex(appointment => new
            {
                appointment.TenantId,
                appointment.EmployeeId,
                appointment.StartsAtUtc,
            }).HasDatabaseName("ix_appointments_tenant_employee_start");
            entity.HasIndex(appointment => new
            {
                appointment.TenantId,
                appointment.CustomerId,
                appointment.StartsAtUtc,
            }).HasDatabaseName("ix_appointments_tenant_customer_start");
            entity.HasIndex(appointment => new
            {
                appointment.TenantId,
                appointment.Status,
                appointment.StartsAtUtc,
            }).HasDatabaseName("ix_appointments_tenant_status_start");
            entity.HasOne(appointment => appointment.Customer)
                .WithMany()
                .HasForeignKey(appointment => new
                {
                    appointment.TenantId,
                    appointment.CustomerId,
                })
                .HasPrincipalKey(customer => new { customer.TenantId, customer.Id })
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(appointment => appointment.Employee)
                .WithMany()
                .HasForeignKey(appointment => new
                {
                    appointment.TenantId,
                    appointment.EmployeeId,
                })
                .HasPrincipalKey(employee => new { employee.TenantId, employee.Id })
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(appointment => appointment.Service)
                .WithMany()
                .HasForeignKey(appointment => new
                {
                    appointment.TenantId,
                    appointment.ServiceId,
                })
                .HasPrincipalKey(service => new { service.TenantId, service.Id })
                .OnDelete(DeleteBehavior.Restrict);
            entity.Navigation(appointment => appointment.StatusHistory)
                .UsePropertyAccessMode(PropertyAccessMode.Field);
            entity.HasQueryFilter(appointment =>
                _tenantContext.IsAvailable && appointment.TenantId == _tenantContext.TenantId);
        });

        modelBuilder.Entity<AppointmentStatusHistory>(entity =>
        {
            entity.ToTable(
                "appointment_status_history",
                table =>
                {
                    table.HasCheckConstraint(
                        "ck_appointment_status_history_from_status",
                        "from_status IS NULL OR from_status IN ('Scheduled', 'Confirmed', 'Completed', 'Cancelled', 'NoShow')");
                    table.HasCheckConstraint(
                        "ck_appointment_status_history_to_status",
                        "to_status IN ('Scheduled', 'Confirmed', 'Completed', 'Cancelled', 'NoShow')");
                });
            entity.HasKey(history => history.Id);
            entity.Property(history => history.Id).HasColumnName("id");
            entity.Property(history => history.TenantId).HasColumnName("tenant_id");
            entity.Property(history => history.AppointmentId).HasColumnName("appointment_id");
            entity.Property(history => history.FromStatus)
                .HasColumnName("from_status")
                .HasConversion<string>()
                .HasMaxLength(16);
            entity.Property(history => history.ToStatus)
                .HasColumnName("to_status")
                .HasConversion<string>()
                .HasMaxLength(16);
            entity.Property(history => history.ActorUserId).HasColumnName("actor_user_id");
            entity.Property(history => history.ActorMembershipId)
                .HasColumnName("actor_membership_id");
            entity.Property(history => history.Reason).HasColumnName("reason").HasMaxLength(500);
            entity.Property(history => history.OccurredAtUtc).HasColumnName("occurred_at_utc");
            entity.HasIndex(history => new
            {
                history.TenantId,
                history.AppointmentId,
                history.OccurredAtUtc,
            }).HasDatabaseName("ix_appointment_status_history_timeline");
            entity.HasOne(history => history.Appointment)
                .WithMany(appointment => appointment.StatusHistory)
                .HasForeignKey(history => new { history.TenantId, history.AppointmentId })
                .HasPrincipalKey(appointment => new { appointment.TenantId, appointment.Id })
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(history => history.ActorMembership)
                .WithMany()
                .HasForeignKey(history => new
                {
                    history.TenantId,
                    history.ActorMembershipId,
                    history.ActorUserId,
                })
                .HasPrincipalKey(membership => new
                {
                    membership.TenantId,
                    membership.Id,
                    membership.UserId,
                })
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasQueryFilter(history =>
                _tenantContext.IsAvailable && history.TenantId == _tenantContext.TenantId);
        });
    }

    private void ConfigureOutbox(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<OutboxMessage>(entity =>
        {
            entity.ToTable(
                "outbox_messages",
                table => table.HasCheckConstraint(
                    "ck_outbox_messages_attempts",
                    "attempts >= 0"));
            entity.HasKey(message => message.Id);
            entity.Property(message => message.Id).HasColumnName("id");
            entity.Property(message => message.TenantId).HasColumnName("tenant_id");
            entity.Property(message => message.Type).HasColumnName("type").HasMaxLength(120);
            entity.Property(message => message.AggregateType)
                .HasColumnName("aggregate_type")
                .HasMaxLength(80);
            entity.Property(message => message.AggregateId).HasColumnName("aggregate_id");
            entity.Property(message => message.PayloadJson)
                .HasColumnName("payload_json")
                .HasColumnType("jsonb");
            entity.Property(message => message.OccurredAtUtc).HasColumnName("occurred_at_utc");
            entity.Property(message => message.ProcessedAtUtc).HasColumnName("processed_at_utc");
            entity.Property(message => message.Attempts).HasColumnName("attempts");
            entity.Property(message => message.NextAttemptAtUtc)
                .HasColumnName("next_attempt_at_utc");
            entity.Property(message => message.LastError)
                .HasColumnName("last_error")
                .HasMaxLength(2_000);
            entity.Property(message => message.FailedAtUtc).HasColumnName("failed_at_utc");
            entity.Property(message => message.LeaseId).HasColumnName("lease_id");
            entity.Property(message => message.LockedUntilUtc)
                .HasColumnName("locked_until_utc");
            entity.Property(message => message.TraceParent)
                .HasColumnName("trace_parent")
                .HasMaxLength(128);
            entity.Property(message => message.TraceState)
                .HasColumnName("trace_state")
                .HasMaxLength(512);
            entity.Property(message => message.CorrelationId)
                .HasColumnName("correlation_id")
                .HasMaxLength(64);
            entity.HasIndex(message => new
            {
                message.ProcessedAtUtc,
                message.FailedAtUtc,
                message.NextAttemptAtUtc,
                message.LockedUntilUtc,
            }).HasDatabaseName("ix_outbox_messages_pending");
            entity.HasIndex(message => new
            {
                message.TenantId,
                message.AggregateType,
                message.AggregateId,
            }).HasDatabaseName("ix_outbox_messages_tenant_aggregate");
            entity.HasOne<Tenant>()
                .WithMany()
                .HasForeignKey(message => message.TenantId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasQueryFilter(message =>
                _tenantContext.IsAvailable && message.TenantId == _tenantContext.TenantId);
            entity.HasAlternateKey(message => new { message.TenantId, message.Id })
                .HasName("ak_outbox_messages_tenant_id");
        });
    }

    private void ConfigureNotificationDeliveries(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<NotificationDelivery>(entity =>
        {
            entity.ToTable("notification_deliveries");
            entity.HasKey(delivery => delivery.Id);
            entity.Property(delivery => delivery.Id).HasColumnName("id");
            entity.Property(delivery => delivery.TenantId).HasColumnName("tenant_id");
            entity.Property(delivery => delivery.OutboxMessageId)
                .HasColumnName("outbox_message_id");
            entity.Property(delivery => delivery.MessageType)
                .HasColumnName("message_type")
                .HasMaxLength(120);
            entity.Property(delivery => delivery.AggregateType)
                .HasColumnName("aggregate_type")
                .HasMaxLength(80);
            entity.Property(delivery => delivery.AggregateId).HasColumnName("aggregate_id");
            entity.Property(delivery => delivery.DeliveredAtUtc)
                .HasColumnName("delivered_at_utc");
            entity.Property(delivery => delivery.TraceId)
                .HasColumnName("trace_id")
                .HasMaxLength(32);
            entity.Property(delivery => delivery.CorrelationId)
                .HasColumnName("correlation_id")
                .HasMaxLength(64);
            entity.HasIndex(delivery => new
            {
                delivery.TenantId,
                delivery.OutboxMessageId,
            })
                .IsUnique()
                .HasDatabaseName("ux_notification_deliveries_tenant_outbox");
            entity.HasIndex(delivery => new
            {
                delivery.TenantId,
                delivery.AggregateType,
                delivery.AggregateId,
            }).HasDatabaseName("ix_notification_deliveries_tenant_aggregate");
            entity.HasOne<OutboxMessage>()
                .WithMany()
                .HasForeignKey(delivery => new
                {
                    delivery.TenantId,
                    delivery.OutboxMessageId,
                })
                .HasPrincipalKey(message => new { message.TenantId, message.Id })
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasQueryFilter(delivery =>
                _tenantContext.IsAvailable && delivery.TenantId == _tenantContext.TenantId);
        });
    }

    private void ConfigureScheduling(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<WeeklySchedule>(entity =>
        {
            entity.ToTable(
                "weekly_schedules",
                table => table.HasCheckConstraint(
                    "ck_weekly_schedules_revision",
                    "revision > 0"));
            entity.HasKey(schedule => schedule.Id);
            entity.HasAlternateKey(schedule => new { schedule.TenantId, schedule.Id });
            entity.Property(schedule => schedule.Id).HasColumnName("id");
            entity.Property(schedule => schedule.TenantId).HasColumnName("tenant_id");
            entity.Property(schedule => schedule.EmployeeId).HasColumnName("employee_id");
            entity.Property(schedule => schedule.CurrentVersionId)
                .HasColumnName("current_version_id");
            entity.Property(schedule => schedule.Revision).HasColumnName("revision");
            entity.Property(schedule => schedule.CreatedAtUtc).HasColumnName("created_at_utc");
            entity.Property(schedule => schedule.UpdatedAtUtc).HasColumnName("updated_at_utc");
            entity.HasIndex(schedule => schedule.TenantId)
                .IsUnique()
                .HasFilter("employee_id IS NULL")
                .HasDatabaseName("ux_weekly_schedules_tenant_default");
            entity.HasIndex(schedule => new { schedule.TenantId, schedule.EmployeeId })
                .IsUnique()
                .HasFilter("employee_id IS NOT NULL")
                .HasDatabaseName("ux_weekly_schedules_tenant_employee");
            entity.HasOne<Employee>()
                .WithMany()
                .HasForeignKey(schedule => new { schedule.TenantId, schedule.EmployeeId })
                .HasPrincipalKey(employee => new { employee.TenantId, employee.Id })
                .OnDelete(DeleteBehavior.Cascade)
                .IsRequired(false);
            entity.HasMany(schedule => schedule.Versions)
                .WithOne(version => version.Schedule)
                .HasForeignKey(version => new { version.TenantId, version.ScheduleId })
                .HasPrincipalKey(schedule => new { schedule.TenantId, schedule.Id })
                .OnDelete(DeleteBehavior.Restrict);
            entity.Navigation(schedule => schedule.Versions)
                .UsePropertyAccessMode(PropertyAccessMode.Field);
            entity.HasQueryFilter(schedule =>
                _tenantContext.IsAvailable && schedule.TenantId == _tenantContext.TenantId);
        });

        modelBuilder.Entity<WeeklyScheduleVersion>(entity =>
        {
            entity.ToTable(
                "weekly_schedule_versions",
                table =>
                {
                    table.HasCheckConstraint(
                        "ck_weekly_schedule_versions_number",
                        "version_number > 0");
                    table.HasCheckConstraint(
                        "ck_weekly_schedule_versions_mode",
                        "mode IN ('Custom', 'Closed', 'Inherited')");
                });
            entity.HasKey(version => version.Id);
            entity.HasAlternateKey(version => new
            {
                version.TenantId,
                version.ScheduleId,
                version.Id,
            });
            entity.HasAlternateKey(version => new { version.TenantId, version.Id });
            entity.Property(version => version.Id).HasColumnName("id");
            entity.Property(version => version.TenantId).HasColumnName("tenant_id");
            entity.Property(version => version.ScheduleId).HasColumnName("schedule_id");
            entity.Property(version => version.VersionNumber).HasColumnName("version_number");
            entity.Property(version => version.Mode)
                .HasColumnName("mode")
                .HasConversion<string>()
                .HasMaxLength(16);
            entity.Property(version => version.ActorUserId).HasColumnName("actor_user_id");
            entity.Property(version => version.ActorMembershipId)
                .HasColumnName("actor_membership_id");
            entity.Property(version => version.ChangeNote)
                .HasColumnName("change_note")
                .HasMaxLength(500);
            entity.Property(version => version.RestoredFromVersionId)
                .HasColumnName("restored_from_version_id");
            entity.Property(version => version.CreatedAtUtc).HasColumnName("created_at_utc");
            entity.HasIndex(version => new
            {
                version.TenantId,
                version.ScheduleId,
                version.VersionNumber,
            })
                .IsUnique()
                .HasDatabaseName("ux_weekly_schedule_versions_schedule_number");
            entity.HasIndex(version => new
            {
                version.TenantId,
                version.ScheduleId,
                version.CreatedAtUtc,
            }).HasDatabaseName("ix_weekly_schedule_versions_history");
            entity.HasOne<TenantMembership>()
                .WithMany()
                .HasForeignKey(version => new
                {
                    version.TenantId,
                    version.ActorMembershipId,
                    version.ActorUserId,
                })
                .HasPrincipalKey(membership => new
                {
                    membership.TenantId,
                    membership.Id,
                    membership.UserId,
                })
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired(false);
            entity.HasOne<WeeklyScheduleVersion>()
                .WithMany()
                .HasForeignKey(version => new
                {
                    version.TenantId,
                    version.ScheduleId,
                    version.RestoredFromVersionId,
                })
                .HasPrincipalKey(version => new
                {
                    version.TenantId,
                    version.ScheduleId,
                    version.Id,
                })
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired(false);
            entity.Navigation(version => version.Periods)
                .UsePropertyAccessMode(PropertyAccessMode.Field);
            entity.HasQueryFilter(version =>
                _tenantContext.IsAvailable && version.TenantId == _tenantContext.TenantId);
        });

        modelBuilder.Entity<WeeklyScheduleVersionPeriod>(entity =>
        {
            entity.ToTable(
                "weekly_schedule_version_periods",
                table =>
                {
                    table.HasCheckConstraint(
                        "ck_weekly_schedule_version_periods_day",
                        "day_of_week BETWEEN 1 AND 7");
                    table.HasCheckConstraint(
                        "ck_weekly_schedule_version_periods_minutes",
                        "start_minute >= 0 AND end_minute <= 1440 AND start_minute < end_minute AND start_minute % 5 = 0 AND end_minute % 5 = 0");
                });
            entity.HasKey(period => period.Id);
            entity.Property(period => period.Id).HasColumnName("id");
            entity.Property(period => period.TenantId).HasColumnName("tenant_id");
            entity.Property(period => period.VersionId).HasColumnName("version_id");
            entity.Property(period => period.DayOfWeek).HasColumnName("day_of_week");
            entity.Property(period => period.StartMinute).HasColumnName("start_minute");
            entity.Property(period => period.EndMinute).HasColumnName("end_minute");
            entity.HasIndex(period => new
            {
                period.TenantId,
                period.VersionId,
                period.DayOfWeek,
                period.StartMinute,
            }).HasDatabaseName("ix_weekly_schedule_version_periods_lookup");
            entity.HasOne(period => period.Version)
                .WithMany(version => version.Periods)
                .HasForeignKey(period => new { period.TenantId, period.VersionId })
                .HasPrincipalKey(version => new { version.TenantId, version.Id })
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasQueryFilter(period =>
                _tenantContext.IsAvailable && period.TenantId == _tenantContext.TenantId);
        });

        modelBuilder.Entity<DateScheduleOverride>(entity =>
        {
            entity.ToTable("date_schedule_overrides");
            entity.HasKey(scheduleOverride => scheduleOverride.Id);
            entity.HasAlternateKey(scheduleOverride => new
            {
                scheduleOverride.TenantId,
                scheduleOverride.Id,
            });
            entity.Property(scheduleOverride => scheduleOverride.Id).HasColumnName("id");
            entity.Property(scheduleOverride => scheduleOverride.TenantId)
                .HasColumnName("tenant_id");
            entity.Property(scheduleOverride => scheduleOverride.EmployeeId)
                .HasColumnName("employee_id");
            entity.Property(scheduleOverride => scheduleOverride.Date).HasColumnName("date");
            entity.Property(scheduleOverride => scheduleOverride.IsClosed)
                .HasColumnName("is_closed");
            entity.Property(scheduleOverride => scheduleOverride.CreatedAtUtc)
                .HasColumnName("created_at_utc");
            entity.Property(scheduleOverride => scheduleOverride.UpdatedAtUtc)
                .HasColumnName("updated_at_utc");
            entity.HasIndex(scheduleOverride => new
            {
                scheduleOverride.TenantId,
                scheduleOverride.Date,
            })
                .IsUnique()
                .HasFilter("employee_id IS NULL")
                .HasDatabaseName("ux_date_overrides_tenant_date");
            entity.HasIndex(scheduleOverride => new
            {
                scheduleOverride.TenantId,
                scheduleOverride.EmployeeId,
                scheduleOverride.Date,
            })
                .IsUnique()
                .HasFilter("employee_id IS NOT NULL")
                .HasDatabaseName("ux_date_overrides_employee_date");
            entity.HasOne<Employee>()
                .WithMany()
                .HasForeignKey(scheduleOverride => new
                {
                    scheduleOverride.TenantId,
                    scheduleOverride.EmployeeId,
                })
                .HasPrincipalKey(employee => new { employee.TenantId, employee.Id })
                .OnDelete(DeleteBehavior.Cascade)
                .IsRequired(false);
            entity.HasQueryFilter(scheduleOverride =>
                _tenantContext.IsAvailable
                && scheduleOverride.TenantId == _tenantContext.TenantId);
        });

        modelBuilder.Entity<DateScheduleOverridePeriod>(entity =>
        {
            entity.ToTable(
                "date_schedule_override_periods",
                table => table.HasCheckConstraint(
                    "ck_date_override_periods_minutes",
                    "start_minute >= 0 AND end_minute <= 1440 AND start_minute < end_minute AND start_minute % 5 = 0 AND end_minute % 5 = 0"));
            entity.HasKey(period => period.Id);
            entity.Property(period => period.Id).HasColumnName("id");
            entity.Property(period => period.TenantId).HasColumnName("tenant_id");
            entity.Property(period => period.OverrideId).HasColumnName("override_id");
            entity.Property(period => period.StartMinute).HasColumnName("start_minute");
            entity.Property(period => period.EndMinute).HasColumnName("end_minute");
            entity.HasIndex(period => new
            {
                period.TenantId,
                period.OverrideId,
                period.StartMinute,
            }).HasDatabaseName("ix_date_override_periods_lookup");
            entity.HasOne(period => period.Override)
                .WithMany(scheduleOverride => scheduleOverride.Periods)
                .HasForeignKey(period => new { period.TenantId, period.OverrideId })
                .HasPrincipalKey(scheduleOverride => new
                {
                    scheduleOverride.TenantId,
                    scheduleOverride.Id,
                })
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasQueryFilter(period =>
                _tenantContext.IsAvailable && period.TenantId == _tenantContext.TenantId);
        });

        modelBuilder.Entity<EmployeeTimeOff>(entity =>
        {
            entity.ToTable(
                "employee_time_offs",
                table => table.HasCheckConstraint(
                    "ck_employee_time_offs_range",
                    "start_utc < end_utc"));
            entity.HasKey(timeOff => timeOff.Id);
            entity.Property(timeOff => timeOff.Id).HasColumnName("id");
            entity.Property(timeOff => timeOff.TenantId).HasColumnName("tenant_id");
            entity.Property(timeOff => timeOff.EmployeeId).HasColumnName("employee_id");
            entity.Property(timeOff => timeOff.StartUtc).HasColumnName("start_utc");
            entity.Property(timeOff => timeOff.EndUtc).HasColumnName("end_utc");
            entity.Property(timeOff => timeOff.Reason).HasColumnName("reason").HasMaxLength(500);
            entity.Property(timeOff => timeOff.CreatedAtUtc).HasColumnName("created_at_utc");
            entity.HasIndex(timeOff => new
            {
                timeOff.TenantId,
                timeOff.EmployeeId,
                timeOff.StartUtc,
                timeOff.EndUtc,
            }).HasDatabaseName("ix_employee_time_offs_overlap");
            entity.HasOne<Employee>()
                .WithMany()
                .HasForeignKey(timeOff => new { timeOff.TenantId, timeOff.EmployeeId })
                .HasPrincipalKey(employee => new { employee.TenantId, employee.Id })
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasQueryFilter(timeOff =>
                _tenantContext.IsAvailable && timeOff.TenantId == _tenantContext.TenantId);
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

    private void GuardImmutableScheduleHistory()
    {
        bool mutatesHistory = ChangeTracker.Entries().Any(entry =>
            (entry.Entity is WeeklyScheduleVersion or WeeklyScheduleVersionPeriod)
            && entry.State is EntityState.Modified or EntityState.Deleted);
        if (mutatesHistory)
        {
            throw new InvalidOperationException(
                "Published weekly schedule versions are immutable.");
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
