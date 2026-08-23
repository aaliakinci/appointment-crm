using System.Diagnostics;
using System.Text.Json;
using AppointmentCrm.Application.Observability;
using AppointmentCrm.Application.Reporting;
using AppointmentCrm.Application.Tenancy;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AppointmentCrm.Infrastructure.Reporting;

internal sealed class CachedReportingService(
    ReportingService source,
    IDistributedCache cache,
    ITenantContext tenantContext,
    IOptions<DashboardCacheOptions> options,
    ILogger<CachedReportingService> logger) : IReportingService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<ReportingDashboard> GetDashboardAsync(
        ReportingFilter filter,
        CancellationToken cancellationToken)
    {
        DashboardCacheOptions settings = options.Value;
        if (!settings.Enabled)
        {
            AppointmentCrmTelemetry.RecordCacheOperation("disabled");
            return await source.GetDashboardAsync(filter, cancellationToken);
        }

        string key = DashboardCacheKey.Create(settings.KeyPrefix, tenantContext.TenantId, filter);
        try
        {
            byte[]? cached = await cache.GetAsync(key, cancellationToken);
            if (cached is not null)
            {
                ReportingDashboard? dashboard = JsonSerializer.Deserialize<ReportingDashboard>(
                    cached,
                    JsonOptions);
                if (dashboard is not null)
                {
                    Activity.Current?.SetTag("cache.hit", true);
                    AppointmentCrmTelemetry.RecordCacheOperation("hit");
                    return dashboard;
                }
            }

            Activity.Current?.SetTag("cache.hit", false);
            AppointmentCrmTelemetry.RecordCacheOperation("miss");
        }
        catch (Exception exception) when (
            exception is not OperationCanceledException || !cancellationToken.IsCancellationRequested)
        {
            AppointmentCrmTelemetry.RecordCacheOperation("read_error");
            Activity.Current?.AddEvent(new ActivityEvent(
                "dashboard.cache.read_failed",
                tags: new ActivityTagsCollection
                {
                    { "error.type", exception.GetType().FullName },
                }));
            logger.LogWarning(
                "Dashboard cache read failed; PostgreSQL fallback will be used. ErrorType={ErrorType}",
                exception.GetType().FullName ?? exception.GetType().Name);
        }

        ReportingDashboard result = await source.GetDashboardAsync(filter, cancellationToken);
        try
        {
            await cache.SetAsync(
                key,
                JsonSerializer.SerializeToUtf8Bytes(result, JsonOptions),
                new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(
                        settings.TimeToLiveSeconds),
                },
                cancellationToken);
            AppointmentCrmTelemetry.RecordCacheOperation("write");
        }
        catch (Exception exception) when (
            exception is not OperationCanceledException || !cancellationToken.IsCancellationRequested)
        {
            AppointmentCrmTelemetry.RecordCacheOperation("write_error");
            logger.LogWarning(
                "Dashboard cache write failed; database result remains authoritative. ErrorType={ErrorType}",
                exception.GetType().FullName ?? exception.GetType().Name);
        }

        return result;
    }
}
