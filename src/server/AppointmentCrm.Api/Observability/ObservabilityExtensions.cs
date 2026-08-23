using AppointmentCrm.Application.Observability;
using OpenTelemetry.Exporter;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace AppointmentCrm.Api.Observability;

internal static class ObservabilityExtensions
{
    public static IServiceCollection AddAppointmentCrmObservability(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        Uri? otlpEndpoint = ResolveOtlpEndpoint(configuration);
        services.AddOpenTelemetry()
            .ConfigureResource(resource => resource.AddService(
                serviceName: "appointment-crm-api",
                serviceVersion: typeof(Program).Assembly.GetName().Version?.ToString()))
            .WithTracing(tracing =>
            {
                tracing.AddAspNetCoreInstrumentation(options => options.RecordException = true)
                    .AddSource(AppointmentCrmTelemetry.ActivitySourceName)
                    .AddSource("Npgsql");
                if (otlpEndpoint is not null)
                {
                    tracing.AddOtlpExporter(options => ConfigureExporter(options, otlpEndpoint));
                }
            })
            .WithMetrics(metrics =>
            {
                metrics.AddAspNetCoreInstrumentation()
                    .AddRuntimeInstrumentation()
                    .AddMeter(AppointmentCrmTelemetry.MeterName);
                if (otlpEndpoint is not null)
                {
                    metrics.AddOtlpExporter(options => ConfigureExporter(options, otlpEndpoint));
                }
            });

        return services;
    }

    private static Uri? ResolveOtlpEndpoint(IConfiguration configuration)
    {
        string? value = configuration["OpenTelemetry:Otlp:Endpoint"]?.Trim();
        if (string.IsNullOrEmpty(value))
        {
            return null;
        }

        if (!Uri.TryCreate(value, UriKind.Absolute, out Uri? endpoint)
            || (endpoint.Scheme != Uri.UriSchemeHttp && endpoint.Scheme != Uri.UriSchemeHttps))
        {
            throw new InvalidOperationException(
                "OpenTelemetry:Otlp:Endpoint must be an absolute HTTP or HTTPS URI.");
        }

        return endpoint;
    }

    private static void ConfigureExporter(OtlpExporterOptions options, Uri endpoint)
    {
        options.Endpoint = endpoint;
        options.Protocol = OtlpExportProtocol.Grpc;
    }
}
