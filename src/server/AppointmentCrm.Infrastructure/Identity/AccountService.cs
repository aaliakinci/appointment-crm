using AppointmentCrm.Application.Common;
using AppointmentCrm.Application.Identity;
using AppointmentCrm.Infrastructure.Auditing;
using AppointmentCrm.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AppointmentCrm.Infrastructure.Identity;

internal sealed class AccountService(
    AppointmentCrmDbContext dbContext,
    AuditWriter auditWriter,
    TimeProvider timeProvider) : IAccountService
{
    public async Task<AccountProfile> GetProfileAsync(
        Guid userId,
        CancellationToken cancellationToken) =>
        await dbContext.Users
            .AsNoTracking()
            .Where(user => user.Id == userId)
            .Select(user => new AccountProfile(
                user.Id,
                user.Email,
                user.DisplayName,
                user.UpdatedAtUtc))
            .SingleAsync(cancellationToken);

    public async Task<AccountProfile> UpdateProfileAsync(
        Guid userId,
        string displayName,
        CancellationToken cancellationToken)
    {
        var user = await dbContext.Users.SingleAsync(
            candidate => candidate.Id == userId,
            cancellationToken);
        var now = timeProvider.GetUtcNow();
        try
        {
            user.UpdateProfile(displayName, now);
        }
        catch (ArgumentException exception)
        {
            throw ApplicationValidationException.FromArgument(exception);
        }

        auditWriter.Add("account.profile-updated", "user", user.Id, now);
        await dbContext.SaveChangesAsync(cancellationToken);
        return new AccountProfile(user.Id, user.Email, user.DisplayName, user.UpdatedAtUtc);
    }

    public async Task<IReadOnlyList<AccountSession>> ListActiveSessionsAsync(
        Guid userId,
        Guid currentSessionId,
        CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow();
        return await dbContext.UserSessions
            .AsNoTracking()
            .Where(session => session.UserId == userId
                && session.RevokedAtUtc == null
                && session.ExpiresAtUtc > now)
            .OrderByDescending(session => session.CreatedAtUtc)
            .Select(session => new AccountSession(
                session.Id,
                session.Membership.Tenant.Name,
                session.CreatedAtUtc,
                session.LastUsedAtUtc,
                session.ExpiresAtUtc,
                session.Id == currentSessionId))
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> RevokeSessionAsync(
        Guid userId,
        Guid sessionId,
        CancellationToken cancellationToken)
    {
        var session = await dbContext.UserSessions.SingleOrDefaultAsync(
            candidate => candidate.Id == sessionId
                && candidate.UserId == userId
                && candidate.RevokedAtUtc == null,
            cancellationToken);
        if (session is null)
        {
            return false;
        }

        var now = timeProvider.GetUtcNow();
        session.Revoke(now, "user-revoked");
        auditWriter.Add("account.session-revoked", "session", session.Id, now);
        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }
}
