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

    public static OutboxMessage Create(
        Guid id,
        Guid tenantId,
        string type,
        string aggregateType,
        Guid aggregateId,
        string payloadJson,
        DateTimeOffset occurredAtUtc)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(type);
        ArgumentException.ThrowIfNullOrWhiteSpace(aggregateType);
        ArgumentException.ThrowIfNullOrWhiteSpace(payloadJson);
        if (type.Length > 120 || aggregateType.Length > 80)
        {
            throw new ArgumentException("Outbox message metadata exceeds its allowed length.");
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
        };
    }
}
