using AppointmentCrm.Api.Security;
using AppointmentCrm.Application.Common;
using AppointmentCrm.Application.Customers;
using AppointmentCrm.Application.Identity;
using AppointmentCrm.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AppointmentCrm.Api.Customers;

[ApiController]
[Route("api/v1/customers")]
[Tags("Customers")]
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
public sealed class CustomersController(ICustomerService customerService) : ControllerBase
{
    [HttpGet]
    [Authorize(Policy = Permissions.CustomerRead)]
    [ProducesResponseType<PagedResponse<CustomerResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(
        StatusCodes.Status400BadRequest,
        "application/problem+json")]
    public async Task<ActionResult<PagedResponse<CustomerResponse>>> ListAsync(
        [FromQuery] CustomerListQuery query,
        CancellationToken cancellationToken = default)
    {
        PagedResult<CustomerSummary> result = await customerService.ListAsync(
            query.ToPageRequest(),
            query.IncludeArchived,
            cancellationToken);
        return Ok(ToResponse(result));
    }

    [HttpGet("{customerId:guid}", Name = "GetCustomerById")]
    [Authorize(Policy = Permissions.CustomerRead)]
    [ProducesResponseType<CustomerResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(
        StatusCodes.Status404NotFound,
        "application/problem+json")]
    public async Task<ActionResult<CustomerResponse>> GetAsync(
        Guid customerId,
        CancellationToken cancellationToken)
    {
        CustomerSummary? customer = await customerService.GetAsync(
            customerId,
            cancellationToken);
        return customer is null ? NotFound() : Ok(ToResponse(customer));
    }

    [HttpPost]
    [Authorize(Policy = Permissions.CustomerManage)]
    [ValidateTrustedOrigin]
    [ProducesResponseType<CustomerResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(
        StatusCodes.Status400BadRequest,
        "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(
        StatusCodes.Status409Conflict,
        "application/problem+json")]
    public async Task<ActionResult<CustomerResponse>> CreateAsync(
        CreateCustomerRequest request,
        CancellationToken cancellationToken)
    {
        CustomerSummary customer = await customerService.CreateAsync(
            ToInput(request),
            cancellationToken);
        return CreatedAtRoute(
            "GetCustomerById",
            new { customerId = customer.Id },
            ToResponse(customer));
    }

    [HttpPut("{customerId:guid}")]
    [Authorize(Policy = Permissions.CustomerManage)]
    [ValidateTrustedOrigin]
    [ProducesResponseType<CustomerResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(
        StatusCodes.Status400BadRequest,
        "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(
        StatusCodes.Status404NotFound,
        "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(
        StatusCodes.Status409Conflict,
        "application/problem+json")]
    public async Task<ActionResult<CustomerResponse>> UpdateAsync(
        Guid customerId,
        UpdateCustomerRequest request,
        CancellationToken cancellationToken)
    {
        CustomerSummary? customer = await customerService.UpdateAsync(
            customerId,
            ToInput(request),
            cancellationToken);
        return customer is null ? NotFound() : Ok(ToResponse(customer));
    }

    [HttpDelete("{customerId:guid}")]
    [Authorize(Policy = Permissions.CustomerManage)]
    [ValidateTrustedOrigin]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(
        StatusCodes.Status404NotFound,
        "application/problem+json")]
    public async Task<ActionResult> ArchiveAsync(
        Guid customerId,
        CancellationToken cancellationToken)
    {
        bool archived = await customerService.ArchiveAsync(customerId, cancellationToken);
        return archived ? NoContent() : NotFound();
    }

    private static CustomerInput ToInput(CreateCustomerRequest request) =>
        new(request.Name, request.Email, request.Phone, request.Notes);

    private static CustomerInput ToInput(UpdateCustomerRequest request) =>
        new(request.Name, request.Email, request.Phone, request.Notes);

    private static PagedResponse<CustomerResponse> ToResponse(
        PagedResult<CustomerSummary> result) =>
        new(
            result.Items.Select(ToResponse).ToList(),
            result.Page,
            result.PageSize,
            result.TotalCount,
            result.TotalPages);

    private static CustomerResponse ToResponse(CustomerSummary customer) =>
        new(
            customer.Id,
            customer.Name,
            customer.Email,
            customer.Phone,
            customer.Notes,
            customer.ArchivedAtUtc,
            customer.CreatedAtUtc,
            customer.UpdatedAtUtc);
}
