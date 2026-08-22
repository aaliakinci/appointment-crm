using AppointmentCrm.Application.Common;
using AppointmentCrm.Application.Employees;
using AppointmentCrm.Contracts;

namespace AppointmentCrm.Api.Employees;

internal static class EmployeeContractMappings
{
    internal static EmployeeInput ToInput(this CreateEmployeeRequest request) =>
        new(request.UserId, request.Name, request.Email, request.Phone);

    internal static EmployeeInput ToInput(this UpdateEmployeeRequest request) =>
        new(request.UserId, request.Name, request.Email, request.Phone);

    internal static PagedResponse<EmployeeResponse> ToResponse(
        this PagedResult<EmployeeSummary> result) =>
        new(
            result.Items.Select(employee => employee.ToResponse()).ToList(),
            result.Page,
            result.PageSize,
            result.TotalCount,
            result.TotalPages);

    internal static EmployeeResponse ToResponse(this EmployeeSummary employee) =>
        new(
            employee.Id,
            employee.UserId,
            employee.Name,
            employee.Email,
            employee.Phone,
            employee.IsActive,
            employee.Services.Select(service => service.ToResponse()).ToList(),
            employee.CreatedAtUtc,
            employee.UpdatedAtUtc);

    internal static EmployeeServiceResponse ToResponse(this EmployeeServiceSummary service) =>
        new(service.Id, service.Name, service.IsActive);

    internal static IReadOnlyList<EmployeeUserOptionResponse> ToResponse(
        this IEnumerable<EmployeeUserOption> options) =>
        options.Select(option => option.ToResponse()).ToList();

    private static EmployeeUserOptionResponse ToResponse(this EmployeeUserOption option) =>
        new(
            option.UserId,
            option.DisplayName,
            option.Email,
            option.Role,
            option.IsLinked);
}
