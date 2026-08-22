namespace AppointmentCrm.Contracts;

public sealed record CreateCustomerRequest(
    string Name,
    string? Email,
    string? Phone,
    string? Notes);

public sealed record UpdateCustomerRequest(
    string Name,
    string? Email,
    string? Phone,
    string? Notes);

public sealed record CustomerResponse(
    Guid Id,
    string Name,
    string? Email,
    string? Phone,
    string? Notes,
    DateTimeOffset? ArchivedAtUtc,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc);
