namespace AppointmentCrm.Application.Identity;

public interface IIdentitySessionService
{
    Task<AuthenticationOutcome> LoginAsync(
        string email,
        string password,
        Guid? tenantId,
        CancellationToken cancellationToken);

    Task<AuthenticationOutcome> RefreshAsync(
        string refreshToken,
        CancellationToken cancellationToken);

    Task<AuthenticationOutcome> SwitchTenantAsync(
        Guid userId,
        Guid currentSessionId,
        Guid targetTenantId,
        CancellationToken cancellationToken);

    Task LogoutAsync(
        string? refreshToken,
        Guid userId,
        Guid currentSessionId,
        CancellationToken cancellationToken);

    Task RevokeAllAsync(Guid userId, CancellationToken cancellationToken);

    Task<IReadOnlyList<TenantOption>> ListAvailableTenantsAsync(
        Guid userId,
        CancellationToken cancellationToken);
}

public interface IMembershipService
{
    Task<IReadOnlyList<MembershipSummary>> ListAsync(CancellationToken cancellationToken);

    Task<MembershipSummary?> GetAsync(Guid membershipId, CancellationToken cancellationToken);

    Task<MembershipSummary?> UpdateAsync(
        Guid membershipId,
        string role,
        bool isActive,
        CancellationToken cancellationToken);

    Task<bool> ArchiveAsync(Guid membershipId, CancellationToken cancellationToken);

    Task<MembershipReport> GetReportAsync(CancellationToken cancellationToken);
}

public interface IAccountService
{
    Task<AccountProfile> GetProfileAsync(
        Guid userId,
        CancellationToken cancellationToken);

    Task<AccountProfile> UpdateProfileAsync(
        Guid userId,
        string displayName,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<AccountSession>> ListActiveSessionsAsync(
        Guid userId,
        Guid currentSessionId,
        CancellationToken cancellationToken);

    Task<bool> RevokeSessionAsync(
        Guid userId,
        Guid sessionId,
        CancellationToken cancellationToken);
}
