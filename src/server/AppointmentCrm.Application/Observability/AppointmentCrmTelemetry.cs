using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace AppointmentCrm.Application.Observability;

public static class AppointmentCrmTelemetry
{
    public const string ActivitySourceName = "AppointmentCrm";
    public const string MeterName = "AppointmentCrm";

    public static readonly ActivitySource ActivitySource = new(ActivitySourceName);

    private static readonly Meter Meter = new(MeterName);
    private static readonly Histogram<double> HttpDuration = Meter.CreateHistogram<double>(
        "appointment_crm.http.server.duration",
        "ms");
    private static readonly Counter<long> HttpErrors = Meter.CreateCounter<long>(
        "appointment_crm.http.server.errors");
    private static readonly Counter<long> CacheOperations = Meter.CreateCounter<long>(
        "appointment_crm.dashboard.cache.operations");
    private static readonly Counter<long> OutboxAttempts = Meter.CreateCounter<long>(
        "appointment_crm.outbox.delivery.attempts");
    private static readonly Counter<long> OutboxTerminalFailures = Meter.CreateCounter<long>(
        "appointment_crm.outbox.delivery.terminal_failures");
    private static readonly Histogram<double> OutboxDuration = Meter.CreateHistogram<double>(
        "appointment_crm.outbox.delivery.duration",
        "ms");

    public static void RecordHttpRequest(
        string method,
        string route,
        int statusCode,
        double elapsedMilliseconds)
    {
        var tags = new TagList
        {
            { "http.request.method", method },
            { "http.route", route },
            { "http.response.status_code", statusCode },
        };
        HttpDuration.Record(elapsedMilliseconds, tags);
        if (statusCode >= 500)
        {
            HttpErrors.Add(1, tags);
        }
    }

    public static void RecordCacheOperation(string outcome) =>
        CacheOperations.Add(1, new KeyValuePair<string, object?>("cache.outcome", outcome));

    public static void RecordOutboxAttempt(
        string messageType,
        string outcome,
        double elapsedMilliseconds,
        bool terminalFailure = false)
    {
        var tags = new TagList
        {
            { "messaging.message.type", messageType },
            { "outbox.outcome", outcome },
        };
        OutboxAttempts.Add(1, tags);
        OutboxDuration.Record(elapsedMilliseconds, tags);
        if (terminalFailure)
        {
            OutboxTerminalFailures.Add(1, tags);
        }
    }
}
