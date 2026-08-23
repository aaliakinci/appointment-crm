using System.Diagnostics;

namespace AppointmentCrm.Api.Observability;

internal sealed class CorrelationIdMiddleware(
    RequestDelegate next,
    ILogger<CorrelationIdMiddleware> logger)
{
    public const string HeaderName = "X-Correlation-ID";

    public async Task InvokeAsync(HttpContext context)
    {
        var requestedCorrelationId = context.Request.Headers[HeaderName].FirstOrDefault();
        var correlationId = CorrelationIdFactory.Create(requestedCorrelationId);
        context.TraceIdentifier = correlationId;
        Activity.Current?.SetTag("app.correlation_id", correlationId);
        Activity.Current?.SetBaggage("app.correlation_id", correlationId);
        context.Response.OnStarting(() =>
        {
            context.Response.Headers[HeaderName] = correlationId;
            return Task.CompletedTask;
        });

        using (logger.BeginScope(new Dictionary<string, object>
        {
            ["CorrelationId"] = correlationId,
            ["TraceId"] = Activity.Current?.TraceId.ToHexString() ?? string.Empty,
        }))
        {
            await next(context);
        }
    }
}
