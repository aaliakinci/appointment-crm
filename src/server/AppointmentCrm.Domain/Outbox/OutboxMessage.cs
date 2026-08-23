using AppointmentCrm.Domain.Common;

namespace AppointmentCrm.Domain.Outbox;

public sealed class OutboxMessage : ITenantOwnedEntity
{
    private OutboxMessage()
    {
    }

    public Guid Id { get; private set; }

    public Guid TenantId { get; private set; }

    public string Type { get; private set; } = string.Empty;

    public string AggregateType { get; private set; } = string.Empty;

    public Guid AggregateId { get; private set; }

    public string PayloadJson { get; private set; } = string.Empty;

    public DateTimeOffset OccurredAtUtc { get; private set; }

    public DateTimeOffset? ProcessedAtUtc { get; private set; }

    public int Attempts { get; private set; }

    public DateTimeOffset? NextAttemptAtUtc { get; private set; }

    public string? LastError { get; private set; }

    public DateTimeOffset? FailedAtUtc { get; private set; }

    public Guid? LeaseId { get; private set; }

    public DateTimeOffset? LockedUntilUtc { get; private set; }

    public string? TraceParent { get; private set; }

    public string? TraceState { get; private set; }

    public string? CorrelationId { get; private set; }

    public static OutboxMessage Create(
        Guid id,
        Guid tenantId,
        string type,
        string aggregateType,
        Guid aggregateId,
        string payloadJson,
        DateTimeOffset occurredAtUtc,
        string? traceParent = null,
        string? traceState = null,
        string? correlationId = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(type);
        ArgumentException.ThrowIfNullOrWhiteSpace(aggregateType);
        ArgumentException.ThrowIfNullOrWhiteSpace(payloadJson);
        if (type.Length > 120 || aggregateType.Length > 80)
        {
            throw new ArgumentException("Outbox message metadata exceeds its allowed length.");
        }

        if (traceParent?.Length > 128
            || traceState?.Length > 512
            || correlationId?.Length > 64)
        {
            throw new ArgumentException("Outbox trace metadata exceeds its allowed length.");
        }

        return new OutboxMessage
        {
            Id = id,
            TenantId = tenantId,
            Type = type,
            AggregateType = aggregateType,
            AggregateId = aggregateId,
            PayloadJson = payloadJson,
            OccurredAtUtc = occurredAtUtc,
            TraceParent = traceParent,
            TraceState = traceState,
            CorrelationId = correlationId,
        };
    }

    public void MarkProcessed(DateTimeOffset processedAtUtc)
    {
        if (ProcessedAtUtc.HasValue)
        {
            return;
        }

        Attempts++;
        ProcessedAtUtc = processedAtUtc;
        NextAttemptAtUtc = null;
        LastError = null;
        FailedAtUtc = null;
        LeaseId = null;
        LockedUntilUtc = null;
    }

    public void MarkFailedAttempt(
        DateTimeOffset attemptedAtUtc,
        DateTimeOffset nextAttemptAtUtc,
        string errorCode,
        int maximumAttempts)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(errorCode);
        if (maximumAttempts <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maximumAttempts));
        }

        if (ProcessedAtUtc.HasValue || FailedAtUtc.HasValue)
        {
            throw new InvalidOperationException("A completed outbox message cannot be retried.");
        }

        Attempts++;
        LastError = errorCode.Length <= 2_000 ? errorCode : errorCode[..2_000];
        LeaseId = null;
        LockedUntilUtc = null;
        if (Attempts >= maximumAttempts)
        {
            FailedAtUtc = attemptedAtUtc;
            NextAttemptAtUtc = null;
            return;
        }

        if (nextAttemptAtUtc <= attemptedAtUtc)
        {
            throw new ArgumentException(
                "The next outbox attempt must be scheduled in the future.",
                nameof(nextAttemptAtUtc));
        }

        NextAttemptAtUtc = nextAttemptAtUtc;
    }
}
