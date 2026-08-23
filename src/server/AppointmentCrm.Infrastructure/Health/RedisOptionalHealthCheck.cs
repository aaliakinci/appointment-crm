using AppointmentCrm.Infrastructure.Reporting;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace AppointmentCrm.Infrastructure.Health;

internal sealed class RedisOptionalHealthCheck(
    IDistributedCache cache,
    IOptions<DashboardCacheOptions> options) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        if (!options.Value.Enabled)
        {
            return HealthCheckResult.Healthy("Dashboard cache is disabled.");
        }

        try
        {
            _ = await cache.GetAsync(
                $"{options.Value.KeyPrefix}health:probe",
                cancellationToken);
            return HealthCheckResult.Healthy("Optional Redis cache is reachable.");
        }
        catch (Exception exception) when (
            exception is not OperationCanceledException || !cancellationToken.IsCancellationRequested)
        {
            return new HealthCheckResult(
                HealthStatus.Unhealthy,
                "Optional Redis cache is unavailable.",
                data: new Dictionary<string, object>
                {
                    ["errorType"] = exception.GetType().FullName ?? exception.GetType().Name,
                });
        }
    }
}
