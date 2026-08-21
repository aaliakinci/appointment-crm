using AppointmentCrm.Api.Health;
using AppointmentCrm.Api.Observability;
using AppointmentCrm.Contracts;
using AppointmentCrm.Infrastructure;
using AppointmentCrm.Infrastructure.Health;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddJsonConsole(options =>
{
    options.IncludeScopes = true;
    options.TimestampFormat = "yyyy-MM-dd'T'HH:mm:ss.fff'Z'";
    options.UseUtcTimestamp = true;
});

builder.Services.AddProblemDetails(options =>
{
    options.CustomizeProblemDetails = context =>
    {
        context.ProblemDetails.Extensions["traceId"] = context.HttpContext.TraceIdentifier;
    };
});
builder.Services.AddOpenApi();
builder.Services.AddInfrastructure();
builder.Services
    .AddHealthChecks()
    .AddCheck<PostgresReadinessHealthCheck>(
        "postgresql",
        tags: ["ready"]);

var allowedOrigins = builder.Configuration
    .GetSection("Cors:AllowedOrigins")
    .Get<string[]>() ?? [];
builder.Services.AddCors(options =>
{
    options.AddPolicy("web", policy =>
    {
        if (allowedOrigins.Length > 0)
        {
            policy.WithOrigins(allowedOrigins).AllowAnyHeader().AllowAnyMethod();
        }
    });
});

var app = builder.Build();

if (args.Contains("--migrate", StringComparer.Ordinal))
{
    await MigrationRunner.RunAsync(app.Services);
    return;
}

app.UseMiddleware<CorrelationIdMiddleware>();
app.UseMiddleware<RequestLoggingMiddleware>();
app.UseExceptionHandler();
app.UseStatusCodePages();
app.UseCors("web");

app.MapOpenApi();
app.MapHealthChecks(
    "/health/live",
    HealthResponseWriter.CreateOptions(_ => false));
app.MapHealthChecks(
    "/health/ready",
    HealthResponseWriter.CreateOptions(registration => registration.Tags.Contains("ready")));

var api = app.MapGroup("/api/v1");
api.MapGet(
        "/system/status",
        (HttpContext context) => TypedResults.Ok(new SystemStatusResponse(
            Service: "appointment-crm-api",
            Status: "ready",
            TimestampUtc: DateTimeOffset.UtcNow,
            TraceId: context.TraceIdentifier)))
    .WithName("GetSystemStatus")
    .WithTags("System");

app.Run();

public partial class Program;
