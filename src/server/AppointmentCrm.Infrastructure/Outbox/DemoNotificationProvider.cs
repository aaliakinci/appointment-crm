using System.Diagnostics;
using AppointmentCrm.Domain.Outbox;

namespace AppointmentCrm.Infrastructure.Outbox;

internal sealed class DemoNotificationProvider(TimeProvider timeProvider) : INotificationProvider
{
    public ValueTask<NotificationDelivery> DeliverAsync(
        OutboxMessage message,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        string? traceId = Activity.Current?.TraceId.ToHexString()
            ?? TraceIdFromParent(message.TraceParent);
        return ValueTask.FromResult(NotificationDelivery.Create(
            Guid.NewGuid(),
            message.TenantId,
            message.Id,
            message.Type,
            message.AggregateType,
            message.AggregateId,
            timeProvider.GetUtcNow(),
            traceId,
            message.CorrelationId));
    }

    private static string? TraceIdFromParent(string? traceParent)
    {
        if (string.IsNullOrWhiteSpace(traceParent))
        {
            return null;
        }

        string[] parts = traceParent.Split('-');
        return parts.Length == 4 && parts[1].Length == 32 ? parts[1] : null;
    }
}
