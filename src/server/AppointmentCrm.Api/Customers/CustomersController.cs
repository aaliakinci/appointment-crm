using AppointmentCrm.Api.Controllers;
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
public sealed class CustomersController(ICustomerService customerService) : ApiControllerBase
{
    private static readonly HashSet<string> AllowedSorts =
        new(StringComparer.Ordinal) { "name", "createdAt", "updatedAt" };

    [HttpGet]
    [Authorize(Policy = Permissions.CustomerRead)]
    public async Task<IActionResult> ListAsync(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = PageRequest.DefaultPageSize,
        [FromQuery] string? search = null,
        [FromQuery] bool includeArchived = false,
        [FromQuery] string? sortBy = null,
        [FromQuery] string? sortDirection = null,
        CancellationToken cancellationToken = default)
    {
        if (!TryCreatePageRequest(
            page,
            pageSize,
            search,
            sortBy,
            sortDirection,
            "name",
            AllowedSorts,
            out PageRequest request,
            out IActionResult? error))
        {
            return error!;
        }

        PagedResult<CustomerSummary> result = await customerService.ListAsync(
            request,
            includeArchived,
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
        try
        {
            CustomerSummary customer = await customerService.CreateAsync(
                ToInput(request),
                cancellationToken);
            return CreatedAtRoute(
                "GetCustomerById",
                new { customerId = customer.Id },
                ToResponse(customer));
        }
        catch (MasterDataConflictException exception)
        {
            return ApiProblem(StatusCodes.Status409Conflict, exception.Message);
        }
        catch (ArgumentException exception)
        {
            return InvalidArgument(exception);
        }
    }

    [HttpPut("{customerId:guid}")]
    [Authorize(Policy = Permissions.CustomerManage)]
    [ValidateTrustedOrigin]
    public async Task<IActionResult> UpdateAsync(
        Guid customerId,
        UpdateCustomerRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            CustomerSummary? customer = await customerService.UpdateAsync(
                customerId,
                ToInput(request),
                cancellationToken);
            return customer is null ? NotFound() : Ok(ToResponse(customer));
        }
        catch (MasterDataConflictException exception)
        {
            return ApiProblem(StatusCodes.Status409Conflict, exception.Message);
        }
        catch (ArgumentException exception)
        {
            return InvalidArgument(exception);
        }
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
