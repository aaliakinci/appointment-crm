using AppointmentCrm.Domain.Common;

namespace AppointmentCrm.Domain.Auditing;

public sealed class AuditEntry : ITenantOwnedEntity
{
    private AuditEntry()
    {
    }

    public Guid Id { get; private set; }

    public Guid TenantId { get; private set; }

    public Guid ActorUserId { get; private set; }

    public Guid ActorMembershipId { get; private set; }

    public string Action { get; private set; } = string.Empty;

    public string TargetType { get; private set; } = string.Empty;

    public Guid TargetId { get; private set; }

    public string? Summary { get; private set; }

    public DateTimeOffset OccurredAtUtc { get; private set; }

    public static AuditEntry Create(
        Guid id,
        Guid tenantId,
        Guid actorUserId,
        Guid actorMembershipId,
        string action,
        string targetType,
        Guid targetId,
        string? summary,
        DateTimeOffset occurredAtUtc)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(action);
        ArgumentException.ThrowIfNullOrWhiteSpace(targetType);
        if (action.Length > 80 || targetType.Length > 80 || summary?.Length > 1_000)
        {
            throw new ArgumentException("Audit entry fields exceed their allowed length.");
        }

        return new AuditEntry
        {
            Id = id,
            TenantId = tenantId,
            ActorUserId = actorUserId,
            ActorMembershipId = actorMembershipId,
            Action = action,
            TargetType = targetType,
            TargetId = targetId,
            Summary = summary,
            OccurredAtUtc = occurredAtUtc,
        };
    }
}
