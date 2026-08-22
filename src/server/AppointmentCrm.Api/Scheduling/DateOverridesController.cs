using AppointmentCrm.Api.Security;
using AppointmentCrm.Application.Identity;
using AppointmentCrm.Application.Scheduling;
using AppointmentCrm.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AppointmentCrm.Api.Scheduling;

[ApiController]
[Route("api/v1/scheduling/date-overrides")]
[Tags("Scheduling")]
[Authorize(Policy = Permissions.SchedulingManage)]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized, "application/problem+json")]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden, "application/problem+json")]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError, "application/problem+json")]
public sealed class DateOverridesController(ISchedulingService schedulingService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<DateOverrideResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest, "application/problem+json")]
    public async Task<ActionResult<IReadOnlyList<DateOverrideResponse>>> ListAsync(
        [FromQuery] DateRangeQuery query,
        CancellationToken cancellationToken)
    {
        IReadOnlyList<DateOverrideSummary> overrides =
            await schedulingService.ListDateOverridesAsync(
                query.EmployeeId,
                query.FromDate!.Value,
                query.ToDate!.Value,
                cancellationToken);
        return Ok(overrides.ToResponse());
    }

    [HttpPut("tenant/{date}")]
    [ValidateTrustedOrigin]
    [ProducesResponseType<DateOverrideResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict, "application/problem+json")]
    public async Task<ActionResult<DateOverrideResponse>> PutTenantAsync(
        DateOnly date,
        PutDateOverrideRequest request,
        CancellationToken cancellationToken)
    {
        DateOverrideSummary scheduleOverride = await schedulingService.PutDateOverrideAsync(
            null,
            date,
            request.ToInput(),
            cancellationToken);
        return Ok(scheduleOverride.ToResponse());
    }

    [HttpDelete("tenant/{date}")]
    [ValidateTrustedOrigin]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<ActionResult> DeleteTenantAsync(
        DateOnly date,
        CancellationToken cancellationToken)
    {
        await schedulingService.DeleteDateOverrideAsync(null, date, cancellationToken);
        return NoContent();
    }

    [HttpPut("employees/{employeeId:guid}/{date}")]
    [ValidateTrustedOrigin]
    [ProducesResponseType<DateOverrideResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict, "application/problem+json")]
    public async Task<ActionResult<DateOverrideResponse>> PutEmployeeAsync(
        Guid employeeId,
        DateOnly date,
        PutDateOverrideRequest request,
        CancellationToken cancellationToken)
    {
        DateOverrideSummary scheduleOverride = await schedulingService.PutDateOverrideAsync(
            employeeId,
            date,
            request.ToInput(),
            cancellationToken);
        return Ok(scheduleOverride.ToResponse());
    }

    [HttpDelete("employees/{employeeId:guid}/{date}")]
    [ValidateTrustedOrigin]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    public async Task<ActionResult> DeleteEmployeeAsync(
        Guid employeeId,
        DateOnly date,
        CancellationToken cancellationToken)
    {
        await schedulingService.DeleteDateOverrideAsync(employeeId, date, cancellationToken);
        return NoContent();
    }
}
