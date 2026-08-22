using AppointmentCrm.Application.Common;

namespace AppointmentCrm.Application.Customers;

public sealed record CustomerSummary(
    Guid Id,
    string Name,
    string? Email,
    string? Phone,
    string? Notes,
    DateTimeOffset? ArchivedAtUtc,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc);

public sealed record CustomerInput(
    string Name,
    string? Email,
    string? Phone,
    string? Notes);

public interface ICustomerService
{
    Task<PagedResult<CustomerSummary>> ListAsync(
        PageRequest request,
        bool includeArchived,
        CancellationToken cancellationToken);

    Task<CustomerSummary?> GetAsync(Guid customerId, CancellationToken cancellationToken);

    Task<CustomerSummary> CreateAsync(
        CustomerInput input,
        CancellationToken cancellationToken);

    Task<CustomerSummary?> UpdateAsync(
        Guid customerId,
        CustomerInput input,
        CancellationToken cancellationToken);

    Task<bool> ArchiveAsync(Guid customerId, CancellationToken cancellationToken);
}
