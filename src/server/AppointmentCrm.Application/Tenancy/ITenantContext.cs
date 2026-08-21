namespace AppointmentCrm.Application.Tenancy;

public interface ITenantContext
{
    bool IsAvailable { get; }

    Guid TenantId { get; }
}
