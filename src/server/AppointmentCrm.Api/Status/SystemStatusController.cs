using AppointmentCrm.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AppointmentCrm.Api.Status;

[ApiController]
[Route("api/v1/system")]
[Tags("System")]
[ProducesResponseType<ProblemDetails>(
    StatusCodes.Status500InternalServerError,
    "application/problem+json")]
public sealed class SystemStatusController : ControllerBase
{
    [HttpGet("status", Name = "GetSystemStatus")]
    [AllowAnonymous]
    [ProducesResponseType<SystemStatusResponse>(StatusCodes.Status200OK)]
    public ActionResult<SystemStatusResponse> GetStatus() =>
        Ok(new SystemStatusResponse(
            Service: "appointment-crm-api",
            Status: "ready",
            TimestampUtc: DateTimeOffset.UtcNow,
            TraceId: HttpContext.TraceIdentifier));
}
