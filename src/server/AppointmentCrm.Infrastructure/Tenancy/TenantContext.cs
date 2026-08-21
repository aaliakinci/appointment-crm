using AppointmentCrm.Application.Tenancy;

namespace AppointmentCrm.Infrastructure.Tenancy;

public sealed class TenantContext : ITenantContext
{
    private Guid? _tenantId;

    public bool IsAvailable => _tenantId.HasValue;

    public Guid TenantId => _tenantId
        ?? throw new InvalidOperationException("An authenticated tenant context is required.");

    internal void SetTenant(Guid tenantId)
    {
        if (tenantId == Guid.Empty)
        {
            throw new ArgumentException("Tenant id cannot be empty.", nameof(tenantId));
        }

        _tenantId = tenantId;
    }
}
