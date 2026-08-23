using AppointmentCrm.Domain.Outbox;

namespace AppointmentCrm.UnitTests.Outbox;

public sealed class OutboxMessageTests
{
    private static readonly DateTimeOffset Now = new(2026, 8, 23, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public void FailedAttempts_UseBoundedErrorCodeAndBecomeTerminal()
    {
        OutboxMessage message = CreateMessage();

        message.MarkFailedAttempt(Now, Now.AddSeconds(2), new string('x', 2_100), 2);

        Assert.Equal(1, message.Attempts);
        Assert.Equal(2_000, message.LastError?.Length);
        Assert.Equal(Now.AddSeconds(2), message.NextAttemptAtUtc);
        Assert.Null(message.FailedAtUtc);

        message.MarkFailedAttempt(Now.AddSeconds(2), Now.AddSeconds(4), "provider.failed", 2);

        Assert.Equal(2, message.Attempts);
        Assert.Equal(Now.AddSeconds(2), message.FailedAtUtc);
        Assert.Null(message.NextAttemptAtUtc);
        Assert.Null(message.LeaseId);
        Assert.Null(message.LockedUntilUtc);
    }

    [Fact]
    public void MarkProcessed_IsIdempotent()
    {
        OutboxMessage message = CreateMessage();

        message.MarkProcessed(Now.AddMinutes(1));
        message.MarkProcessed(Now.AddMinutes(2));

        Assert.Equal(1, message.Attempts);
        Assert.Equal(Now.AddMinutes(1), message.ProcessedAtUtc);
        Assert.Null(message.LastError);
    }

    [Fact]
    public void NotificationDelivery_DoesNotContainTheOutboxPayload()
    {
        OutboxMessage message = CreateMessage();
        NotificationDelivery delivery = NotificationDelivery.Create(
            Guid.NewGuid(),
            message.TenantId,
            message.Id,
            message.Type,
            message.AggregateType,
            message.AggregateId,
            Now,
            "0123456789abcdef0123456789abcdef",
            "correlation-id");

        Assert.Equal(message.Id, delivery.OutboxMessageId);
        Assert.DoesNotContain(
            typeof(NotificationDelivery).GetProperties(),
            property => property.Name.Contains("Payload", StringComparison.Ordinal));
    }

    private static OutboxMessage CreateMessage() => OutboxMessage.Create(
        Guid.NewGuid(),
        Guid.NewGuid(),
        "appointment.created",
        "appointment",
        Guid.NewGuid(),
        "{\"appointmentId\":\"safe\"}",
        Now,
        "00-0123456789abcdef0123456789abcdef-0123456789abcdef-01",
        null,
        "correlation-id");
}
