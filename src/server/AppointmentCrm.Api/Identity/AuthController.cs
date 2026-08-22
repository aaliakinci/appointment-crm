using System.Security.Claims;
using AppointmentCrm.Api.Security;
using AppointmentCrm.Application.Common;
using AppointmentCrm.Application.Identity;
using AppointmentCrm.Contracts;
using AppointmentCrm.Infrastructure.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;

namespace AppointmentCrm.Api.Identity;

[ApiController]
[Route("api/v1/auth")]
[Tags("Identity")]
[Authorize]
public sealed class AuthController : ControllerBase
{
    private readonly IIdentitySessionService _identityService;
    private readonly IdentityOptions _options;

    public AuthController(
        IIdentitySessionService identityService,
        IOptions<IdentityOptions> options)
    {
        _identityService = identityService;
        _options = options.Value;
    }

    [HttpPost("login")]
    [AllowAnonymous]
    [EnableRateLimiting("login")]
    public async Task<IActionResult> LoginAsync(
        LoginRequest request,
        CancellationToken cancellationToken)
    {
        AuthenticationOutcome outcome = await _identityService.LoginAsync(
            request.Email,
            request.Password,
            request.TenantId,
            cancellationToken);
        return WriteAuthenticationOutcome(
            outcome,
            IdentityErrorCodes.InvalidCredentials,
            clearCookieOnFailure: false);
    }

    [HttpPost("refresh")]
    [AllowAnonymous]
    [EnableRateLimiting("refresh")]
    [ValidateTrustedOrigin]
    public async Task<IActionResult> RefreshAsync(CancellationToken cancellationToken)
    {
        string? refreshToken = Request.Cookies[_options.RefreshCookieName];
        if (string.IsNullOrWhiteSpace(refreshToken))
        {
            ScheduleRefreshCookieDeletion();
            throw new ApplicationUnauthorizedException(
                IdentityErrorCodes.InvalidSession,
                "The session is not valid.");
        }

        AuthenticationOutcome outcome = await _identityService.RefreshAsync(
            refreshToken,
            cancellationToken);
        return WriteAuthenticationOutcome(outcome);
    }

    [HttpPost("logout")]
    [ValidateTrustedOrigin]
    public async Task<IActionResult> LogoutAsync(CancellationToken cancellationToken)
    {
        if (!TryGetIdentityIds(User, out Guid userId, out _, out Guid sessionId))
        {
            throw new ApplicationUnauthorizedException(
                IdentityErrorCodes.InvalidSession,
                "The session is not valid.");
        }

        string? refreshToken = Request.Cookies[_options.RefreshCookieName];
        await _identityService.LogoutAsync(
            refreshToken,
            userId,
            sessionId,
            cancellationToken);
        DeleteRefreshCookie();
        return NoContent();
    }

    [HttpPost("revoke-all")]
    [Authorize(Policy = Permissions.SessionManageOwn)]
    [ValidateTrustedOrigin]
    public async Task<IActionResult> RevokeAllAsync(CancellationToken cancellationToken)
    {
        if (!TryGetIdentityIds(User, out Guid userId, out _, out _))
        {
            throw new ApplicationUnauthorizedException(
                IdentityErrorCodes.InvalidSession,
                "The session is not valid.");
        }

        await _identityService.RevokeAllAsync(userId, cancellationToken);
        DeleteRefreshCookie();
        return NoContent();
    }

    [HttpPost("switch-tenant")]
    [Authorize(Policy = Permissions.TenantSwitch)]
    [ValidateTrustedOrigin]
    public async Task<IActionResult> SwitchTenantAsync(
        SwitchTenantRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetIdentityIds(User, out Guid userId, out _, out Guid sessionId))
        {
            throw new ApplicationUnauthorizedException(
                IdentityErrorCodes.InvalidSession,
                "The session is not valid.");
        }

