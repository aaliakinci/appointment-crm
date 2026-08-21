namespace AppointmentCrm.Contracts;

public sealed record SystemStatusResponse(
    string Service,
    string Status,
    DateTimeOffset TimestampUtc,
    string TraceId);
