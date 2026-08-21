using System.Globalization;
using System.Security.Claims;
using AppointmentCrm.Application.Identity;
using AppointmentCrm.Infrastructure.Persistence;
using AppointmentCrm.Infrastructure.Tenancy;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AppointmentCrm.Infrastructure.Identity;

public sealed class SessionValidationMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(
        HttpContext context,
        AppointmentCrmDbContext dbContext,
        TenantContext tenantContext,
        TimeProvider timeProvider,
        ILogger<SessionValidationMiddleware> logger)
    {
        if (context.User.Identity?.IsAuthenticated == true)
        {
            bool valid = await ValidateAsync(
                context.User,
                dbContext,
                tenantContext,
                timeProvider.GetUtcNow(),
                context.RequestAborted);
            if (!valid)
            {
                logger.LogInformation(
                    "Rejected a bearer token because its server-side session or membership is no longer valid.");
                context.User = new ClaimsPrincipal(new ClaimsIdentity());
            }
        }

        await next(context);
    }

    private static async Task<bool> ValidateAsync(
        ClaimsPrincipal principal,
        AppointmentCrmDbContext dbContext,
        TenantContext tenantContext,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        if (!TryReadGuid(principal, ClaimTypes.NameIdentifier, out Guid userId)
            || !TryReadGuid(principal, IdentityClaimNames.TenantId, out Guid tenantId)
            || !TryReadGuid(principal, IdentityClaimNames.MembershipId, out Guid membershipId)
            || !TryReadGuid(principal, IdentityClaimNames.SessionId, out Guid sessionId)
            || !TryReadInt(principal, IdentityClaimNames.SecurityVersion, out int securityVersion)
            || !TryReadInt(principal, IdentityClaimNames.AuthorizationVersion, out int authorizationVersion))
        {
            return false;
        }

        var session = await dbContext.UserSessions
            .IgnoreQueryFilters()
            .Include(candidate => candidate.Membership)
                .ThenInclude(membership => membership.User)
            .Include(candidate => candidate.Membership)
                .ThenInclude(membership => membership.Tenant)
            .SingleOrDefaultAsync(
                candidate => candidate.Id == sessionId
                    && candidate.UserId == userId
                    && candidate.TenantId == tenantId
                    && candidate.MembershipId == membershipId,
                cancellationToken);
        if (session is null
            || session.RevokedAtUtc is not null
            || session.ExpiresAtUtc <= now
            || !session.Membership.IsActive
            || !session.Membership.User.IsActive
            || !session.Membership.Tenant.IsActive
            || session.Membership.User.SecurityVersion != securityVersion
            || session.Membership.AuthorizationVersion != authorizationVersion
            || !string.Equals(
                session.Membership.Role,
                principal.FindFirstValue(ClaimTypes.Role),
                StringComparison.Ordinal))
        {
            return false;
        }

        string[] tokenPermissions = principal.FindAll(IdentityClaimNames.Permission)
            .Select(claim => claim.Value)
            .Order(StringComparer.Ordinal)
            .ToArray();
        string[] currentPermissions = Permissions.ForRole(session.Membership.Role)
            .Order(StringComparer.Ordinal)
            .ToArray();
        if (!tokenPermissions.SequenceEqual(currentPermissions, StringComparer.Ordinal))
        {
            return false;
        }

        tenantContext.SetTenant(tenantId);
        return true;
    }

    private static bool TryReadGuid(
        ClaimsPrincipal principal,
        string claimType,
        out Guid value) =>
        Guid.TryParse(principal.FindFirstValue(claimType), out value);

    private static bool TryReadInt(
        ClaimsPrincipal principal,
        string claimType,
        out int value) =>
        int.TryParse(
            principal.FindFirstValue(claimType),
            CultureInfo.InvariantCulture,
            out value);
}

public static class SessionValidationApplicationBuilderExtensions
{
    public static IApplicationBuilder UseSessionValidation(this IApplicationBuilder app) =>
        app.UseMiddleware<SessionValidationMiddleware>();
}
