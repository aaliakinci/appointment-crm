using AppointmentCrm.Api.Security;
using AppointmentCrm.Application.Auditing;
using AppointmentCrm.Application.Identity;
using AppointmentCrm.Contracts;
using AppointmentCrm.Infrastructure.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AppointmentCrm.Api.Identity;

[ApiController]
[Route("api/v1/account")]
[Tags("Account")]
[Authorize]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized, "application/problem+json")]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden, "application/problem+json")]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError, "application/problem+json")]
public sealed class AccountController(
    IAccountService accountService,
    ICurrentActor currentActor) : ControllerBase
{
    [HttpGet("profile")]
    [ProducesResponseType<AccountProfileResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<AccountProfileResponse>> GetProfileAsync(
        CancellationToken cancellationToken)
    {
        AccountProfile profile = await accountService.GetProfileAsync(
            currentActor.UserId,
            cancellationToken);
        return Ok(profile.ToResponse());
    }

    [HttpPut("profile")]
    [ValidateTrustedOrigin]
    [ProducesResponseType<AccountProfileResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(
        StatusCodes.Status400BadRequest,
        "application/problem+json")]
    public async Task<ActionResult<AccountProfileResponse>> UpdateProfileAsync(
        UpdateProfileRequest request,
        CancellationToken cancellationToken)
    {
        AccountProfile profile = await accountService.UpdateProfileAsync(
            currentActor.UserId,
            request.DisplayName,
            cancellationToken);
        return Ok(profile.ToResponse());
    }

    [HttpGet("sessions")]
    [Authorize(Policy = Permissions.SessionManageOwn)]
    [ProducesResponseType<IReadOnlyList<AccountSessionResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<AccountSessionResponse>>> ListSessionsAsync(
        CancellationToken cancellationToken)
    {
        IReadOnlyList<AccountSession> sessions = await accountService.ListActiveSessionsAsync(
            currentActor.UserId,
            GetSessionId(),
            cancellationToken);
        return Ok(sessions.ToResponse());
    }

    [HttpDelete("sessions/{sessionId:guid}")]
    [Authorize(Policy = Permissions.SessionManageOwn)]
    [ValidateTrustedOrigin]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    public async Task<ActionResult> RevokeSessionAsync(
        Guid sessionId,
        CancellationToken cancellationToken)
    {
        bool revoked = await accountService.RevokeSessionAsync(
            currentActor.UserId,
            sessionId,
            cancellationToken);
        return revoked ? NoContent() : NotFound();
    }

    private Guid GetSessionId()
    {
        string? value = User.FindFirst(IdentityClaimNames.SessionId)?.Value;
        return Guid.TryParse(value, out Guid sessionId)
            ? sessionId
            : throw new InvalidOperationException("The authenticated session id is missing.");
    }
}
