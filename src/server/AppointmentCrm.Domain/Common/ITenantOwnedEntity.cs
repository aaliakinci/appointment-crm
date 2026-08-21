namespace AppointmentCrm.Domain.Common;

public interface ITenantOwnedEntity
{
    Guid TenantId { get; }
}
