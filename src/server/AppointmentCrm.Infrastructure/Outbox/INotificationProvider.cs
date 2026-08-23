using AppointmentCrm.Domain.Outbox;

namespace AppointmentCrm.Infrastructure.Outbox;

internal interface INotificationProvider
{
    ValueTask<NotificationDelivery> DeliverAsync(
        OutboxMessage message,
        CancellationToken cancellationToken);
}
