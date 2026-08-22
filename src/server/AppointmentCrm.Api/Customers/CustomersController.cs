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
public sealed class CustomersController(ICustomerService customerService) : ControllerBase
{
    [HttpGet]
    [Authorize(Policy = Permissions.CustomerRead)]
    public async Task<IActionResult> ListAsync(
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
    public async Task<IActionResult> GetAsync(
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
    public async Task<IActionResult> CreateAsync(
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
    public async Task<IActionResult> UpdateAsync(
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
    public async Task<IActionResult> ArchiveAsync(
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
