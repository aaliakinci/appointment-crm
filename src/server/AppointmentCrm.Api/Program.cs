using System.Diagnostics;
using System.Security.Claims;
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
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.Options;

Activity.DefaultIdFormat = ActivityIdFormat.W3C;
Activity.ForceDefaultIdFormat = true;

var builder = WebApplication.CreateBuilder(args);
var securityConfiguration = builder.Configuration
    .GetSection(SecurityOptions.SectionName)
    .Get<SecurityOptions>() ?? new SecurityOptions();

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
builder.Services.AddAppointmentCrmObservability(builder.Configuration);
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddOptions<SecurityOptions>()
    .BindConfiguration(SecurityOptions.SectionName)
    .Validate(
        options => options.MaximumRequestBodyBytes is >= 16_384 and <= 10_485_760,
        "Security:MaximumRequestBodyBytes must be between 16 KiB and 10 MiB.")
    .Validate(
        options => options.WritePermitLimit is >= 1 and <= 10_000,
        "Security:WritePermitLimit must be between 1 and 10000.")
    .Validate(
        options => options.WriteWindowSeconds is >= 1 and <= 3_600,
        "Security:WriteWindowSeconds must be between 1 and 3600.")
    .Validate(
        options => options.ForwardLimit is >= 1 and <= 10,
        "Security:ForwardLimit must be between 1 and 10.")
    .ValidateOnStart();
builder.WebHost.ConfigureKestrel(options =>
    options.Limits.MaxRequestBodySize = securityConfiguration.MaximumRequestBodyBytes);
builder.Services.Configure<ForwardedHeadersOptions>(options =>
    SecurityConfiguration.ConfigureForwardedHeaders(options, securityConfiguration));
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
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
        GlobalRateLimit(
            context,
            context.RequestServices.GetRequiredService<IOptions<SecurityOptions>>().Value));
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
if (allowedOrigins.Any(origin => !SecurityConfiguration.IsAllowedOrigin(origin)))
{
    throw new InvalidOperationException(
        "Cors:AllowedOrigins must contain absolute HTTP(S) origins without wildcards, credentials, paths, queries, or fragments.");
}
builder.Services.AddCors(options =>
{
    options.AddPolicy(CorsPolicyNames.Web, policy =>
    {
        if (allowedOrigins.Length > 0)
        {
            policy.WithOrigins(allowedOrigins.Distinct(StringComparer.OrdinalIgnoreCase).ToArray())
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

app.UseForwardedHeaders();
if (!app.Environment.IsDevelopment() && !app.Environment.IsEnvironment("Testing"))
{
    app.UseHsts();
    app.UseHttpsRedirection();
}

app.UseMiddleware<CorrelationIdMiddleware>();
app.UseMiddleware<RequestLoggingMiddleware>();
app.UseMiddleware<SecurityHeadersMiddleware>();
app.UseExceptionHandler();
app.UseStatusCodePages();
app.UseMiddleware<RequestSizeLimitMiddleware>();
app.UseRouting();
app.UseCors(CorsPolicyNames.Web);
app.UseAuthentication();
app.UseSessionValidation();
app.UseRateLimiter();
app.UseAuthorization();

app.MapOpenApi();
app.MapControllers();
app.MapHealthChecks(
    "/health/live",
    HealthResponseWriter.CreateOptions(_ => false));
app.MapHealthChecks(
    "/health/ready",
    HealthResponseWriter.CreateOptions(registration => registration.Tags.Contains("ready")));
app.MapHealthChecks(
    "/health/dependencies",
    HealthResponseWriter.CreateOptions(registration =>
        registration.Tags.Contains("ready") || registration.Tags.Contains("optional")));

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

static RateLimitPartition<string> GlobalRateLimit(
    HttpContext context,
    SecurityOptions options)
{
    if (HttpMethods.IsGet(context.Request.Method)
        || HttpMethods.IsHead(context.Request.Method)
        || HttpMethods.IsOptions(context.Request.Method)
        || HttpMethods.IsTrace(context.Request.Method))
    {
        return RateLimitPartition.GetNoLimiter("safe-method");
    }

    string? tenantId = context.User.FindFirstValue(IdentityClaimNames.TenantId);
    string? userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
    string client = tenantId is not null && userId is not null
        ? $"{tenantId}:{userId}"
        : context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    return RateLimitPartition.GetFixedWindowLimiter(
        $"write:{client}",
        _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = options.WritePermitLimit,
            Window = TimeSpan.FromSeconds(options.WriteWindowSeconds),
            QueueLimit = 0,
            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
            AutoReplenishment = true,
        });
}

public partial class Program;
