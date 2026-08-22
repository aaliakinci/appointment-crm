using AppointmentCrm.Api.Security;
using AppointmentCrm.Application.Common;
using AppointmentCrm.Application.Identity;
using AppointmentCrm.Application.Scheduling;
using AppointmentCrm.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AppointmentCrm.Api.Scheduling;

[ApiController]
[Route("api/v1/scheduling/working-hours")]
[Tags("Scheduling")]
[Authorize(Policy = Permissions.SchedulingManage)]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized, "application/problem+json")]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden, "application/problem+json")]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError, "application/problem+json")]
public sealed class WorkingHoursController(ISchedulingService schedulingService) : ControllerBase
{
    [HttpGet("tenant")]
    [ProducesResponseType<WeeklyScheduleResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<WeeklyScheduleResponse>> GetTenantAsync(
        CancellationToken cancellationToken)
    {
        WeeklyScheduleSummary schedule = await schedulingService.GetWeeklyScheduleAsync(
            null,
            cancellationToken);
        return Ok(schedule.ToResponse());
    }

    [HttpPut("tenant")]
    [ValidateTrustedOrigin]
    [ProducesResponseType<WeeklyScheduleResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict, "application/problem+json")]
    public async Task<ActionResult<WeeklyScheduleResponse>> PutTenantAsync(
        PutWeeklyScheduleRequest request,
        CancellationToken cancellationToken)
    {
        WeeklyScheduleSummary schedule = await schedulingService.PutWeeklyScheduleAsync(
            null,
            request.ToInput(),
            cancellationToken);
        return Ok(schedule.ToResponse());
    }

    [HttpGet("employees/{employeeId:guid}")]
    [ProducesResponseType<WeeklyScheduleResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    public async Task<ActionResult<WeeklyScheduleResponse>> GetEmployeeAsync(
        Guid employeeId,
        CancellationToken cancellationToken)
    {
        WeeklyScheduleSummary schedule = await schedulingService.GetWeeklyScheduleAsync(
            employeeId,
            cancellationToken);
        return Ok(schedule.ToResponse());
    }

    [HttpPut("employees/{employeeId:guid}")]
    [ValidateTrustedOrigin]
    [ProducesResponseType<WeeklyScheduleResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict, "application/problem+json")]
    public async Task<ActionResult<WeeklyScheduleResponse>> PutEmployeeAsync(
        Guid employeeId,
        PutWeeklyScheduleRequest request,
        CancellationToken cancellationToken)
    {
        WeeklyScheduleSummary schedule = await schedulingService.PutWeeklyScheduleAsync(
            employeeId,
            request.ToInput(),
            cancellationToken);
        return Ok(schedule.ToResponse());
    }

    [HttpDelete("employees/{employeeId:guid}")]
    [ValidateTrustedOrigin]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict, "application/problem+json")]
    public async Task<ActionResult> DeleteEmployeeAsync(
        Guid employeeId,
        [FromQuery] WeeklyScheduleInheritanceQuery query,
        CancellationToken cancellationToken)
    {
        await schedulingService.DeleteEmployeeWeeklyScheduleAsync(
            employeeId,
            query.ExpectedRevision!.Value,
            query.ChangeNote,
            cancellationToken);
        return NoContent();
    }

    [HttpGet("tenant/versions")]
    [ProducesResponseType<PagedResponse<WeeklyScheduleVersionResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResponse<WeeklyScheduleVersionResponse>>>
        ListTenantVersionsAsync(
            [FromQuery] WeeklyScheduleVersionListQuery query,
            CancellationToken cancellationToken)
    {
        PagedResult<WeeklyScheduleVersionSummary> result =
            await schedulingService.ListWeeklyScheduleVersionsAsync(
                null,
                query.ToPageRequest(),
                cancellationToken);
        return Ok(result.ToResponse());
    }

    [HttpGet("tenant/versions/{versionId:guid}")]
    [ProducesResponseType<WeeklyScheduleVersionResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    public async Task<ActionResult<WeeklyScheduleVersionResponse>> GetTenantVersionAsync(
        Guid versionId,
        CancellationToken cancellationToken)
    {
        WeeklyScheduleVersionSummary version =
            await schedulingService.GetWeeklyScheduleVersionAsync(
                null,
                versionId,
                cancellationToken);
        return Ok(version.ToResponse());
    }

    [HttpPost("tenant/versions/{versionId:guid}/restore")]
    [ValidateTrustedOrigin]
    [ProducesResponseType<WeeklyScheduleResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict, "application/problem+json")]
    public async Task<ActionResult<WeeklyScheduleResponse>> RestoreTenantVersionAsync(
        Guid versionId,
        RestoreWeeklyScheduleVersionRequest request,
        CancellationToken cancellationToken)
    {
        WeeklyScheduleSummary schedule =
            await schedulingService.RestoreWeeklyScheduleVersionAsync(
                null,
                versionId,
                request.ToInput(),
                cancellationToken);
        return Ok(schedule.ToResponse());
    }

    [HttpGet("employees/{employeeId:guid}/versions")]
    [ProducesResponseType<PagedResponse<WeeklyScheduleVersionResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    public async Task<ActionResult<PagedResponse<WeeklyScheduleVersionResponse>>>
        ListEmployeeVersionsAsync(
            Guid employeeId,
            [FromQuery] WeeklyScheduleVersionListQuery query,
            CancellationToken cancellationToken)
    {
        PagedResult<WeeklyScheduleVersionSummary> result =
            await schedulingService.ListWeeklyScheduleVersionsAsync(
                employeeId,
                query.ToPageRequest(),
                cancellationToken);
        return Ok(result.ToResponse());
    }

    [HttpGet("employees/{employeeId:guid}/versions/{versionId:guid}")]
    [ProducesResponseType<WeeklyScheduleVersionResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    public async Task<ActionResult<WeeklyScheduleVersionResponse>> GetEmployeeVersionAsync(
        Guid employeeId,
        Guid versionId,
        CancellationToken cancellationToken)
    {
        WeeklyScheduleVersionSummary version =
            await schedulingService.GetWeeklyScheduleVersionAsync(
                employeeId,
                versionId,
                cancellationToken);
        return Ok(version.ToResponse());
    }

    [HttpPost("employees/{employeeId:guid}/versions/{versionId:guid}/restore")]
    [ValidateTrustedOrigin]
    [ProducesResponseType<WeeklyScheduleResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict, "application/problem+json")]
    public async Task<ActionResult<WeeklyScheduleResponse>> RestoreEmployeeVersionAsync(
        Guid employeeId,
        Guid versionId,
        RestoreWeeklyScheduleVersionRequest request,
        CancellationToken cancellationToken)
    {
        WeeklyScheduleSummary schedule =
            await schedulingService.RestoreWeeklyScheduleVersionAsync(
                employeeId,
                versionId,
                request.ToInput(),
                cancellationToken);
        return Ok(schedule.ToResponse());
    }
}
