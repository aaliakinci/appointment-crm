using AppointmentCrm.Domain.Common;
using AppointmentCrm.Domain.Identity;

namespace AppointmentCrm.Domain.Appointments;

public sealed class AppointmentStatusHistory : ITenantOwnedEntity
{
    private AppointmentStatusHistory()
    {
    }

    public Guid Id { get; private set; }

    public Guid TenantId { get; private set; }

    public Guid AppointmentId { get; private set; }

    public AppointmentStatus? FromStatus { get; private set; }

    public AppointmentStatus ToStatus { get; private set; }

    public Guid ActorUserId { get; private set; }

    public Guid ActorMembershipId { get; private set; }

    public string? Reason { get; private set; }

    public DateTimeOffset OccurredAtUtc { get; private set; }

    public Appointment Appointment { get; private set; } = null!;

    public TenantMembership ActorMembership { get; private set; } = null!;

    internal static AppointmentStatusHistory Create(
        Guid id,
        Guid tenantId,
        Guid appointmentId,
        AppointmentStatus? fromStatus,
        AppointmentStatus toStatus,
        Guid actorUserId,
        Guid actorMembershipId,
        string? reason,
        DateTimeOffset occurredAtUtc) =>
        new()
        {
            Id = id,
            TenantId = tenantId,
            AppointmentId = appointmentId,
            FromStatus = fromStatus,
            ToStatus = toStatus,
            ActorUserId = actorUserId,
            ActorMembershipId = actorMembershipId,
            Reason = reason,
            OccurredAtUtc = occurredAtUtc,
        };
}
