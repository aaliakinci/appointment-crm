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
[ProducesResponseType<ProblemDetails>(
    StatusCodes.Status401Unauthorized,
    "application/problem+json")]
[ProducesResponseType<ProblemDetails>(
    StatusCodes.Status403Forbidden,
    "application/problem+json")]
[ProducesResponseType<ProblemDetails>(
    StatusCodes.Status500InternalServerError,
    "application/problem+json")]
public sealed class EmployeesController(IEmployeeManagementService employeeService)
    : ControllerBase
{
    [HttpGet]
    [Authorize(Policy = Permissions.EmployeeRead)]
    [ProducesResponseType<PagedResponse<EmployeeResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(
        StatusCodes.Status400BadRequest,
        "application/problem+json")]
    public async Task<ActionResult<PagedResponse<EmployeeResponse>>> ListAsync(
        [FromQuery] EmployeeListQuery query,
        CancellationToken cancellationToken = default)
    {
        PagedResult<EmployeeSummary> result = await employeeService.ListAsync(
            query.ToPageRequest(),
            query.IsActive,
            query.ServiceId,
            cancellationToken);
        return Ok(ToResponse(result));
    }

    [HttpGet("user-options")]
    [Authorize(Policy = Permissions.EmployeeManage)]
    [ProducesResponseType<IReadOnlyList<EmployeeUserOptionResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<EmployeeUserOptionResponse>>> ListUserOptionsAsync(
        CancellationToken cancellationToken)
    {
        IReadOnlyList<EmployeeUserOption> options = await employeeService.ListUserOptionsAsync(
            cancellationToken);
        return Ok(options
            .Select(option => new EmployeeUserOptionResponse(
                option.UserId,
                option.DisplayName,
                option.Email,
                option.Role,
                option.IsLinked))
            .ToList());
    }

    [HttpGet("{employeeId:guid}", Name = "GetEmployeeById")]
    [Authorize(Policy = Permissions.EmployeeRead)]
    [ProducesResponseType<EmployeeResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(
        StatusCodes.Status404NotFound,
        "application/problem+json")]
    public async Task<ActionResult<EmployeeResponse>> GetAsync(
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
    [ProducesResponseType<EmployeeResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(
        StatusCodes.Status400BadRequest,
        "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(
        StatusCodes.Status409Conflict,
        "application/problem+json")]
    public async Task<ActionResult<EmployeeResponse>> CreateAsync(
        CreateEmployeeRequest request,
        CancellationToken cancellationToken)
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

    [HttpPut("{employeeId:guid}")]
    [Authorize(Policy = Permissions.EmployeeManage)]
    [ValidateTrustedOrigin]
    [ProducesResponseType<EmployeeResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(
        StatusCodes.Status400BadRequest,
        "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(
        StatusCodes.Status404NotFound,
        "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(
        StatusCodes.Status409Conflict,
        "application/problem+json")]
    public async Task<ActionResult<EmployeeResponse>> UpdateAsync(
        Guid employeeId,
        UpdateEmployeeRequest request,
        CancellationToken cancellationToken)
    {
        EmployeeSummary? employee = await employeeService.UpdateAsync(
            employeeId,
            ToInput(request.UserId, request.Name, request.Email, request.Phone),
            cancellationToken);
        return employee is null ? NotFound() : Ok(ToResponse(employee));
    }

    [HttpPut("{employeeId:guid}/services")]
    [Authorize(Policy = Permissions.EmployeeManage)]
    [ValidateTrustedOrigin]
    [ProducesResponseType<EmployeeResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(
        StatusCodes.Status400BadRequest,
        "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(
        StatusCodes.Status404NotFound,
        "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(
        StatusCodes.Status409Conflict,
        "application/problem+json")]
    public async Task<ActionResult<EmployeeResponse>> SetServicesAsync(
        Guid employeeId,
        SetEmployeeServicesRequest request,
        CancellationToken cancellationToken)
    {
        EmployeeSummary? employee = await employeeService.SetServicesAsync(
            employeeId,
            request.ServiceIds ?? [],
            cancellationToken);
        return employee is null ? NotFound() : Ok(ToResponse(employee));
    }

    [HttpPost("{employeeId:guid}/activate")]
    [Authorize(Policy = Permissions.EmployeeManage)]
    [ValidateTrustedOrigin]
    [ProducesResponseType<EmployeeResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(
        StatusCodes.Status404NotFound,
        "application/problem+json")]
    public Task<ActionResult<EmployeeResponse>> ActivateAsync(
        Guid employeeId,
        CancellationToken cancellationToken) =>
        SetActiveAsync(employeeId, true, cancellationToken);

    [HttpPost("{employeeId:guid}/deactivate")]
    [Authorize(Policy = Permissions.EmployeeManage)]
    [ValidateTrustedOrigin]
    [ProducesResponseType<EmployeeResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(
        StatusCodes.Status404NotFound,
        "application/problem+json")]
    public Task<ActionResult<EmployeeResponse>> DeactivateAsync(
        Guid employeeId,
        CancellationToken cancellationToken) =>
        SetActiveAsync(employeeId, false, cancellationToken);

    private async Task<ActionResult<EmployeeResponse>> SetActiveAsync(
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
