using AppointmentCrm.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace AppointmentCrm.Infrastructure.Health;

public sealed class TimeZoneReadinessHealthCheck(
    IServiceScopeFactory serviceScopeFactory) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            using IServiceScope scope = serviceScopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppointmentCrmDbContext>();
            string[] timeZoneIds = await dbContext.Tenants
                .IgnoreQueryFilters()
                .AsNoTracking()
                .Where(tenant => tenant.IsActive)
                .Select(tenant => tenant.TimeZone)
                .Distinct()
                .ToArrayAsync(cancellationToken);

            foreach (string timeZoneId in timeZoneIds)
            {
                _ = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
            }

            return HealthCheckResult.Healthy(
                $"All {timeZoneIds.Length} active tenant time zones are available.");
        }
        catch (Exception exception) when (
            exception is TimeZoneNotFoundException
                or InvalidTimeZoneException
                or InvalidOperationException)
        {
            return HealthCheckResult.Unhealthy(
                "An active tenant time zone is unavailable on this runtime.",
                exception);
        }
    }
}
