using AppointmentCrm.Domain.Common;
using AppointmentCrm.Domain.Customers;
using AppointmentCrm.Domain.Services;

namespace AppointmentCrm.Domain.Appointments;

public enum AppointmentStatus
{
    Scheduled,
    Confirmed,
    Completed,
    Cancelled,
    NoShow,
}

public sealed class Appointment : ITenantOwnedEntity
{
    private readonly List<AppointmentStatusHistory> _statusHistory = [];

    private Appointment()
    {
    }

    public Guid Id { get; private set; }

    public Guid TenantId { get; private set; }

    public Guid CustomerId { get; private set; }

    public Guid EmployeeId { get; private set; }

    public Guid ServiceId { get; private set; }

    public AppointmentStatus Status { get; private set; }

    public DateTimeOffset StartsAtUtc { get; private set; }

    public DateTimeOffset EndsAtUtc { get; private set; }

    public string ServiceName { get; private set; } = string.Empty;

    public int ServiceDurationMinutes { get; private set; }

    public decimal ServicePrice { get; private set; }

    public string ServiceCurrency { get; private set; } = string.Empty;

    public string? Notes { get; private set; }

    public long Revision { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public DateTimeOffset UpdatedAtUtc { get; private set; }

    public Customer Customer { get; private set; } = null!;

    public Employee Employee { get; private set; } = null!;

    public ServiceOffering Service { get; private set; } = null!;

    public IReadOnlyCollection<AppointmentStatusHistory> StatusHistory => _statusHistory;

    public static Appointment Create(
        Guid id,
        Guid tenantId,
        Guid customerId,
        Guid employeeId,
        Guid serviceId,
        DateTimeOffset startsAtUtc,
        string serviceName,
        int serviceDurationMinutes,
        decimal servicePrice,
        string serviceCurrency,
        string? notes,
        Guid actorUserId,
        Guid actorMembershipId,
        DateTimeOffset now)
    {
        ValidateIdentifiers(id, tenantId, customerId, employeeId, serviceId);
        ValidateUtc(startsAtUtc, nameof(startsAtUtc));
        ValidateServiceSnapshot(
            serviceName,
            serviceDurationMinutes,
            servicePrice,
            serviceCurrency);

        string? normalizedNotes = NormalizeOptional(notes, 1_000, nameof(notes));
        var appointment = new Appointment
        {
            Id = id,
            TenantId = tenantId,
            CustomerId = customerId,
            EmployeeId = employeeId,
            ServiceId = serviceId,
            Status = AppointmentStatus.Scheduled,
            StartsAtUtc = startsAtUtc,
            EndsAtUtc = startsAtUtc.AddMinutes(serviceDurationMinutes),
            ServiceName = serviceName.Trim(),
            ServiceDurationMinutes = serviceDurationMinutes,
            ServicePrice = servicePrice,
            ServiceCurrency = serviceCurrency.Trim().ToUpperInvariant(),
            Notes = normalizedNotes,
            Revision = 1,
            CreatedAtUtc = now,
            UpdatedAtUtc = now,
        };
        appointment._statusHistory.Add(AppointmentStatusHistory.Create(
            Guid.NewGuid(),
            tenantId,
            id,
            fromStatus: null,
            AppointmentStatus.Scheduled,
            actorUserId,
            actorMembershipId,
            reason: null,
            now));
        return appointment;
    }

    public void Reschedule(
        DateTimeOffset startsAtUtc,
        long expectedRevision,
        DateTimeOffset now)
    {
        EnsureRevision(expectedRevision);
        ValidateUtc(startsAtUtc, nameof(startsAtUtc));
        if (Status is not AppointmentStatus.Scheduled and not AppointmentStatus.Confirmed)
        {
            throw new InvalidOperationException(
                "Only scheduled or confirmed appointments can be rescheduled.");
        }

        StartsAtUtc = startsAtUtc;
        EndsAtUtc = startsAtUtc.AddMinutes(ServiceDurationMinutes);
        AdvanceRevision(now);
    }

    public AppointmentStatusHistory TransitionTo(
        AppointmentStatus targetStatus,
        long expectedRevision,
        string? reason,
        Guid actorUserId,
        Guid actorMembershipId,
        DateTimeOffset now)
    {
        EnsureRevision(expectedRevision);
        if (!CanTransition(Status, targetStatus))
        {
            throw new InvalidOperationException(
                $"Appointment status cannot transition from {Status} to {targetStatus}.");
        }

        if (targetStatus is AppointmentStatus.Completed or AppointmentStatus.NoShow
            && now < StartsAtUtc)
        {
            throw new InvalidOperationException(
                "An appointment cannot be completed or marked as no-show before it starts.");
        }

        string? normalizedReason = NormalizeOptional(reason, 500, nameof(reason));
        AppointmentStatus previous = Status;
        Status = targetStatus;
        AdvanceRevision(now);
        AppointmentStatusHistory history = AppointmentStatusHistory.Create(
            Guid.NewGuid(),
            TenantId,
            Id,
            previous,
            targetStatus,
            actorUserId,
            actorMembershipId,
            normalizedReason,
            now);
        _statusHistory.Add(history);
        return history;
    }

    public static bool OccupiesTime(AppointmentStatus status) =>
        status is AppointmentStatus.Scheduled
            or AppointmentStatus.Confirmed
            or AppointmentStatus.Completed
            or AppointmentStatus.NoShow;

    public static bool CanTransition(AppointmentStatus from, AppointmentStatus to) =>
        (from, to) switch
        {
            (AppointmentStatus.Scheduled, AppointmentStatus.Confirmed) => true,
            (AppointmentStatus.Scheduled, AppointmentStatus.Cancelled) => true,
            (AppointmentStatus.Scheduled, AppointmentStatus.NoShow) => true,
            (AppointmentStatus.Confirmed, AppointmentStatus.Completed) => true,
            (AppointmentStatus.Confirmed, AppointmentStatus.Cancelled) => true,
            (AppointmentStatus.Confirmed, AppointmentStatus.NoShow) => true,
            _ => false,
        };

    private void EnsureRevision(long expectedRevision)
    {
        if (expectedRevision != Revision)
        {
            throw new InvalidOperationException("The appointment revision is stale.");
        }
    }

    private void AdvanceRevision(DateTimeOffset now)
    {
        Revision = checked(Revision + 1);
        UpdatedAtUtc = now;
    }

    private static void ValidateIdentifiers(params Guid[] identifiers)
    {
        if (identifiers.Any(identifier => identifier == Guid.Empty))
        {
            throw new ArgumentException("Appointment identifiers cannot be empty.");
        }
    }

    private static void ValidateUtc(DateTimeOffset value, string parameterName)
    {
        if (value.Offset != TimeSpan.Zero)
        {
            throw new ArgumentException("Appointment boundaries must be UTC instants.", parameterName);
        }
    }

    private static void ValidateServiceSnapshot(
        string name,
        int durationMinutes,
        decimal price,
        string currency)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        if (name.Trim().Length > 160)
        {
            throw new ArgumentException("Service snapshot name cannot exceed 160 characters.", nameof(name));
        }

        if (durationMinutes is < 5 or > 480 || durationMinutes % 5 != 0)
        {
            throw new ArgumentOutOfRangeException(nameof(durationMinutes));
        }

        if (price is < 0 or > 1_000_000 || decimal.Round(price, 2) != price)
        {
            throw new ArgumentOutOfRangeException(nameof(price));
        }

        _ = ServiceOffering.NormalizeCurrency(currency);
    }

    private static string? NormalizeOptional(string? value, int maximumLength, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        string normalized = value.Trim();
        if (normalized.Length > maximumLength)
        {
            throw new ArgumentException(
                $"Value cannot exceed {maximumLength} characters.",
                parameterName);
        }

        return normalized;
    }
}
