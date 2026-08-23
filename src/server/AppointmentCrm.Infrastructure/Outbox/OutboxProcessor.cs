using System.Diagnostics;
using AppointmentCrm.Application.Observability;
using AppointmentCrm.Domain.Outbox;
using AppointmentCrm.Infrastructure.Persistence;
using AppointmentCrm.Infrastructure.Tenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AppointmentCrm.Infrastructure.Outbox;

internal sealed class OutboxProcessor(
    IServiceScopeFactory scopeFactory,
    TimeProvider timeProvider,
    IOptions<OutboxOptions> options,
    ILogger<OutboxProcessor> logger)
{
    public async Task<OutboxBatchResult> ProcessBatchAsync(CancellationToken cancellationToken)
    {
        DateTimeOffset now = timeProvider.GetUtcNow();
        Guid leaseId = Guid.NewGuid();
        IReadOnlyList<OutboxClaim> claims = await ClaimAsync(leaseId, now, cancellationToken);
        int processed = 0;
        int failed = 0;
        int terminalFailures = 0;
        foreach (OutboxClaim claim in claims)
        {
            OutboxProcessOutcome outcome = await ProcessOneAsync(
                claim,
                leaseId,
                cancellationToken);
            processed += outcome is OutboxProcessOutcome.Processed
                or OutboxProcessOutcome.AlreadyDelivered
                ? 1
                : 0;
            failed += outcome is OutboxProcessOutcome.RetryScheduled
                or OutboxProcessOutcome.TerminalFailure
                ? 1
                : 0;
            terminalFailures += outcome == OutboxProcessOutcome.TerminalFailure ? 1 : 0;
        }

        return new OutboxBatchResult(claims.Count, processed, failed, terminalFailures);
    }

    private async Task<IReadOnlyList<OutboxClaim>> ClaimAsync(
        Guid leaseId,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        await using AsyncServiceScope scope = scopeFactory.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppointmentCrmDbContext>();
        OutboxOptions settings = options.Value;
        List<OutboxClaim> candidates = await dbContext.OutboxMessages
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(message => message.ProcessedAtUtc == null
                && message.FailedAtUtc == null
                && (message.NextAttemptAtUtc == null || message.NextAttemptAtUtc <= now)
                && (message.LockedUntilUtc == null || message.LockedUntilUtc <= now))
            .OrderBy(message => message.OccurredAtUtc)
            .Select(message => new OutboxClaim(message.Id, message.TenantId))
            .Take(settings.BatchSize)
            .ToListAsync(cancellationToken);
        var claimed = new List<OutboxClaim>(candidates.Count);
        DateTimeOffset lockedUntil = now.AddSeconds(settings.LeaseSeconds);
        foreach (OutboxClaim candidate in candidates)
        {
            int updated = await dbContext.OutboxMessages
                .IgnoreQueryFilters()
                .Where(message => message.Id == candidate.MessageId
                    && message.ProcessedAtUtc == null
                    && message.FailedAtUtc == null
                    && (message.NextAttemptAtUtc == null || message.NextAttemptAtUtc <= now)
                    && (message.LockedUntilUtc == null || message.LockedUntilUtc <= now))
                .ExecuteUpdateAsync(
                    setters => setters
                        .SetProperty(message => message.LeaseId, leaseId)
                        .SetProperty(message => message.LockedUntilUtc, lockedUntil),
                    cancellationToken);
            if (updated == 1)
            {
                claimed.Add(candidate);
            }
        }

        return claimed;
    }

    private async Task<OutboxProcessOutcome> ProcessOneAsync(
        OutboxClaim claim,
        Guid leaseId,
        CancellationToken cancellationToken)
    {
        await using AsyncServiceScope scope = scopeFactory.CreateAsyncScope();
        scope.ServiceProvider.GetRequiredService<TenantContext>().SetTenant(claim.TenantId);
        var dbContext = scope.ServiceProvider.GetRequiredService<AppointmentCrmDbContext>();
        OutboxMessage? message = await dbContext.OutboxMessages.SingleOrDefaultAsync(
            candidate => candidate.Id == claim.MessageId && candidate.LeaseId == leaseId,
            cancellationToken);
        if (message is null)
        {
            return OutboxProcessOutcome.LeaseLost;
        }

        using Activity? activity = StartActivity(message);
        activity?.SetTag("messaging.system", "appointment-crm-outbox");
        activity?.SetTag("messaging.operation.type", "process");
        activity?.SetTag("messaging.message.id", message.Id);
        activity?.SetTag("messaging.message.type", message.Type);
        activity?.SetTag("tenant.id", message.TenantId);
        if (!string.IsNullOrWhiteSpace(message.CorrelationId))
        {
            activity?.SetTag("app.correlation_id", message.CorrelationId);
            activity?.SetBaggage("app.correlation_id", message.CorrelationId);
        }

        var stopwatch = Stopwatch.StartNew();
        try
        {
            bool alreadyDelivered = await dbContext.NotificationDeliveries.AnyAsync(
                delivery => delivery.OutboxMessageId == message.Id,
                cancellationToken);
            if (!alreadyDelivered)
            {
                var provider = scope.ServiceProvider.GetRequiredService<INotificationProvider>();
                NotificationDelivery delivery = await provider.DeliverAsync(
                    message,
                    cancellationToken);
                dbContext.NotificationDeliveries.Add(delivery);
            }

            message.MarkProcessed(timeProvider.GetUtcNow());
            await dbContext.SaveChangesAsync(cancellationToken);
            stopwatch.Stop();
            string outcome = alreadyDelivered ? "already_delivered" : "processed";
            AppointmentCrmTelemetry.RecordOutboxAttempt(
                message.Type,
                outcome,
                stopwatch.Elapsed.TotalMilliseconds);
            logger.LogInformation(
                "Outbox message delivered. MessageId={MessageId} TenantId={TenantId} MessageType={MessageType} Outcome={Outcome}",
                message.Id,
                message.TenantId,
                message.Type,
                outcome);
            return alreadyDelivered
                ? OutboxProcessOutcome.AlreadyDelivered
                : OutboxProcessOutcome.Processed;
        }
        catch (Exception exception) when (
            exception is not OperationCanceledException || !cancellationToken.IsCancellationRequested)
        {
            stopwatch.Stop();
            dbContext.ChangeTracker.Clear();
            OutboxMessage? failedMessage = await dbContext.OutboxMessages.SingleOrDefaultAsync(
                candidate => candidate.Id == claim.MessageId && candidate.LeaseId == leaseId,
                cancellationToken);
            if (failedMessage is null)
            {
                return OutboxProcessOutcome.LeaseLost;
            }

            string errorCode = exception.GetType().FullName ?? exception.GetType().Name;
            DateTimeOffset attemptedAt = timeProvider.GetUtcNow();
            failedMessage.MarkFailedAttempt(
                attemptedAt,
                attemptedAt.Add(NextDelay(failedMessage.Attempts + 1)),
                errorCode,
                options.Value.MaximumAttempts);
            await dbContext.SaveChangesAsync(cancellationToken);
            bool terminal = failedMessage.FailedAtUtc.HasValue;
            activity?.SetStatus(ActivityStatusCode.Error, errorCode);
            activity?.SetTag("error.type", errorCode);
            AppointmentCrmTelemetry.RecordOutboxAttempt(
                failedMessage.Type,
                terminal ? "terminal_failure" : "retry_scheduled",
                stopwatch.Elapsed.TotalMilliseconds,
                terminal);
            logger.LogWarning(
                "Outbox delivery failed. MessageId={MessageId} TenantId={TenantId} MessageType={MessageType} ErrorType={ErrorType} Attempt={Attempt} Terminal={Terminal}",
                failedMessage.Id,
                failedMessage.TenantId,
                failedMessage.Type,
                errorCode,
                failedMessage.Attempts,
                terminal);
            return terminal
                ? OutboxProcessOutcome.TerminalFailure
                : OutboxProcessOutcome.RetryScheduled;
        }
    }

    private TimeSpan NextDelay(int attempt)
    {
        OutboxOptions settings = options.Value;
        double seconds = settings.BaseRetryDelaySeconds * Math.Pow(2, Math.Max(0, attempt - 1));
        return TimeSpan.FromSeconds(Math.Min(seconds, settings.MaximumRetryDelaySeconds));
    }

    private static Activity? StartActivity(OutboxMessage message)
    {
        if (ActivityContext.TryParse(
            message.TraceParent,
            message.TraceState,
            isRemote: true,
            out ActivityContext parentContext))
        {
            return AppointmentCrmTelemetry.ActivitySource.StartActivity(
                "outbox.notification.deliver",
                ActivityKind.Consumer,
                parentContext);
        }

        return AppointmentCrmTelemetry.ActivitySource.StartActivity(
            "outbox.notification.deliver",
            ActivityKind.Consumer);
    }

    private sealed record OutboxClaim(Guid MessageId, Guid TenantId);
}

internal sealed record OutboxBatchResult(
    int Claimed,
    int Processed,
    int Failed,
    int TerminalFailures);

internal enum OutboxProcessOutcome
{
    Processed,
    AlreadyDelivered,
    RetryScheduled,
    TerminalFailure,
    LeaseLost,
}
