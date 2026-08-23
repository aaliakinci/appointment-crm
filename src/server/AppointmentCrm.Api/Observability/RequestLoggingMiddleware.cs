using System.Diagnostics;
using AppointmentCrm.Application.Observability;
using Microsoft.AspNetCore.Routing;

namespace AppointmentCrm.Api.Observability;

internal sealed class RequestLoggingMiddleware(
    RequestDelegate next,
    ILogger<RequestLoggingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            await next(context);
        }
        finally
        {
            stopwatch.Stop();
            string route = context.GetEndpoint() is RouteEndpoint endpoint
                ? endpoint.RoutePattern.RawText ?? "unmatched"
                : "unmatched";
            AppointmentCrmTelemetry.RecordHttpRequest(
                context.Request.Method,
                route,
                context.Response.StatusCode,
                stopwatch.Elapsed.TotalMilliseconds);
            logger.LogInformation(
                "HTTP {Method} {Route} responded {StatusCode} in {ElapsedMilliseconds} ms",
                context.Request.Method,
                route,
                context.Response.StatusCode,
                stopwatch.Elapsed.TotalMilliseconds);
        }
    }
}
