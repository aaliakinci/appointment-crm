using AppointmentCrm.Application.Appointments;
using AppointmentCrm.Application.Auditing;
using AppointmentCrm.Application.Customers;
using AppointmentCrm.Application.Employees;
using AppointmentCrm.Application.Identity;
using AppointmentCrm.Application.Reporting;
using AppointmentCrm.Application.Scheduling;
using AppointmentCrm.Application.Services;
using AppointmentCrm.Application.Tenancy;
using AppointmentCrm.Infrastructure.Appointments;
using AppointmentCrm.Infrastructure.Auditing;
using AppointmentCrm.Infrastructure.Customers;
using AppointmentCrm.Infrastructure.Employees;
using AppointmentCrm.Infrastructure.Health;
using AppointmentCrm.Infrastructure.Identity;
using AppointmentCrm.Infrastructure.Outbox;
using AppointmentCrm.Infrastructure.Persistence;
using AppointmentCrm.Infrastructure.Reporting;
using AppointmentCrm.Infrastructure.Scheduling;
using AppointmentCrm.Infrastructure.Services;
using AppointmentCrm.Infrastructure.Tenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.StackExchangeRedis;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AppointmentCrm.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddOptions<IdentityOptions>()
            .BindConfiguration(IdentityOptions.SectionName)
            .Validate(
                options => options.AccessTokenMinutes is >= 1 and <= 60,
                "Identity:AccessTokenMinutes must be between 1 and 60.")
            .Validate(
                options => options.RefreshTokenDays is >= 1 and <= 90,
                "Identity:RefreshTokenDays must be between 1 and 90.")
            .Validate(
                options => !string.IsNullOrWhiteSpace(options.RefreshCookieName),
                "Identity:RefreshCookieName is required.")
            .Validate(
                options => options.LoginPermitLimit is >= 1 and <= 100,
                "Identity:LoginPermitLimit must be between 1 and 100.")
            .Validate(
                options => options.RefreshPermitLimit is >= 1 and <= 100,
                "Identity:RefreshPermitLimit must be between 1 and 100.")
            .ValidateOnStart();
        services.AddOptions<DemoSeedOptions>()
            .BindConfiguration(DemoSeedOptions.SectionName)
            .Validate(
                options => !options.Enabled || options.Password.Length >= 12,
                "DemoSeed:Password must contain at least 12 characters when demo seeding is enabled.")
            .ValidateOnStart();
        services.AddOptions<OutboxOptions>()
            .BindConfiguration(OutboxOptions.SectionName)
            .Validate(
                options => options.BatchSize is >= 1 and <= 200,
                "Outbox:BatchSize must be between 1 and 200.")
            .Validate(
                options => options.PollIntervalSeconds is >= 1 and <= 60,
                "Outbox:PollIntervalSeconds must be between 1 and 60.")
            .Validate(
                options => options.LeaseSeconds is >= 5 and <= 600,
                "Outbox:LeaseSeconds must be between 5 and 600.")
            .Validate(
                options => options.MaximumAttempts is >= 1 and <= 20,
                "Outbox:MaximumAttempts must be between 1 and 20.")
            .Validate(
                options => options.BaseRetryDelaySeconds >= 1
                    && options.MaximumRetryDelaySeconds >= options.BaseRetryDelaySeconds,
                "Outbox retry delays are invalid.")
            .ValidateOnStart();
        services.AddOptions<DashboardCacheOptions>()
            .BindConfiguration(DashboardCacheOptions.SectionName)
            .Validate(
                options => options.TimeToLiveSeconds is >= 5 and <= 300,
                "DashboardCache:TimeToLiveSeconds must be between 5 and 300.")
            .Validate(
                options => !string.IsNullOrWhiteSpace(options.KeyPrefix)
                    && options.KeyPrefix.Length <= 100,
                "DashboardCache:KeyPrefix is required and cannot exceed 100 characters.")
            .ValidateOnStart();

        services.AddStackExchangeRedisCache(_ => { });
        services.AddOptions<RedisCacheOptions>()
            .Configure<IConfiguration>((options, currentConfiguration) =>
                options.Configuration = GetRedisConnectionString(currentConfiguration));

        services.AddSingleton(TimeProvider.System);
        services.AddScoped<TenantContext>();
        services.AddScoped<ITenantContext>(provider =>
            provider.GetRequiredService<TenantContext>());
        services.AddScoped<CurrentActor>();
        services.AddScoped<ICurrentActor>(provider =>
            provider.GetRequiredService<CurrentActor>());
        services.AddDbContext<AppointmentCrmDbContext>((serviceProvider, options) =>
        {
            var configuration = serviceProvider.GetRequiredService<IConfiguration>();
            var connectionString = GetPostgresConnectionString(configuration);

            options.UseNpgsql(
                connectionString,
                npgsql =>
                {
                    npgsql.MigrationsAssembly(typeof(AppointmentCrmDbContext).Assembly.FullName);
                    npgsql.EnableRetryOnFailure(maxRetryCount: 3);
                });
        });
        services.AddSingleton<PasswordHashService>();
        services.AddScoped<AccessTokenIssuer>();
        services.AddScoped<IIdentitySessionService, IdentitySessionService>();
        services.AddScoped<IAccountService, AccountService>();
        services.AddScoped<IMembershipService, MembershipService>();
        services.AddScoped<AuditWriter>();
        services.AddScoped<IAuditReader, AuditReader>();
        services.AddScoped<ReportingService>();
        services.AddScoped<IReportingService, CachedReportingService>();
        services.AddScoped<IAppointmentService, AppointmentService>();
        services.AddScoped<ICustomerService, CustomerService>();
        services.AddScoped<IServiceCatalogService, ServiceCatalogService>();
        services.AddScoped<IEmployeeManagementService, EmployeeManagementService>();
        services.AddScoped<ISchedulingService, SchedulingService>();
        services.AddScoped<DemoDataSeeder>();
        services.AddSingleton<INotificationProvider, DemoNotificationProvider>();
        services.AddSingleton<OutboxProcessor>();
        services.AddHostedService<OutboxWorker>();
        services.AddSingleton<PostgresReadinessHealthCheck>();
        services.AddSingleton<TimeZoneReadinessHealthCheck>();
        services.AddHealthChecks()
            .AddCheck<RedisOptionalHealthCheck>(
                "redis-cache",
                tags: ["optional"],
                timeout: TimeSpan.FromSeconds(2));

        return services;
    }

    internal static string GetPostgresConnectionString(IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Postgres");
        return !string.IsNullOrWhiteSpace(connectionString)
            ? connectionString
            : throw new InvalidOperationException(
                "ConnectionStrings:Postgres must be configured.");
    }

    internal static string GetRedisConnectionString(IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Redis");
        return !string.IsNullOrWhiteSpace(connectionString)
            ? connectionString
            : throw new InvalidOperationException(
                "ConnectionStrings:Redis must be configured when dashboard cache is enabled.");
    }
}
