using System.Threading.RateLimiting;
using AppointmentCrm.Api.Errors;
using AppointmentCrm.Api.Health;
using AppointmentCrm.Api.Observability;
using AppointmentCrm.Api.Security;
using AppointmentCrm.Application.Identity;
using AppointmentCrm.Infrastructure;
using AppointmentCrm.Infrastructure.Health;
using AppointmentCrm.Infrastructure.Identity;
using Microsoft.AspNetCore.Authentication.BearerToken;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;

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
    options.CustomizeProblemDetails = context => ApiProblemDetailsDefaults.Apply(
        context.HttpContext,
        context.ProblemDetails);
});
builder.Services.AddOpenApi();
builder.Services.AddControllers();
builder.Services.AddSingleton<ProblemDetailsFactory, ApiProblemDetailsFactory>();
builder.Services.AddExceptionHandler<ApiExceptionHandler>();
builder.Services.AddInfrastructure();
builder.Services
    .AddAuthentication(BearerTokenDefaults.AuthenticationScheme)
    .AddBearerToken(options =>
    {
        var identityOptions = builder.Configuration
            .GetSection(IdentityOptions.SectionName)
            .Get<IdentityOptions>() ?? new IdentityOptions();
        options.BearerTokenExpiration = TimeSpan.FromMinutes(
            identityOptions.AccessTokenMinutes);
    });
builder.Services.AddAuthorization(options =>
{
    foreach (string permission in Permissions.All)
    {
        options.AddPolicy(
            permission,
            policy => policy.RequireClaim(IdentityClaimNames.Permission, permission));
    }
});
var identityConfiguration = builder.Configuration
    .GetSection(IdentityOptions.SectionName)
    .Get<IdentityOptions>() ?? new IdentityOptions();
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy(
        "login",
        context => FixedWindow(
            "login",
            context,
            identityConfiguration.LoginPermitLimit));
    options.AddPolicy(
        "refresh",
        context => FixedWindow(
            "refresh",
            context,
            identityConfiguration.RefreshPermitLimit));
});
builder.Services
    .AddHealthChecks()
    .AddCheck<PostgresReadinessHealthCheck>(
        "postgresql",
        tags: ["ready"])
    .AddCheck<TimeZoneReadinessHealthCheck>(
        "tenant-time-zones",
        tags: ["ready"]);

var allowedOrigins = builder.Configuration
    .GetSection("Cors:AllowedOrigins")
    .Get<string[]>() ?? [];
builder.Services.AddCors(options =>
{
    options.AddPolicy(CorsPolicyNames.Web, policy =>
    {
        if (allowedOrigins.Length > 0)
        {
            policy.WithOrigins(allowedOrigins)
                .AllowAnyHeader()
                .AllowAnyMethod()
                .AllowCredentials();
        }
    });
});

if (builder.Configuration.GetValue<bool>("DataProtection:UseEphemeralKeys"))
{
    // Local instances are disposable. A refresh cookie can recover a session after
    // restart without leaving unencrypted key material in the container filesystem.
    builder.Services.AddSingleton<IDataProtectionProvider, EphemeralDataProtectionProvider>();

    // AddAuthentication registers the default key-ring preloader even when the
    // provider is replaced. Remove that unused hosted service so disposable
    // containers do not create an unencrypted file-system key ring.
    var keyRingPreloaders = builder.Services
        .Where(descriptor =>
            descriptor.ServiceType == typeof(IHostedService)
            && descriptor.ImplementationType?.FullName
                == "Microsoft.AspNetCore.DataProtection.Internal.DataProtectionHostedService")
        .ToArray();
    foreach (var keyRingPreloader in keyRingPreloaders)
    {
        builder.Services.Remove(keyRingPreloader);
    }
}
else
{
    builder.Services.AddDataProtection()
        .SetApplicationName("AppointmentCrm");
}

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
app.UseRouting();
app.UseCors(CorsPolicyNames.Web);
app.UseRateLimiter();
app.UseAuthentication();
app.UseSessionValidation();
app.UseAuthorization();

app.MapOpenApi();
app.MapControllers();
app.MapHealthChecks(
    "/health/live",
    HealthResponseWriter.CreateOptions(_ => false));
app.MapHealthChecks(
    "/health/ready",
    HealthResponseWriter.CreateOptions(registration => registration.Tags.Contains("ready")));

app.Run();

static RateLimitPartition<string> FixedWindow(
    string policy,
    HttpContext context,
    int permitLimit)
{
    string client = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    return RateLimitPartition.GetFixedWindowLimiter(
        $"{policy}:{client}",
        _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = permitLimit,
            Window = TimeSpan.FromMinutes(1),
            QueueLimit = 0,
            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
            AutoReplenishment = true,
        });
}

public partial class Program;
