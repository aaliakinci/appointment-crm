namespace AppointmentCrm.Application.Identity;

public enum AuthenticationStatus
{
    Authenticated,
    TenantSelectionRequired,
    InvalidCredentials,
    InvalidSession,
    InvalidTenant,
}

public sealed record TenantOption(
    Guid Id,
    string Name,
    string Slug,
    string Role);

public sealed record AuthenticatedIdentity(
    Guid UserId,
    string Email,
    string DisplayName,
    Guid TenantId,
    string TenantName,
    string TenantSlug,
    string TenantCurrency,
    string TenantTimeZone,
    Guid MembershipId,
    string Role,
    IReadOnlyList<string> Permissions,
    Guid SessionId,
    string AccessToken,
    DateTimeOffset AccessTokenExpiresAtUtc,
    string RefreshToken,
    DateTimeOffset RefreshTokenExpiresAtUtc);

public sealed record AuthenticationOutcome(
    AuthenticationStatus Status,
    AuthenticatedIdentity? Identity,
    IReadOnlyList<TenantOption> Tenants)
{
    public static AuthenticationOutcome Success(AuthenticatedIdentity identity) =>
        new(AuthenticationStatus.Authenticated, identity, []);

    public static AuthenticationOutcome SelectionRequired(IReadOnlyList<TenantOption> tenants) =>
        new(AuthenticationStatus.TenantSelectionRequired, null, tenants);

    public static AuthenticationOutcome Failure(AuthenticationStatus status) =>
        new(status, null, []);
}

public sealed record CurrentIdentity(
    Guid UserId,
    string Email,
    string DisplayName,
    Guid TenantId,
    string TenantName,
    string TenantSlug,
    string TenantCurrency,
    string TenantTimeZone,
    Guid MembershipId,
    string Role,
    IReadOnlyList<string> Permissions,
    Guid SessionId);

public sealed record MembershipSummary(
    Guid Id,
    Guid UserId,
    string Email,
    string DisplayName,
    string Role,
    bool IsActive,
    DateTimeOffset UpdatedAtUtc);

public sealed record MembershipReport(
    int Total,
    int Active,
    IReadOnlyDictionary<string, int> ByRole);

public sealed record AccountProfile(
    Guid UserId,
    string Email,
    string DisplayName,
    DateTimeOffset UpdatedAtUtc);

public sealed record AccountSession(
    Guid Id,
    string TenantName,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? LastUsedAtUtc,
    DateTimeOffset ExpiresAtUtc,
    bool IsCurrent);
