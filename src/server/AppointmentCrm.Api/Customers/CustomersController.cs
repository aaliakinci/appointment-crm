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
        return Ok(result.ToResponse());
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
        return customer is null ? NotFound() : Ok(customer.ToResponse());
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
            request.ToInput(),
            cancellationToken);
        return CreatedAtRoute(
            "GetCustomerById",
            new { customerId = customer.Id },
            customer.ToResponse());
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
            request.ToInput(),
            cancellationToken);
        return customer is null ? NotFound() : Ok(customer.ToResponse());
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
}
