using AppointmentCrm.Domain.Common;

namespace AppointmentCrm.Domain.Identity;

public sealed class UserSession : ITenantOwnedEntity
{
    private UserSession()
    {
    }

    public Guid Id { get; private set; }

    public Guid TenantId { get; private set; }

    public Guid MembershipId { get; private set; }

    public Guid UserId { get; private set; }

    public Guid FamilyId { get; private set; }

    public string TokenHash { get; private set; } = string.Empty;

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public DateTimeOffset ExpiresAtUtc { get; private set; }

    public DateTimeOffset? LastUsedAtUtc { get; private set; }

    public DateTimeOffset? RevokedAtUtc { get; private set; }

    public string? RevocationReason { get; private set; }

    public Guid? ReplacedBySessionId { get; private set; }

    public TenantMembership Membership { get; private set; } = null!;

    public static UserSession Create(
        Guid id,
        Guid tenantId,
        Guid membershipId,
        Guid userId,
        Guid familyId,
        string tokenHash,
        DateTimeOffset now,
        DateTimeOffset expiresAt)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tokenHash);

        return new UserSession
        {
            Id = id,
            TenantId = tenantId,
            MembershipId = membershipId,
            UserId = userId,
            FamilyId = familyId,
            TokenHash = tokenHash,
            CreatedAtUtc = now,
            ExpiresAtUtc = expiresAt,
        };
    }

    public void Revoke(
        DateTimeOffset now,
        string reason,
        Guid? replacedBySessionId = null)
    {
        if (RevokedAtUtc is not null)
        {
            return;
        }

        LastUsedAtUtc = now;
        RevokedAtUtc = now;
        RevocationReason = reason;
        ReplacedBySessionId = replacedBySessionId;
    }
}
