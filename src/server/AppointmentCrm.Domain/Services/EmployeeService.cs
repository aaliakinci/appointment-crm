using AppointmentCrm.Domain.Common;

namespace AppointmentCrm.Domain.Services;

public sealed class EmployeeService : ITenantOwnedEntity
{
    private EmployeeService()
    {
    }

    public Guid TenantId { get; private set; }

    public Guid EmployeeId { get; private set; }

    public Guid ServiceId { get; private set; }

    public DateTimeOffset AssignedAtUtc { get; private set; }

    public Employee Employee { get; private set; } = null!;

    public ServiceOffering Service { get; private set; } = null!;

    public static EmployeeService Create(
        Guid tenantId,
        Guid employeeId,
        Guid serviceId,
        DateTimeOffset now) =>
        new()
        {
            TenantId = tenantId,
            EmployeeId = employeeId,
            ServiceId = serviceId,
            AssignedAtUtc = now,
        };
}
