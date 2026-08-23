using AppointmentCrm.Application.Auditing;
using AppointmentCrm.Application.Common;
using AppointmentCrm.Application.Identity;
using AppointmentCrm.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AppointmentCrm.Api.Auditing;

[ApiController]
[Route("api/v1/audit")]
[Tags("Audit")]
[Authorize]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized, "application/problem+json")]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden, "application/problem+json")]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError, "application/problem+json")]
public sealed class AuditController(IAuditReader auditReader) : ControllerBase
{
    [HttpGet]
    [Authorize(Policy = Permissions.ReportingRead)]
    [ProducesResponseType<PagedResponse<AuditResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(
        StatusCodes.Status400BadRequest,
        "application/problem+json")]
    public async Task<ActionResult<PagedResponse<AuditResponse>>> ListAsync(
        [FromQuery] AuditListQuery query,
        CancellationToken cancellationToken)
    {
        PagedResult<AuditSummary> result = await auditReader.ListAsync(
            query.ToPageRequest(),
            query.ToFilter(),
            cancellationToken);
        return Ok(result.ToResponse());
    }
}
