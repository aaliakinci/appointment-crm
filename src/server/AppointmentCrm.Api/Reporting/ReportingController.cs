using AppointmentCrm.Application.Identity;
using AppointmentCrm.Application.Reporting;
using AppointmentCrm.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AppointmentCrm.Api.Reporting;

[ApiController]
[Route("api/v1/reporting")]
[Tags("Reporting")]
[Authorize]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized, "application/problem+json")]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden, "application/problem+json")]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError, "application/problem+json")]
public sealed class ReportingController(IReportingService reportingService) : ControllerBase
{
    [HttpGet("dashboard")]
    [Authorize(Policy = Permissions.ReportingRead)]
    [ProducesResponseType<ReportingDashboardResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(
        StatusCodes.Status400BadRequest,
        "application/problem+json")]
    public async Task<ActionResult<ReportingDashboardResponse>> GetDashboardAsync(
        [FromQuery] ReportingQuery query,
        CancellationToken cancellationToken)
    {
        ReportingDashboard dashboard = await reportingService.GetDashboardAsync(
            query.ToFilter(),
            cancellationToken);
        return Ok(dashboard.ToResponse());
    }
}
