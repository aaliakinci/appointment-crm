namespace AppointmentCrm.Contracts;

public sealed record CreateEmployeeRequest(
    Guid? UserId,
    string Name,
    string? Email,
    string? Phone,
    IReadOnlyList<Guid> ServiceIds);

public sealed record UpdateEmployeeRequest(
    Guid? UserId,
    string Name,
    string? Email,
    string? Phone);

public sealed record SetEmployeeServicesRequest(IReadOnlyList<Guid> ServiceIds);

public sealed record EmployeeServiceResponse(Guid Id, string Name, bool IsActive);

public sealed record EmployeeResponse(
    Guid Id,
    Guid? UserId,
    string Name,
    string? Email,
    string? Phone,
    bool IsActive,
    IReadOnlyList<EmployeeServiceResponse> Services,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc);

public sealed record EmployeeUserOptionResponse(
    Guid UserId,
    string DisplayName,
    string Email,
    string Role,
    bool IsLinked);
