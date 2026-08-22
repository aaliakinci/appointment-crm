using AppointmentCrm.Application.Identity;
using AppointmentCrm.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AppointmentCrm.Api.Identity;

[ApiController]
[Route("api/v1/memberships")]
[Tags("Memberships")]
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
public sealed class MembershipsController : ControllerBase
{
    private readonly IMembershipService _membershipService;

    public MembershipsController(IMembershipService membershipService)
    {
        _membershipService = membershipService;
    }

    [HttpGet]
    [Authorize(Policy = Permissions.MembershipRead)]
    [ProducesResponseType<IReadOnlyList<MembershipResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<MembershipResponse>>> ListAsync(
        CancellationToken cancellationToken)
    {
        IReadOnlyList<MembershipSummary> memberships = await _membershipService.ListAsync(
            cancellationToken);
        return Ok(memberships.Select(ToResponse).ToList());
    }

    [HttpGet("report")]
    [Authorize(Policy = Permissions.MembershipRead)]
    [ProducesResponseType<MembershipReportResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<MembershipReportResponse>> GetReportAsync(
        CancellationToken cancellationToken)
    {
        MembershipReport report = await _membershipService.GetReportAsync(cancellationToken);
        return Ok(new MembershipReportResponse(report.Total, report.Active, report.ByRole));
    }

    [HttpGet("{membershipId:guid}")]
    [Authorize(Policy = Permissions.MembershipRead)]
    [ProducesResponseType<MembershipResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(
        StatusCodes.Status404NotFound,
        "application/problem+json")]
    public async Task<ActionResult<MembershipResponse>> GetAsync(
        Guid membershipId,
        CancellationToken cancellationToken)
    {
        MembershipSummary? membership = await _membershipService.GetAsync(
            membershipId,
            cancellationToken);
        return membership is null
            ? NotFound()
            : Ok(ToResponse(membership));
    }

    [HttpPatch("{membershipId:guid}")]
    [Authorize(Policy = Permissions.MembershipManage)]
    [ProducesResponseType<MembershipResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(
        StatusCodes.Status400BadRequest,
        "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(
        StatusCodes.Status404NotFound,
        "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(
        StatusCodes.Status409Conflict,
        "application/problem+json")]
    public async Task<ActionResult<MembershipResponse>> UpdateAsync(
        Guid membershipId,
        UpdateMembershipRequest request,
        CancellationToken cancellationToken)
    {
        MembershipSummary? membership = await _membershipService.UpdateAsync(
            membershipId,
            request.Role,
            request.IsActive,
            cancellationToken);
        return membership is null
            ? NotFound()
            : Ok(ToResponse(membership));
    }

    [HttpDelete("{membershipId:guid}")]
    [Authorize(Policy = Permissions.MembershipManage)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(
        StatusCodes.Status404NotFound,
        "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(
        StatusCodes.Status409Conflict,
        "application/problem+json")]
    public async Task<ActionResult> ArchiveAsync(
        Guid membershipId,
        CancellationToken cancellationToken)
    {
        bool archived = await _membershipService.ArchiveAsync(
            membershipId,
            cancellationToken);
        return archived ? NoContent() : NotFound();
    }

    private static MembershipResponse ToResponse(MembershipSummary membership) =>
        new(
            membership.Id,
            membership.UserId,
            membership.Email,
            membership.DisplayName,
            membership.Role,
            membership.IsActive,
            membership.UpdatedAtUtc);
}
