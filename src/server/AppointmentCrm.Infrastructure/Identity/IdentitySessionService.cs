using AppointmentCrm.Application.Identity;
using AppointmentCrm.Domain.Identity;
using AppointmentCrm.Infrastructure.Persistence;
using AppointmentCrm.Infrastructure.Tenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace AppointmentCrm.Infrastructure.Identity;

internal sealed class IdentitySessionService(
    AppointmentCrmDbContext dbContext,
    TenantContext tenantContext,
    PasswordHashService passwordHashService,
    AccessTokenIssuer accessTokenIssuer,
    IOptions<IdentityOptions> identityOptions,
    TimeProvider timeProvider) : IIdentitySessionService
{
    public async Task<AuthenticationOutcome> LoginAsync(
        string email,
        string password,
        Guid? tenantId,
        CancellationToken cancellationToken)
    {
        string normalizedEmail = User.NormalizeEmail(email);
        var user = await dbContext.Users
            .SingleOrDefaultAsync(
                candidate => candidate.NormalizedEmail == normalizedEmail,
                cancellationToken);

        if (user is null)
        {
            passwordHashService.PerformDummyVerification(password);
            return AuthenticationOutcome.Failure(AuthenticationStatus.InvalidCredentials);
        }

        if (!user.IsActive || !passwordHashService.Verify(user.PasswordHash, password))
        {
            return AuthenticationOutcome.Failure(AuthenticationStatus.InvalidCredentials);
        }

        List<TenantMembership> memberships = await dbContext.TenantMemberships
            .IgnoreQueryFilters()
            .Include(membership => membership.Tenant)
            .Where(membership => membership.UserId == user.Id
                && membership.IsActive
                && membership.Tenant.IsActive)
            .OrderBy(membership => membership.Tenant.Name)
            .ToListAsync(cancellationToken);

        if (memberships.Count == 0)
        {
            return AuthenticationOutcome.Failure(AuthenticationStatus.InvalidCredentials);
        }

        if (tenantId is null && memberships.Count > 1)
        {
            return AuthenticationOutcome.SelectionRequired(
                memberships.Select(ToTenantOption).ToList());
        }

        TenantMembership? membership = tenantId is null
            ? memberships[0]
            : memberships.SingleOrDefault(candidate => candidate.TenantId == tenantId.Value);
        if (membership is null)
        {
            return AuthenticationOutcome.Failure(AuthenticationStatus.InvalidTenant);
        }

        return AuthenticationOutcome.Success(
            await CreateSessionAsync(user, membership, Guid.NewGuid(), cancellationToken));
    }

    public async Task<AuthenticationOutcome> RefreshAsync(
        string refreshToken,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
        {
            return AuthenticationOutcome.Failure(AuthenticationStatus.InvalidSession);
        }

        string tokenHash = RefreshTokenFactory.Hash(refreshToken);
        var existing = await SessionQuery()
            .SingleOrDefaultAsync(session => session.TokenHash == tokenHash, cancellationToken);
        if (existing is null)
        {
            return AuthenticationOutcome.Failure(AuthenticationStatus.InvalidSession);
        }

        var now = timeProvider.GetUtcNow();
        if (existing.RevokedAtUtc is not null)
        {
            if (string.Equals(existing.RevocationReason, "rotated", StringComparison.Ordinal))
            {
                await RevokeFamilyAsync(existing.FamilyId, now, "refresh-reuse", cancellationToken);
            }

            return AuthenticationOutcome.Failure(AuthenticationStatus.InvalidSession);
        }

        if (existing.ExpiresAtUtc <= now
            || !existing.Membership.IsActive
            || !existing.Membership.User.IsActive
            || !existing.Membership.Tenant.IsActive)
        {
            await RevokeSessionAsync(existing.Id, now, "inactive-or-expired", cancellationToken);
            return AuthenticationOutcome.Failure(AuthenticationStatus.InvalidSession);
        }

        string replacementToken = RefreshTokenFactory.Create();
        var replacement = UserSession.Create(
            Guid.NewGuid(),
            existing.TenantId,
            existing.MembershipId,
            existing.UserId,
            existing.FamilyId,
            RefreshTokenFactory.Hash(replacementToken),
            now,
            now.AddDays(identityOptions.Value.RefreshTokenDays));

        var executionStrategy = dbContext.Database.CreateExecutionStrategy();
        bool rotationSucceeded = await executionStrategy.ExecuteAsync(async () =>
        {
            await using var transaction = await dbContext.Database.BeginTransactionAsync(
                cancellationToken);
            int rotated = await dbContext.UserSessions
                .IgnoreQueryFilters()
                .Where(session => session.Id == existing.Id && session.RevokedAtUtc == null)
                .ExecuteUpdateAsync(
                    setters => setters
                        .SetProperty(session => session.LastUsedAtUtc, now)
                        .SetProperty(session => session.RevokedAtUtc, now)
                        .SetProperty(session => session.RevocationReason, "rotated")
                        .SetProperty(session => session.ReplacedBySessionId, replacement.Id),
                    cancellationToken);
            if (rotated != 1)
            {
                await RevokeFamilyAsync(
                    existing.FamilyId,
                    now,
                    "concurrent-refresh",
                    cancellationToken);
                await transaction.CommitAsync(cancellationToken);
                return false;
            }

            tenantContext.SetTenant(existing.TenantId);
            dbContext.UserSessions.Add(replacement);
            await dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return true;
        });
        if (!rotationSucceeded)
        {
            return AuthenticationOutcome.Failure(AuthenticationStatus.InvalidSession);
        }

        return AuthenticationOutcome.Success(BuildIdentity(
            existing.Membership.User,
            existing.Membership.Tenant,
            existing.Membership,
            replacement,
            replacementToken));
    }

    public async Task<AuthenticationOutcome> SwitchTenantAsync(
        Guid userId,
        Guid currentSessionId,
        Guid targetTenantId,
        CancellationToken cancellationToken)
    {
        var currentSession = await SessionQuery()
            .SingleOrDefaultAsync(
                session => session.Id == currentSessionId
                    && session.UserId == userId
                    && session.RevokedAtUtc == null,
                cancellationToken);
        if (currentSession is null)
        {
            return AuthenticationOutcome.Failure(AuthenticationStatus.InvalidSession);
        }

        var membership = await dbContext.TenantMemberships
            .IgnoreQueryFilters()
            .Include(candidate => candidate.User)
            .Include(candidate => candidate.Tenant)
            .SingleOrDefaultAsync(
                candidate => candidate.UserId == userId
                    && candidate.TenantId == targetTenantId
                    && candidate.IsActive
                    && candidate.Tenant.IsActive,
                cancellationToken);
        if (membership is null || !membership.User.IsActive)
        {
            return AuthenticationOutcome.Failure(AuthenticationStatus.InvalidTenant);
        }

        var now = timeProvider.GetUtcNow();
        await RevokeSessionAsync(currentSession.Id, now, "tenant-switch", cancellationToken);
        return AuthenticationOutcome.Success(
            await CreateSessionAsync(membership.User, membership, Guid.NewGuid(), cancellationToken));
    }

    public async Task LogoutAsync(
        string? refreshToken,
        Guid userId,
        Guid currentSessionId,
        CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow();
        IQueryable<UserSession> query = dbContext.UserSessions
            .IgnoreQueryFilters()
            .Where(session => session.UserId == userId && session.RevokedAtUtc == null);

        if (!string.IsNullOrWhiteSpace(refreshToken))
        {
            string tokenHash = RefreshTokenFactory.Hash(refreshToken);
            query = query.Where(session => session.TokenHash == tokenHash);
        }
        else
        {
            query = query.Where(session => session.Id == currentSessionId);
        }

        await query.ExecuteUpdateAsync(
            setters => setters
                .SetProperty(session => session.LastUsedAtUtc, now)
                .SetProperty(session => session.RevokedAtUtc, now)
                .SetProperty(session => session.RevocationReason, "logout"),
            cancellationToken);
    }

    public async Task RevokeAllAsync(Guid userId, CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow();
        await dbContext.UserSessions
            .IgnoreQueryFilters()
            .Where(session => session.UserId == userId && session.RevokedAtUtc == null)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(session => session.LastUsedAtUtc, now)
                    .SetProperty(session => session.RevokedAtUtc, now)
                    .SetProperty(session => session.RevocationReason, "revoke-all"),
                cancellationToken);
    }

    public async Task<IReadOnlyList<TenantOption>> ListAvailableTenantsAsync(
        Guid userId,
        CancellationToken cancellationToken) =>
        await dbContext.TenantMemberships
            .IgnoreQueryFilters()
            .Include(membership => membership.Tenant)
            .Where(membership => membership.UserId == userId
                && membership.IsActive
                && membership.Tenant.IsActive)
            .OrderBy(membership => membership.Tenant.Name)
            .Select(membership => new TenantOption(
                membership.TenantId,
                membership.Tenant.Name,
                membership.Tenant.Slug,
                membership.Role))
            .ToListAsync(cancellationToken);

    private IQueryable<UserSession> SessionQuery() =>
        dbContext.UserSessions
            .IgnoreQueryFilters()
            .Include(session => session.Membership)
                .ThenInclude(membership => membership.User)
            .Include(session => session.Membership)
                .ThenInclude(membership => membership.Tenant);

    private async Task<AuthenticatedIdentity> CreateSessionAsync(
        User user,
        TenantMembership membership,
        Guid familyId,
        CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow();
        string refreshToken = RefreshTokenFactory.Create();
        var session = UserSession.Create(
            Guid.NewGuid(),
            membership.TenantId,
            membership.Id,
            user.Id,
            familyId,
            RefreshTokenFactory.Hash(refreshToken),
            now,
            now.AddDays(identityOptions.Value.RefreshTokenDays));

        tenantContext.SetTenant(membership.TenantId);
        dbContext.UserSessions.Add(session);
        await dbContext.SaveChangesAsync(cancellationToken);

        return BuildIdentity(user, membership.Tenant, membership, session, refreshToken);
    }

    private AuthenticatedIdentity BuildIdentity(
        User user,
        Tenant tenant,
        TenantMembership membership,
        UserSession session,
        string refreshToken)
    {
        (string accessToken, DateTimeOffset expiresAt) = accessTokenIssuer.Issue(
            user,
            tenant,
            membership,
            session);
        return new AuthenticatedIdentity(
            user.Id,
            user.Email,
            user.DisplayName,
            tenant.Id,
            tenant.Name,
            tenant.Slug,
            membership.Id,
            membership.Role,
            Permissions.ForRole(membership.Role),
            session.Id,
            accessToken,
            expiresAt,
            refreshToken,
            session.ExpiresAtUtc);
    }

    private Task<int> RevokeSessionAsync(
        Guid sessionId,
        DateTimeOffset now,
        string reason,
        CancellationToken cancellationToken) =>
        dbContext.UserSessions
            .IgnoreQueryFilters()
            .Where(session => session.Id == sessionId && session.RevokedAtUtc == null)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(session => session.LastUsedAtUtc, now)
                    .SetProperty(session => session.RevokedAtUtc, now)
                    .SetProperty(session => session.RevocationReason, reason),
                cancellationToken);

    private Task<int> RevokeFamilyAsync(
        Guid familyId,
        DateTimeOffset now,
        string reason,
        CancellationToken cancellationToken) =>
        dbContext.UserSessions
            .IgnoreQueryFilters()
            .Where(session => session.FamilyId == familyId && session.RevokedAtUtc == null)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(session => session.LastUsedAtUtc, now)
                    .SetProperty(session => session.RevokedAtUtc, now)
                    .SetProperty(session => session.RevocationReason, reason),
                cancellationToken);

    private static TenantOption ToTenantOption(TenantMembership membership) =>
        new(
            membership.TenantId,
            membership.Tenant.Name,
            membership.Tenant.Slug,
            membership.Role);
}
