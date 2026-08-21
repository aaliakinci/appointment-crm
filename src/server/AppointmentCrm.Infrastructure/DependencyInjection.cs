using AppointmentCrm.Application.Identity;
using AppointmentCrm.Application.Tenancy;
using AppointmentCrm.Infrastructure.Health;
using AppointmentCrm.Infrastructure.Identity;
using AppointmentCrm.Infrastructure.Persistence;
using AppointmentCrm.Infrastructure.Tenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AppointmentCrm.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services)
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

        services.AddSingleton(TimeProvider.System);
        services.AddScoped<TenantContext>();
        services.AddScoped<ITenantContext>(provider =>
            provider.GetRequiredService<TenantContext>());
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
        services.AddScoped<IMembershipService, MembershipService>();
        services.AddScoped<DemoDataSeeder>();
        services.AddSingleton<PostgresReadinessHealthCheck>();

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
}
