using AppointmentCrm.Application.Identity;
using AppointmentCrm.Application.Scheduling;
using AppointmentCrm.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AppointmentCrm.Api.Scheduling;

[ApiController]
[Route("api/v1/availability")]
[Tags("Availability")]
[Authorize(Policy = Permissions.AvailabilityRead)]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized, "application/problem+json")]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden, "application/problem+json")]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError, "application/problem+json")]
public sealed class AvailabilityController(ISchedulingService schedulingService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<AvailabilityResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict, "application/problem+json")]
    public async Task<ActionResult<AvailabilityResponse>> GetAsync(
        [FromQuery] AvailabilityRequestQuery query,
        CancellationToken cancellationToken)
    {
        AvailabilityDay availability = await schedulingService.GetAvailabilityAsync(
            new AvailabilityQuery(query.Date!.Value, query.EmployeeId, query.ServiceId),
            cancellationToken);
        return Ok(availability.ToResponse());
    }
}
