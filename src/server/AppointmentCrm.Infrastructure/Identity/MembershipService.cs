using AppointmentCrm.Application.Common;
using AppointmentCrm.Application.Identity;
using AppointmentCrm.Application.Tenancy;
using AppointmentCrm.Domain.Identity;
using AppointmentCrm.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AppointmentCrm.Infrastructure.Identity;

internal sealed class MembershipService(
    AppointmentCrmDbContext dbContext,
    ITenantContext tenantContext,
    TimeProvider timeProvider) : IMembershipService
{
    public async Task<IReadOnlyList<MembershipSummary>> ListAsync(
        CancellationToken cancellationToken) =>
        await dbContext.TenantMemberships
            .Include(membership => membership.User)
            .OrderBy(membership => membership.User.DisplayName)
            .ThenBy(membership => membership.User.Email)
            .Select(membership => ToSummary(membership))
            .ToListAsync(cancellationToken);

    public Task<MembershipSummary?> GetAsync(
        Guid membershipId,
        CancellationToken cancellationToken) =>
        dbContext.TenantMemberships
            .Include(membership => membership.User)
            .Where(membership => membership.Id == membershipId)
            .Select(membership => ToSummary(membership))
            .SingleOrDefaultAsync(cancellationToken);

    public async Task<MembershipSummary?> UpdateAsync(
        Guid membershipId,
        string role,
        bool isActive,
        CancellationToken cancellationToken)
    {
        if (!TenantRoles.IsDefined(role))
        {
            throw new ApplicationValidationException(
                CommonErrorCodes.ValidationFailed,
                new Dictionary<string, string[]>
                {
                    [nameof(role)] = ["Role is not valid."],
                });
        }

        var membership = await dbContext.TenantMemberships
            .Include(candidate => candidate.User)
            .SingleOrDefaultAsync(candidate => candidate.Id == membershipId, cancellationToken);
        if (membership is null)
        {
            return null;
        }

        bool ownerAccessRemoved = string.Equals(membership.Role, TenantRoles.Owner, StringComparison.Ordinal)
            && (!string.Equals(role, TenantRoles.Owner, StringComparison.Ordinal) || !isActive);
        if (ownerAccessRemoved)
        {
            bool anotherOwnerExists = await dbContext.TenantMemberships.AnyAsync(
                candidate => candidate.Id != membership.Id
                    && candidate.Role == TenantRoles.Owner
                    && candidate.IsActive,
                cancellationToken);
            if (!anotherOwnerExists)
            {
                throw new ApplicationConflictException(
                    IdentityErrorCodes.LastActiveOwner,
                    "A tenant must retain at least one active owner.");
            }
        }

        var now = timeProvider.GetUtcNow();
        int previousVersion = membership.AuthorizationVersion;
        membership.ChangeRole(role, now);
        membership.SetActive(isActive, now);
        await dbContext.SaveChangesAsync(cancellationToken);

        if (membership.AuthorizationVersion != previousVersion)
        {
            await RevokeMembershipSessionsAsync(membership.Id, now, cancellationToken);
        }

        return ToSummary(membership);
    }

    public async Task<bool> ArchiveAsync(
        Guid membershipId,
        CancellationToken cancellationToken)
    {
        var membership = await dbContext.TenantMemberships
            .SingleOrDefaultAsync(candidate => candidate.Id == membershipId, cancellationToken);
        if (membership is null)
        {
            return false;
        }

        if (string.Equals(membership.Role, TenantRoles.Owner, StringComparison.Ordinal)
            && membership.IsActive)
        {
            bool anotherOwnerExists = await dbContext.TenantMemberships.AnyAsync(
                candidate => candidate.Id != membership.Id
                    && candidate.Role == TenantRoles.Owner
                    && candidate.IsActive,
                cancellationToken);
            if (!anotherOwnerExists)
            {
                throw new ApplicationConflictException(
                    IdentityErrorCodes.LastActiveOwner,
                    "A tenant must retain at least one active owner.");
            }
        }

        var now = timeProvider.GetUtcNow();
        membership.SetActive(false, now);
        await dbContext.SaveChangesAsync(cancellationToken);
        await RevokeMembershipSessionsAsync(membership.Id, now, cancellationToken);
        return true;
    }

    public async Task<MembershipReport> GetReportAsync(CancellationToken cancellationToken)
    {
        int total = await dbContext.TenantMemberships.CountAsync(cancellationToken);
        int active = await dbContext.TenantMemberships.CountAsync(
            membership => membership.IsActive,
            cancellationToken);
        var byRole = await dbContext.TenantMemberships
            .GroupBy(membership => membership.Role)
            .Select(group => new { Role = group.Key, Count = group.Count() })
            .ToDictionaryAsync(item => item.Role, item => item.Count, cancellationToken);

        return new MembershipReport(total, active, byRole);
    }

    private Task<int> RevokeMembershipSessionsAsync(
        Guid membershipId,
        DateTimeOffset now,
        CancellationToken cancellationToken) =>
        dbContext.UserSessions
            .Where(session => session.TenantId == tenantContext.TenantId
                && session.MembershipId == membershipId
                && session.RevokedAtUtc == null)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(session => session.LastUsedAtUtc, now)
                    .SetProperty(session => session.RevokedAtUtc, now)
                    .SetProperty(session => session.RevocationReason, "membership-changed"),
                cancellationToken);

    private static MembershipSummary ToSummary(TenantMembership membership) =>
        new(
            membership.Id,
            membership.UserId,
            membership.User.Email,
            membership.User.DisplayName,
            membership.Role,
            membership.IsActive,
            membership.UpdatedAtUtc);
}
