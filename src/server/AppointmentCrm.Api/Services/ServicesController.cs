using AppointmentCrm.Api.Security;
using AppointmentCrm.Application.Common;
using AppointmentCrm.Application.Identity;
using AppointmentCrm.Application.Services;
using AppointmentCrm.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AppointmentCrm.Api.Services;

[ApiController]
[Route("api/v1/services")]
[Tags("Services")]
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
public sealed class ServicesController(IServiceCatalogService serviceCatalog) : ControllerBase
{
    [HttpGet]
    [Authorize(Policy = Permissions.ServiceRead)]
    [ProducesResponseType<PagedResponse<ServiceResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(
        StatusCodes.Status400BadRequest,
        "application/problem+json")]
    public async Task<ActionResult<PagedResponse<ServiceResponse>>> ListAsync(
        [FromQuery] ServiceListQuery query,
        CancellationToken cancellationToken = default)
    {
        PagedResult<ServiceSummary> result = await serviceCatalog.ListAsync(
            query.ToPageRequest(),
            query.IsActive,
            cancellationToken);
        return Ok(ToResponse(result));
    }

    [HttpGet("{serviceId:guid}", Name = "GetServiceById")]
    [Authorize(Policy = Permissions.ServiceRead)]
    [ProducesResponseType<ServiceResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(
        StatusCodes.Status404NotFound,
        "application/problem+json")]
    public async Task<ActionResult<ServiceResponse>> GetAsync(
        Guid serviceId,
        CancellationToken cancellationToken)
    {
        ServiceSummary? service = await serviceCatalog.GetAsync(serviceId, cancellationToken);
        return service is null ? NotFound() : Ok(ToResponse(service));
    }

    [HttpPost]
    [Authorize(Policy = Permissions.ServiceManage)]
    [ValidateTrustedOrigin]
    [ProducesResponseType<ServiceResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(
        StatusCodes.Status400BadRequest,
        "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(
        StatusCodes.Status409Conflict,
        "application/problem+json")]
    public async Task<ActionResult<ServiceResponse>> CreateAsync(
        CreateServiceRequest request,
        CancellationToken cancellationToken)
    {
        ServiceSummary service = await serviceCatalog.CreateAsync(
            ToInput(request),
            cancellationToken);
        return CreatedAtRoute(
            "GetServiceById",
            new { serviceId = service.Id },
            ToResponse(service));
    }

    [HttpPut("{serviceId:guid}")]
    [Authorize(Policy = Permissions.ServiceManage)]
    [ValidateTrustedOrigin]
    [ProducesResponseType<ServiceResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(
        StatusCodes.Status400BadRequest,
        "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(
        StatusCodes.Status404NotFound,
        "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(
        StatusCodes.Status409Conflict,
        "application/problem+json")]
    public async Task<ActionResult<ServiceResponse>> UpdateAsync(
        Guid serviceId,
        UpdateServiceRequest request,
        CancellationToken cancellationToken)
    {
        ServiceSummary? service = await serviceCatalog.UpdateAsync(
            serviceId,
            ToInput(request),
            cancellationToken);
        return service is null ? NotFound() : Ok(ToResponse(service));
    }

    [HttpPost("{serviceId:guid}/activate")]
    [Authorize(Policy = Permissions.ServiceManage)]
    [ValidateTrustedOrigin]
    [ProducesResponseType<ServiceResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(
        StatusCodes.Status404NotFound,
        "application/problem+json")]
    public Task<ActionResult<ServiceResponse>> ActivateAsync(
        Guid serviceId,
        CancellationToken cancellationToken) =>
        SetActiveAsync(serviceId, true, cancellationToken);

    [HttpPost("{serviceId:guid}/deactivate")]
    [Authorize(Policy = Permissions.ServiceManage)]
    [ValidateTrustedOrigin]
    [ProducesResponseType<ServiceResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(
        StatusCodes.Status404NotFound,
        "application/problem+json")]
    public Task<ActionResult<ServiceResponse>> DeactivateAsync(
        Guid serviceId,
        CancellationToken cancellationToken) =>
        SetActiveAsync(serviceId, false, cancellationToken);

    private async Task<ActionResult<ServiceResponse>> SetActiveAsync(
        Guid serviceId,
        bool isActive,
        CancellationToken cancellationToken)
    {
        ServiceSummary? service = await serviceCatalog.SetActiveAsync(
            serviceId,
            isActive,
            cancellationToken);
        return service is null ? NotFound() : Ok(ToResponse(service));
    }

    private static ServiceInput ToInput(CreateServiceRequest request) =>
        new(request.Name, request.DurationMinutes, request.Price, request.Currency);

    private static ServiceInput ToInput(UpdateServiceRequest request) =>
        new(request.Name, request.DurationMinutes, request.Price, request.Currency);

    private static PagedResponse<ServiceResponse> ToResponse(PagedResult<ServiceSummary> result) =>
        new(
            result.Items.Select(ToResponse).ToList(),
            result.Page,
            result.PageSize,
            result.TotalCount,
            result.TotalPages);

    private static ServiceResponse ToResponse(ServiceSummary service) =>
        new(
            service.Id,
            service.Name,
            service.DurationMinutes,
            service.Price,
            service.Currency,
            service.IsActive,
            service.CreatedAtUtc,
            service.UpdatedAtUtc);
}
