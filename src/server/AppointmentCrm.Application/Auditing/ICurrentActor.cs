namespace AppointmentCrm.Application.Auditing;

public interface ICurrentActor
{
    bool IsAvailable { get; }

    Guid UserId { get; }

    Guid MembershipId { get; }
}
