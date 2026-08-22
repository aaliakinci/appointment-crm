using AppointmentCrm.Api.Controllers;
using AppointmentCrm.Api.Security;
using AppointmentCrm.Application.Common;
using AppointmentCrm.Application.Employees;
using AppointmentCrm.Application.Identity;
using AppointmentCrm.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AppointmentCrm.Api.Employees;

[ApiController]
[Route("api/v1/employees")]
[Tags("Employees")]
[Authorize]
public sealed class EmployeesController(IEmployeeManagementService employeeService)
    : ApiControllerBase
{
    private static readonly HashSet<string> AllowedSorts =
        new(StringComparer.Ordinal) { "name", "createdAt", "updatedAt" };

    [HttpGet]
    [Authorize(Policy = Permissions.EmployeeRead)]
    public async Task<IActionResult> ListAsync(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = PageRequest.DefaultPageSize,
        [FromQuery] string? search = null,
        [FromQuery] bool? isActive = null,
        [FromQuery] Guid? serviceId = null,
        [FromQuery] string? sortBy = null,
        [FromQuery] string? sortDirection = null,
        CancellationToken cancellationToken = default)
    {
        if (!TryCreatePageRequest(
            page,
            pageSize,
            search,
            sortBy,
            sortDirection,
            "name",
            AllowedSorts,
            out PageRequest request,
            out IActionResult? error))
        {
            return error!;
        }

        PagedResult<EmployeeSummary> result = await employeeService.ListAsync(
            request,
            isActive,
            serviceId,
            cancellationToken);
        return Ok(ToResponse(result));
    }

    [HttpGet("user-options")]
    [Authorize(Policy = Permissions.EmployeeManage)]
    public async Task<IActionResult> ListUserOptionsAsync(CancellationToken cancellationToken)
    {
        IReadOnlyList<EmployeeUserOption> options = await employeeService.ListUserOptionsAsync(
            cancellationToken);
        return Ok(options.Select(option => new EmployeeUserOptionResponse(
            option.UserId,
            option.DisplayName,
            option.Email,
            option.Role,
            option.IsLinked)));
    }

    [HttpGet("{employeeId:guid}", Name = "GetEmployeeById")]
    [Authorize(Policy = Permissions.EmployeeRead)]
    public async Task<IActionResult> GetAsync(
        Guid employeeId,
        CancellationToken cancellationToken)
    {
        EmployeeSummary? employee = await employeeService.GetAsync(
            employeeId,
            cancellationToken);
        return employee is null ? NotFound() : Ok(ToResponse(employee));
    }

    [HttpPost]
    [Authorize(Policy = Permissions.EmployeeManage)]
    [ValidateTrustedOrigin]
    public async Task<IActionResult> CreateAsync(
        CreateEmployeeRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            EmployeeSummary employee = await employeeService.CreateAsync(
                ToInput(request.UserId, request.Name, request.Email, request.Phone),
                request.ServiceIds ?? [],
                cancellationToken);
            return CreatedAtRoute(
                "GetEmployeeById",
                new { employeeId = employee.Id },
                ToResponse(employee));
        }
        catch (MasterDataConflictException exception)
        {
            return ApiProblem(StatusCodes.Status409Conflict, exception.Message);
        }
        catch (ArgumentException exception)
        {
            return InvalidArgument(exception);
        }
    }

    [HttpPut("{employeeId:guid}")]
    [Authorize(Policy = Permissions.EmployeeManage)]
    [ValidateTrustedOrigin]
    public async Task<IActionResult> UpdateAsync(
        Guid employeeId,
        UpdateEmployeeRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            EmployeeSummary? employee = await employeeService.UpdateAsync(
                employeeId,
                ToInput(request.UserId, request.Name, request.Email, request.Phone),
                cancellationToken);
            return employee is null ? NotFound() : Ok(ToResponse(employee));
        }
        catch (MasterDataConflictException exception)
        {
            return ApiProblem(StatusCodes.Status409Conflict, exception.Message);
        }
        catch (ArgumentException exception)
        {
            return InvalidArgument(exception);
        }
    }

    [HttpPut("{employeeId:guid}/services")]
    [Authorize(Policy = Permissions.EmployeeManage)]
    [ValidateTrustedOrigin]
    public async Task<IActionResult> SetServicesAsync(
        Guid employeeId,
        SetEmployeeServicesRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            EmployeeSummary? employee = await employeeService.SetServicesAsync(
                employeeId,
                request.ServiceIds ?? [],
                cancellationToken);
            return employee is null ? NotFound() : Ok(ToResponse(employee));
        }
        catch (MasterDataConflictException exception)
        {
            return ApiProblem(StatusCodes.Status409Conflict, exception.Message);
        }
        catch (ArgumentException exception)
        {
            return InvalidArgument(exception);
        }
    }

    [HttpPost("{employeeId:guid}/activate")]
    [Authorize(Policy = Permissions.EmployeeManage)]
    [ValidateTrustedOrigin]
    public Task<IActionResult> ActivateAsync(
        Guid employeeId,
        CancellationToken cancellationToken) =>
        SetActiveAsync(employeeId, true, cancellationToken);

    [HttpPost("{employeeId:guid}/deactivate")]
    [Authorize(Policy = Permissions.EmployeeManage)]
    [ValidateTrustedOrigin]
    public Task<IActionResult> DeactivateAsync(
        Guid employeeId,
        CancellationToken cancellationToken) =>
        SetActiveAsync(employeeId, false, cancellationToken);

    private async Task<IActionResult> SetActiveAsync(
        Guid employeeId,
        bool isActive,
        CancellationToken cancellationToken)
    {
        EmployeeSummary? employee = await employeeService.SetActiveAsync(
            employeeId,
            isActive,
            cancellationToken);
        return employee is null ? NotFound() : Ok(ToResponse(employee));
    }

    private static EmployeeInput ToInput(
        Guid? userId,
        string name,
        string? email,
        string? phone) =>
        new(userId, name, email, phone);

    private static PagedResponse<EmployeeResponse> ToResponse(
        PagedResult<EmployeeSummary> result) =>
        new(
            result.Items.Select(ToResponse).ToList(),
            result.Page,
            result.PageSize,
            result.TotalCount,
            result.TotalPages);

    private static EmployeeResponse ToResponse(EmployeeSummary employee) =>
        new(
            employee.Id,
            employee.UserId,
            employee.Name,
            employee.Email,
            employee.Phone,
            employee.IsActive,
            employee.Services.Select(service => new EmployeeServiceResponse(
                service.Id,
                service.Name,
                service.IsActive)).ToList(),
            employee.CreatedAtUtc,
            employee.UpdatedAtUtc);
}
