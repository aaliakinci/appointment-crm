using AppointmentCrm.Domain.Common;

namespace AppointmentCrm.Domain.Outbox;

public sealed class NotificationDelivery : ITenantOwnedEntity
{
    private NotificationDelivery()
    {
    }

    public Guid Id { get; private set; }

    public Guid TenantId { get; private set; }

    public Guid OutboxMessageId { get; private set; }

    public string MessageType { get; private set; } = string.Empty;

    public string AggregateType { get; private set; } = string.Empty;

    public Guid AggregateId { get; private set; }

    public DateTimeOffset DeliveredAtUtc { get; private set; }

    public string? TraceId { get; private set; }

    public string? CorrelationId { get; private set; }

    public static NotificationDelivery Create(
        Guid id,
        Guid tenantId,
        Guid outboxMessageId,
        string messageType,
        string aggregateType,
        Guid aggregateId,
        DateTimeOffset deliveredAtUtc,
        string? traceId,
        string? correlationId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(messageType);
        ArgumentException.ThrowIfNullOrWhiteSpace(aggregateType);
        if (id == Guid.Empty || tenantId == Guid.Empty || outboxMessageId == Guid.Empty)
        {
            throw new ArgumentException("Notification delivery identifiers cannot be empty.");
        }

        if (messageType.Length > 120
            || aggregateType.Length > 80
            || traceId?.Length > 32
            || correlationId?.Length > 64)
        {
            throw new ArgumentException("Notification delivery metadata exceeds its allowed length.");
        }

        return new NotificationDelivery
        {
            Id = id,
            TenantId = tenantId,
            OutboxMessageId = outboxMessageId,
            MessageType = messageType,
            AggregateType = aggregateType,
            AggregateId = aggregateId,
            DeliveredAtUtc = deliveredAtUtc,
            TraceId = traceId,
            CorrelationId = correlationId,
        };
    }
}
