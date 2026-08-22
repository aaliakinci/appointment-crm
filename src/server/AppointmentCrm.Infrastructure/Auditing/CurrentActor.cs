using AppointmentCrm.Application.Auditing;

namespace AppointmentCrm.Infrastructure.Auditing;

public sealed class CurrentActor : ICurrentActor
{
    private Guid? _userId;
    private Guid? _membershipId;

    public bool IsAvailable => _userId.HasValue && _membershipId.HasValue;

    public Guid UserId => _userId
        ?? throw new InvalidOperationException("An authenticated actor is required.");

    public Guid MembershipId => _membershipId
        ?? throw new InvalidOperationException("An authenticated actor is required.");

    public void SetActor(Guid userId, Guid membershipId)
    {
        if (_userId.HasValue && (_userId != userId || _membershipId != membershipId))
        {
            throw new InvalidOperationException("The current request actor cannot be changed.");
        }

        _userId = userId;
        _membershipId = membershipId;
    }
}
