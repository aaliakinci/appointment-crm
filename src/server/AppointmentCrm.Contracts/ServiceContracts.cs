namespace AppointmentCrm.Contracts;

public sealed record CreateServiceRequest(
    string Name,
    int DurationMinutes,
    decimal Price,
    string Currency);

public sealed record UpdateServiceRequest(
    string Name,
    int DurationMinutes,
    decimal Price,
    string Currency);

public sealed record ServiceResponse(
    Guid Id,
    string Name,
    int DurationMinutes,
    decimal Price,
    string Currency,
    bool IsActive,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc);
