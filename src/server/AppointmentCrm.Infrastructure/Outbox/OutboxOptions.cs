namespace AppointmentCrm.Infrastructure.Outbox;

internal sealed class OutboxOptions
{
    public const string SectionName = "Outbox";

    public bool WorkerEnabled { get; init; } = true;

    public int BatchSize { get; init; } = 20;

    public int PollIntervalSeconds { get; init; } = 2;

    public int LeaseSeconds { get; init; } = 30;

    public int MaximumAttempts { get; init; } = 5;

    public int BaseRetryDelaySeconds { get; init; } = 2;

    public int MaximumRetryDelaySeconds { get; init; } = 300;
}
