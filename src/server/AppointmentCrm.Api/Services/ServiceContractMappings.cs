using AppointmentCrm.Application.Common;
using AppointmentCrm.Application.Services;
using AppointmentCrm.Contracts;

namespace AppointmentCrm.Api.Services;

internal static class ServiceContractMappings
{
    internal static ServiceInput ToInput(this CreateServiceRequest request) =>
        new(request.Name, request.DurationMinutes, request.Price, request.Currency);

    internal static ServiceInput ToInput(this UpdateServiceRequest request) =>
        new(request.Name, request.DurationMinutes, request.Price, request.Currency);

    internal static PagedResponse<ServiceResponse> ToResponse(
        this PagedResult<ServiceSummary> result) =>
        new(
            result.Items.Select(service => service.ToResponse()).ToList(),
            result.Page,
            result.PageSize,
            result.TotalCount,
            result.TotalPages);

    internal static ServiceResponse ToResponse(this ServiceSummary service) =>
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
