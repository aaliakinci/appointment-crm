using System.Globalization;
using System.Security.Claims;
using AppointmentCrm.Application.Identity;
using AppointmentCrm.Domain.Identity;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.BearerToken;
using Microsoft.Extensions.Options;

namespace AppointmentCrm.Infrastructure.Identity;

internal sealed class AccessTokenIssuer(
    IOptionsMonitor<BearerTokenOptions> bearerOptions,
    IOptions<IdentityOptions> identityOptions,
    TimeProvider timeProvider)
{
    public (string Token, DateTimeOffset ExpiresAtUtc) Issue(
        User user,
        Tenant tenant,
        TenantMembership membership,
        UserSession session)
    {
        var now = timeProvider.GetUtcNow();
        var expiresAt = now.AddMinutes(identityOptions.Value.AccessTokenMinutes);
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.Name, user.DisplayName),
            new(ClaimTypes.Role, membership.Role),
            new(IdentityClaimNames.TenantId, tenant.Id.ToString()),
            new(IdentityClaimNames.TenantName, tenant.Name),
            new(IdentityClaimNames.TenantSlug, tenant.Slug),
            new(IdentityClaimNames.MembershipId, membership.Id.ToString()),
            new(IdentityClaimNames.SessionId, session.Id.ToString()),
            new(
                IdentityClaimNames.SecurityVersion,
                user.SecurityVersion.ToString(CultureInfo.InvariantCulture)),
            new(
                IdentityClaimNames.AuthorizationVersion,
                membership.AuthorizationVersion.ToString(CultureInfo.InvariantCulture)),
        };
        claims.AddRange(Permissions.ForRole(membership.Role).Select(
            permission => new Claim(IdentityClaimNames.Permission, permission)));

        var identity = new ClaimsIdentity(
            claims,
            BearerTokenDefaults.AuthenticationScheme,
            ClaimTypes.Name,
            ClaimTypes.Role);
        var properties = new AuthenticationProperties
        {
            IssuedUtc = now,
            ExpiresUtc = expiresAt,
        };
        var ticket = new AuthenticationTicket(
            new ClaimsPrincipal(identity),
            properties,
            BearerTokenDefaults.AuthenticationScheme);
        var protector = bearerOptions
            .Get(BearerTokenDefaults.AuthenticationScheme)
            .BearerTokenProtector;

        return (protector.Protect(ticket), expiresAt);
    }
}