        AuthenticationOutcome outcome = await _identityService.SwitchTenantAsync(
            userId,
            sessionId,
            request.TenantId,
            cancellationToken);
        return WriteAuthenticationOutcome(outcome, clearCookieOnFailure: false);
    }

    [HttpGet("me")]
    public IActionResult GetCurrentIdentity()
    {
        ClaimsPrincipal user = User;
        if (!TryGetIdentityIds(user, out Guid userId, out Guid membershipId, out Guid sessionId)
            || !TryGetGuidClaim(user, IdentityClaimNames.TenantId, out Guid tenantId))
        {
            throw new ApplicationUnauthorizedException(
                IdentityErrorCodes.InvalidSession,
                "The session is not valid.");
        }

        return Ok(new CurrentIdentityResponse(
            new AuthenticatedUserResponse(
                userId,
                user.FindFirstValue(ClaimTypes.Email) ?? string.Empty,
                user.FindFirstValue(ClaimTypes.Name) ?? string.Empty),
            new ActiveTenantResponse(
                tenantId,
                user.FindFirstValue(IdentityClaimNames.TenantName) ?? string.Empty,
                user.FindFirstValue(IdentityClaimNames.TenantSlug) ?? string.Empty,
                user.FindFirstValue(IdentityClaimNames.TenantCurrency) ?? string.Empty,
                user.FindFirstValue(IdentityClaimNames.TenantTimeZone) ?? string.Empty,
                user.FindFirstValue(ClaimTypes.Role) ?? string.Empty,
                user.FindAll(IdentityClaimNames.Permission).Select(claim => claim.Value).ToList()),
            membershipId,
            sessionId));
    }

    [HttpGet("tenants")]
    [Authorize(Policy = Permissions.TenantSwitch)]
    public async Task<IActionResult> ListAvailableTenantsAsync(
        CancellationToken cancellationToken)
    {
        if (!TryGetIdentityIds(User, out Guid userId, out _, out _))
        {
            throw new ApplicationUnauthorizedException(
                IdentityErrorCodes.InvalidSession,
                "The session is not valid.");
        }

        IReadOnlyList<TenantOption> tenants = await _identityService.ListAvailableTenantsAsync(
            userId,
            cancellationToken);
        return Ok(tenants.Select(ToResponse));
    }

    private IActionResult WriteAuthenticationOutcome(
        AuthenticationOutcome outcome,
        string failureCode = IdentityErrorCodes.InvalidSession,
        bool clearCookieOnFailure = true)
    {
        if (outcome.Status == AuthenticationStatus.TenantSelectionRequired)
        {
            return Ok(new AuthenticationResponse(
                true,
                null,
                null,
                null,
                null,
                outcome.Tenants.Select(ToResponse).ToList()));
        }

        if (outcome.Status != AuthenticationStatus.Authenticated || outcome.Identity is null)
        {
            if (clearCookieOnFailure)
            {
                ScheduleRefreshCookieDeletion();
            }

            throw outcome.Status switch
            {
                AuthenticationStatus.InvalidTenant => new ApplicationNotFoundException(
                    IdentityErrorCodes.TenantNotAvailable,
                    "The requested tenant is not available."),
                AuthenticationStatus.InvalidCredentials or AuthenticationStatus.InvalidSession =>
                    new ApplicationUnauthorizedException(
                        failureCode,
                        "The credentials or session are not valid."),
                _ => new InvalidOperationException(
                    "The authentication service returned an invalid outcome."),
            };
        }

        AuthenticatedIdentity identity = outcome.Identity;
        AppendRefreshCookie(
            identity.RefreshToken,
            identity.RefreshTokenExpiresAtUtc);
        return Ok(new AuthenticationResponse(
            false,
            identity.AccessToken,
            identity.AccessTokenExpiresAtUtc,
            new AuthenticatedUserResponse(identity.UserId, identity.Email, identity.DisplayName),
            new ActiveTenantResponse(
                identity.TenantId,
                identity.TenantName,
                identity.TenantSlug,
                identity.TenantCurrency,
                identity.TenantTimeZone,
                identity.Role,
                identity.Permissions),
            []));
    }

    private void AppendRefreshCookie(string refreshToken, DateTimeOffset expiresAtUtc)
    {
        Response.Cookies.Append(
            _options.RefreshCookieName,
            refreshToken,
            new CookieOptions
            {
                HttpOnly = true,
                Secure = _options.RequireSecureCookie,
                SameSite = SameSiteMode.Strict,
                IsEssential = true,
                Path = "/api/v1/auth",
                Expires = expiresAtUtc,
            });
    }

    private void DeleteRefreshCookie()
    {
        Response.Cookies.Delete(
            _options.RefreshCookieName,
            new CookieOptions
            {
                HttpOnly = true,
                Secure = _options.RequireSecureCookie,
                SameSite = SameSiteMode.Strict,
                IsEssential = true,
                Path = "/api/v1/auth",
            });
    }

    private void ScheduleRefreshCookieDeletion() =>
        Response.OnStarting(static state =>
        {
            ((AuthController)state).DeleteRefreshCookie();
            return Task.CompletedTask;
        }, this);

    private static bool TryGetIdentityIds(
        ClaimsPrincipal principal,
        out Guid userId,
        out Guid membershipId,
        out Guid sessionId)
    {
        userId = Guid.Empty;
        membershipId = Guid.Empty;
        sessionId = Guid.Empty;
        return TryGetGuidClaim(principal, ClaimTypes.NameIdentifier, out userId)
            && TryGetGuidClaim(principal, IdentityClaimNames.MembershipId, out membershipId)
            && TryGetGuidClaim(principal, IdentityClaimNames.SessionId, out sessionId);
    }

    private static bool TryGetGuidClaim(
        ClaimsPrincipal principal,
        string claimType,
        out Guid value) =>
        Guid.TryParse(principal.FindFirstValue(claimType), out value);

    private static TenantOptionResponse ToResponse(TenantOption tenant) =>
        new(tenant.Id, tenant.Name, tenant.Slug, tenant.Role);

}
