using System.Security.Claims;
using AppointmentCrm.Application.Identity;
using AppointmentCrm.Contracts;
using AppointmentCrm.Infrastructure.Identity;

namespace AppointmentCrm.Api.Identity;

internal static class AuthContractMappings
{
    internal static AuthenticationResponse ToAuthenticationResponse(
        this AuthenticationOutcome outcome) =>
        new(
            RequiresTenantSelection: true,
            AccessToken: null,
            AccessTokenExpiresAtUtc: null,
            User: null,
            ActiveTenant: null,
            Tenants: outcome.Tenants.ToResponse());

    internal static AuthenticationResponse ToAuthenticationResponse(
        this AuthenticatedIdentity identity) =>
        new(
            RequiresTenantSelection: false,
            identity.AccessToken,
            identity.AccessTokenExpiresAtUtc,
            new AuthenticatedUserResponse(
                identity.UserId,
                identity.Email,
                identity.DisplayName),
            new ActiveTenantResponse(
                identity.TenantId,
                identity.TenantName,
                identity.TenantSlug,
                identity.TenantCurrency,
                identity.TenantTimeZone,
                identity.Role,
                identity.Permissions),
            Tenants: []);

    internal static CurrentIdentityResponse ToCurrentIdentityResponse(
        this ClaimsPrincipal principal,
        Guid userId,
        Guid membershipId,
        Guid sessionId,
        Guid tenantId) =>
        new(
            new AuthenticatedUserResponse(
                userId,
                principal.FindFirstValue(ClaimTypes.Email) ?? string.Empty,
                principal.FindFirstValue(ClaimTypes.Name) ?? string.Empty),
            new ActiveTenantResponse(
                tenantId,
                principal.FindFirstValue(IdentityClaimNames.TenantName) ?? string.Empty,
                principal.FindFirstValue(IdentityClaimNames.TenantSlug) ?? string.Empty,
                principal.FindFirstValue(IdentityClaimNames.TenantCurrency) ?? string.Empty,
                principal.FindFirstValue(IdentityClaimNames.TenantTimeZone) ?? string.Empty,
                principal.FindFirstValue(ClaimTypes.Role) ?? string.Empty,
                principal.FindAll(IdentityClaimNames.Permission)
                    .Select(claim => claim.Value)
                    .ToList()),
            membershipId,
            sessionId);

    internal static IReadOnlyList<TenantOptionResponse> ToResponse(
        this IEnumerable<TenantOption> tenants) =>
        tenants.Select(tenant => tenant.ToResponse()).ToList();

    private static TenantOptionResponse ToResponse(this TenantOption tenant) =>
        new(tenant.Id, tenant.Name, tenant.Slug, tenant.Role);
}
