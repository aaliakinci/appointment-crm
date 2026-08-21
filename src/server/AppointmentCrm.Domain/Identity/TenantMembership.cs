using AppointmentCrm.Domain.Common;

namespace AppointmentCrm.Domain.Identity;

public sealed class TenantMembership : ITenantOwnedEntity
{
    private TenantMembership()
    {
    }

    public Guid Id { get; private set; }

    public Guid TenantId { get; private set; }

    public Guid UserId { get; private set; }

    public string Role { get; private set; } = string.Empty;

    public bool IsActive { get; private set; }

    public int AuthorizationVersion { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public DateTimeOffset UpdatedAtUtc { get; private set; }

    public Tenant Tenant { get; private set; } = null!;

    public User User { get; private set; } = null!;

    public ICollection<UserSession> Sessions { get; } = [];

    public static TenantMembership Create(
        Guid id,
        Guid tenantId,
        Guid userId,
        string role,
        DateTimeOffset now)
    {
        if (!TenantRoles.IsDefined(role))
        {
            throw new ArgumentOutOfRangeException(nameof(role), role, "Unknown tenant role.");
        }

        return new TenantMembership
        {
            Id = id,
            TenantId = tenantId,
            UserId = userId,
            Role = role,
            IsActive = true,
            AuthorizationVersion = 1,
            CreatedAtUtc = now,
            UpdatedAtUtc = now,
        };
    }

    public void ChangeRole(string role, DateTimeOffset now)
    {
        if (!TenantRoles.IsDefined(role))
        {
            throw new ArgumentOutOfRangeException(nameof(role), role, "Unknown tenant role.");
        }

        if (string.Equals(Role, role, StringComparison.Ordinal))
        {
            return;
        }

        Role = role;
        AuthorizationVersion++;
        UpdatedAtUtc = now;
    }

    public void SetActive(bool isActive, DateTimeOffset now)
    {
        if (IsActive == isActive)
        {
            return;
        }

        IsActive = isActive;
        AuthorizationVersion++;
        UpdatedAtUtc = now;
    }
}
