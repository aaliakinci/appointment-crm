using AppointmentCrm.Application.Identity;
using AppointmentCrm.Contracts;
using AppointmentCrm.Domain.Identity;
using AppointmentCrm.Infrastructure.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;

namespace AppointmentCrm.Api.Identity;

[ApiController]
[Route("api/v1/memberships")]
[Tags("Memberships")]
[Authorize]
public sealed class MembershipsController : ControllerBase
{
    private readonly IMembershipService _membershipService;

    public MembershipsController(IMembershipService membershipService)
    {
        _membershipService = membershipService;
    }

    [HttpGet]
    [Authorize(Policy = Permissions.MembershipRead)]
    public async Task<IActionResult> ListAsync(CancellationToken cancellationToken)
    {
        IReadOnlyList<MembershipSummary> memberships = await _membershipService.ListAsync(
            cancellationToken);
        return Ok(memberships.Select(ToResponse));
    }

    [HttpGet("report")]
    [Authorize(Policy = Permissions.MembershipRead)]
    public async Task<IActionResult> GetReportAsync(CancellationToken cancellationToken)
    {
        MembershipReport report = await _membershipService.GetReportAsync(cancellationToken);
        return Ok(new MembershipReportResponse(report.Total, report.Active, report.ByRole));
    }

    [HttpGet("{membershipId:guid}")]
    [Authorize(Policy = Permissions.MembershipRead)]
    public async Task<IActionResult> GetAsync(
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
    public async Task<IActionResult> UpdateAsync(
        Guid membershipId,
        UpdateMembershipRequest request,
        CancellationToken cancellationToken)
    {
        if (!TenantRoles.IsDefined(request.Role))
        {
            return ValidationProblem(new ValidationProblemDetails(
                new Dictionary<string, string[]>
                {
                    [nameof(request.Role)] = ["Role is not valid."],
                }));
        }

        try
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
        catch (MembershipConflictException exception)
        {
            return ApiProblem(StatusCodes.Status409Conflict, exception.Message);
        }
    }

    [HttpDelete("{membershipId:guid}")]
    [Authorize(Policy = Permissions.MembershipManage)]
    public async Task<IActionResult> ArchiveAsync(
        Guid membershipId,
        CancellationToken cancellationToken)
    {
        try
        {
            bool archived = await _membershipService.ArchiveAsync(
                membershipId,
                cancellationToken);
            return archived ? NoContent() : NotFound();
        }
        catch (MembershipConflictException exception)
        {
            return ApiProblem(StatusCodes.Status409Conflict, exception.Message);
        }
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

    private ObjectResult ApiProblem(int status, string detail)
    {
        var problem = new ProblemDetails
        {
            Status = status,
            Title = ReasonPhrases.GetReasonPhrase(status),
            Detail = detail,
        };
        problem.Extensions["traceId"] = HttpContext.TraceIdentifier;

        var result = new ObjectResult(problem) { StatusCode = status };
        result.ContentTypes.Add("application/problem+json");
        return result;
    }
}
