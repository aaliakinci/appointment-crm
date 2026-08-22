using AppointmentCrm.Api.Security;
using AppointmentCrm.Application.Identity;
using AppointmentCrm.Application.Scheduling;
using AppointmentCrm.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AppointmentCrm.Api.Scheduling;

[ApiController]
[Route("api/v1/scheduling/time-off")]
[Tags("Scheduling")]
[Authorize(Policy = Permissions.SchedulingManage)]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized, "application/problem+json")]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden, "application/problem+json")]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError, "application/problem+json")]
public sealed class TimeOffController(ISchedulingService schedulingService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<TimeOffResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest, "application/problem+json")]
    public async Task<ActionResult<IReadOnlyList<TimeOffResponse>>> ListAsync(
        [FromQuery] DateRangeQuery query,
        CancellationToken cancellationToken)
    {
        IReadOnlyList<TimeOffSummary> timeOffs = await schedulingService.ListTimeOffAsync(
            query.EmployeeId,
            query.FromDate!.Value,
            query.ToDate!.Value,
            cancellationToken);
        return Ok(timeOffs.ToResponse());
    }

    [HttpPost]
    [ValidateTrustedOrigin]
    [ProducesResponseType<TimeOffResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict, "application/problem+json")]
    public async Task<ActionResult<TimeOffResponse>> CreateAsync(
        CreateTimeOffRequest request,
        CancellationToken cancellationToken)
    {
        TimeOffSummary timeOff = await schedulingService.CreateTimeOffAsync(
            request.ToInput(),
            cancellationToken);
        return Created($"/api/v1/scheduling/time-off/{timeOff.Id}", timeOff.ToResponse());
    }

    [HttpDelete("{timeOffId:guid}")]
    [ValidateTrustedOrigin]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    public async Task<ActionResult> DeleteAsync(
        Guid timeOffId,
        CancellationToken cancellationToken)
    {
        await schedulingService.DeleteTimeOffAsync(timeOffId, cancellationToken);
        return NoContent();
    }
}
