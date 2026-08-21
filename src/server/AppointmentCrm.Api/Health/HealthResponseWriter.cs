using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace AppointmentCrm.Api.Health;

internal static class HealthResponseWriter
{
    public static HealthCheckOptions CreateOptions(Func<HealthCheckRegistration, bool> predicate)
    {
        return new HealthCheckOptions
        {
            Predicate = predicate,
            ResponseWriter = WriteAsync,
        };
    }

    private static Task WriteAsync(HttpContext context, HealthReport report)
    {
        var payload = new
        {
            status = report.Status.ToString(),
            durationMilliseconds = report.TotalDuration.TotalMilliseconds,
            traceId = context.TraceIdentifier,
            checks = report.Entries.ToDictionary(
                entry => entry.Key,
                entry => new
                {
                    status = entry.Value.Status.ToString(),
                    description = entry.Value.Description,
                    durationMilliseconds = entry.Value.Duration.TotalMilliseconds,
                }),
        };

        context.Response.ContentType = "application/json; charset=utf-8";
        return context.Response.WriteAsJsonAsync(payload);
    }
}
