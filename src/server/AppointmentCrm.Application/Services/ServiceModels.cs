using AppointmentCrm.Application.Common;

namespace AppointmentCrm.Application.Services;

public sealed record ServiceSummary(
    Guid Id,
    string Name,
    int DurationMinutes,
    decimal Price,
    string Currency,
    bool IsActive,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc);

public sealed record ServiceInput(
    string Name,
    int DurationMinutes,
    decimal Price,
    string Currency);

public interface IServiceCatalogService
{
    Task<PagedResult<ServiceSummary>> ListAsync(
        PageRequest request,
        bool? isActive,
        CancellationToken cancellationToken);

    Task<ServiceSummary?> GetAsync(Guid serviceId, CancellationToken cancellationToken);

    Task<ServiceSummary> CreateAsync(
        ServiceInput input,
        CancellationToken cancellationToken);

    Task<ServiceSummary?> UpdateAsync(
        Guid serviceId,
        ServiceInput input,
        CancellationToken cancellationToken);

    Task<ServiceSummary?> SetActiveAsync(
        Guid serviceId,
        bool isActive,
        CancellationToken cancellationToken);
}
