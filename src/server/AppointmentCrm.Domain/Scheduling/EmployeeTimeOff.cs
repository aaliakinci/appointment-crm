using AppointmentCrm.Domain.Common;

namespace AppointmentCrm.Domain.Scheduling;

public sealed class EmployeeTimeOff : ITenantOwnedEntity
{
    private EmployeeTimeOff()
    {
    }

    public Guid Id { get; private set; }

    public Guid TenantId { get; private set; }

    public Guid EmployeeId { get; private set; }

    public DateTimeOffset StartUtc { get; private set; }

    public DateTimeOffset EndUtc { get; private set; }

    public string? Reason { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public static EmployeeTimeOff Create(
        Guid id,
        Guid tenantId,
        Guid employeeId,
        DateTimeOffset startUtc,
        DateTimeOffset endUtc,
        string? reason,
        DateTimeOffset now)
    {
        if (startUtc.Offset != TimeSpan.Zero || endUtc.Offset != TimeSpan.Zero)
        {
            throw new ArgumentException("Time-off boundaries must be UTC instants.");
        }

        if (startUtc >= endUtc)
        {
            throw new ArgumentException("Time off must end after it starts.");
        }

        string? trimmedReason = string.IsNullOrWhiteSpace(reason) ? null : reason.Trim();
        if (trimmedReason?.Length > 500)
        {
            throw new ArgumentException("Time-off reason cannot exceed 500 characters.", nameof(reason));
        }

        return new EmployeeTimeOff
        {
            Id = id,
            TenantId = tenantId,
            EmployeeId = employeeId,
            StartUtc = startUtc,
            EndUtc = endUtc,
            Reason = trimmedReason,
            CreatedAtUtc = now,
        };
    }
}
