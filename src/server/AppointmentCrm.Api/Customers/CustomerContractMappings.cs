using AppointmentCrm.Application.Common;
using AppointmentCrm.Application.Customers;
using AppointmentCrm.Contracts;

namespace AppointmentCrm.Api.Customers;

internal static class CustomerContractMappings
{
    internal static CustomerInput ToInput(this CreateCustomerRequest request) =>
        new(request.Name, request.Email, request.Phone, request.Notes);

    internal static CustomerInput ToInput(this UpdateCustomerRequest request) =>
        new(request.Name, request.Email, request.Phone, request.Notes);

    internal static PagedResponse<CustomerResponse> ToResponse(
        this PagedResult<CustomerSummary> result) =>
        new(
            result.Items.Select(customer => customer.ToResponse()).ToList(),
            result.Page,
            result.PageSize,
            result.TotalCount,
            result.TotalPages);

    internal static CustomerResponse ToResponse(this CustomerSummary customer) =>
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
