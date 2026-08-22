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
public sealed class ServicesController(IServiceCatalogService serviceCatalog) : ControllerBase
{
    [HttpGet]
    [Authorize(Policy = Permissions.ServiceRead)]
    public async Task<IActionResult> ListAsync(
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
    public async Task<IActionResult> GetAsync(
        Guid serviceId,
        CancellationToken cancellationToken)
    {
        ServiceSummary? service = await serviceCatalog.GetAsync(serviceId, cancellationToken);
        return service is null ? NotFound() : Ok(ToResponse(service));
    }

    [HttpPost]
    [Authorize(Policy = Permissions.ServiceManage)]
    [ValidateTrustedOrigin]
    public async Task<IActionResult> CreateAsync(
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
    public async Task<IActionResult> UpdateAsync(
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
    public Task<IActionResult> ActivateAsync(
        Guid serviceId,
        CancellationToken cancellationToken) =>
        SetActiveAsync(serviceId, true, cancellationToken);

    [HttpPost("{serviceId:guid}/deactivate")]
    [Authorize(Policy = Permissions.ServiceManage)]
    [ValidateTrustedOrigin]
    public Task<IActionResult> DeactivateAsync(
        Guid serviceId,
        CancellationToken cancellationToken) =>
        SetActiveAsync(serviceId, false, cancellationToken);

    private async Task<IActionResult> SetActiveAsync(
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
