using System.ComponentModel.DataAnnotations;

namespace AppointmentCrm.Contracts;

public sealed record LoginRequest(
    [Required(ErrorMessage = "A valid email address is required.")]
    [EmailAddress(ErrorMessage = "A valid email address is required.")]
    string Email,
    [Required(ErrorMessage = "Password is required.")]
    string Password,
    Guid? TenantId);

public sealed record SwitchTenantRequest(
    [NotEmptyGuid(ErrorMessage = "TenantId is required.")]
    Guid TenantId);

public sealed record TenantOptionResponse(
    Guid Id,
    string Name,
    string Slug,
    string Role);

public sealed record AuthenticatedUserResponse(
    Guid Id,
    string Email,
    string DisplayName);

public sealed record ActiveTenantResponse(
    Guid Id,
    string Name,
    string Slug,
    string Currency,
    string TimeZone,
    string Role,
    IReadOnlyList<string> Permissions);

public sealed record AuthenticationResponse(
    bool RequiresTenantSelection,
    string? AccessToken,
    DateTimeOffset? AccessTokenExpiresAtUtc,
    AuthenticatedUserResponse? User,
    ActiveTenantResponse? ActiveTenant,
    IReadOnlyList<TenantOptionResponse> Tenants);

public sealed record CurrentIdentityResponse(
    AuthenticatedUserResponse User,
    ActiveTenantResponse ActiveTenant,
    Guid MembershipId,
    Guid SessionId);

public sealed record UpdateMembershipRequest(
    [Required(ErrorMessage = "Role is required.")]
    string Role,
    bool IsActive);

public sealed record MembershipResponse(
    Guid Id,
    Guid UserId,
    string Email,
    string DisplayName,
    string Role,
    bool IsActive,
    DateTimeOffset UpdatedAtUtc);

public sealed record MembershipReportResponse(
    int Total,
    int Active,
    IReadOnlyDictionary<string, int> ByRole);
