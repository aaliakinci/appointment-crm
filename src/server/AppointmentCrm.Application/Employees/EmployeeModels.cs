using AppointmentCrm.Application.Common;

namespace AppointmentCrm.Application.Employees;

public sealed record EmployeeServiceSummary(Guid Id, string Name, bool IsActive);

public sealed record EmployeeSummary(
    Guid Id,
    Guid? UserId,
    string Name,
    string? Email,
    string? Phone,
    bool IsActive,
    IReadOnlyList<EmployeeServiceSummary> Services,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc);

public sealed record EmployeeInput(
    Guid? UserId,
    string Name,
    string? Email,
    string? Phone);

public sealed record EmployeeUserOption(
    Guid UserId,
    string DisplayName,
    string Email,
    string Role,
    bool IsLinked);

public interface IEmployeeManagementService
{
    Task<PagedResult<EmployeeSummary>> ListAsync(
        PageRequest request,
        bool? isActive,
        Guid? serviceId,
        CancellationToken cancellationToken);

    Task<EmployeeSummary?> GetAsync(Guid employeeId, CancellationToken cancellationToken);

    Task<EmployeeSummary> CreateAsync(
        EmployeeInput input,
        IReadOnlyCollection<Guid> serviceIds,
        CancellationToken cancellationToken);

    Task<EmployeeSummary?> UpdateAsync(
        Guid employeeId,
        EmployeeInput input,
        CancellationToken cancellationToken);

    Task<EmployeeSummary?> SetActiveAsync(
        Guid employeeId,
        bool isActive,
        CancellationToken cancellationToken);

    Task<EmployeeSummary?> SetServicesAsync(
        Guid employeeId,
        IReadOnlyCollection<Guid> serviceIds,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<EmployeeUserOption>> ListUserOptionsAsync(
        CancellationToken cancellationToken);
}
