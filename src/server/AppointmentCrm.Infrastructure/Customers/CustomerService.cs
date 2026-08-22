using AppointmentCrm.Application.Common;
using AppointmentCrm.Application.Customers;
using AppointmentCrm.Application.Tenancy;
using AppointmentCrm.Domain.Customers;
using AppointmentCrm.Infrastructure.Auditing;
using AppointmentCrm.Infrastructure.Common;
using AppointmentCrm.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AppointmentCrm.Infrastructure.Customers;

internal sealed class CustomerService(
    AppointmentCrmDbContext dbContext,
    ITenantContext tenantContext,
    AuditWriter auditWriter,
    TimeProvider timeProvider) : ICustomerService
{
    public async Task<PagedResult<CustomerSummary>> ListAsync(
        PageRequest request,
        bool includeArchived,
        CancellationToken cancellationToken)
    {
        IQueryable<Customer> query = dbContext.Customers.AsNoTracking();
        if (!includeArchived)
        {
            query = query.Where(customer => customer.ArchivedAtUtc == null);
        }

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            string text = request.Search.Trim().ToUpperInvariant();
            string digits = string.Concat(request.Search.Where(char.IsAsciiDigit));
            query = query.Where(customer =>
                customer.NormalizedName.Contains(text)
                || (customer.NormalizedEmail != null && customer.NormalizedEmail.Contains(text))
                || (digits.Length > 0
                    && customer.NormalizedPhone != null
                    && customer.NormalizedPhone.Contains(digits)));
        }

        int totalCount = await query.CountAsync(cancellationToken);
        query = Order(query, request);
        List<CustomerSummary> items = await query
            .Skip(request.Skip)
            .Take(request.PageSize)
            .Select(customer => ToSummary(customer))
            .ToListAsync(cancellationToken);
        return new PagedResult<CustomerSummary>(
            items,
            request.Page,
            request.PageSize,
            totalCount);
    }

    public Task<CustomerSummary?> GetAsync(
        Guid customerId,
        CancellationToken cancellationToken) =>
        dbContext.Customers
            .AsNoTracking()
            .Where(customer => customer.Id == customerId)
            .Select(customer => ToSummary(customer))
            .SingleOrDefaultAsync(cancellationToken);

    public async Task<CustomerSummary> CreateAsync(
        CustomerInput input,
        CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow();
        Customer customer = Customer.Create(
            Guid.NewGuid(),
            tenantContext.TenantId,
            input.Name,
            input.Email,
            input.Phone,
            input.Notes,
            now);
        await EnsureContactIsUniqueAsync(customer, null, cancellationToken);

        dbContext.Customers.Add(customer);
        auditWriter.Add("customer.created", "customer", customer.Id, now);
        await SaveAsync(cancellationToken);
        return ToSummary(customer);
    }

    public async Task<CustomerSummary?> UpdateAsync(
        Guid customerId,
        CustomerInput input,
        CancellationToken cancellationToken)
    {
        Customer? customer = await dbContext.Customers.SingleOrDefaultAsync(
            candidate => candidate.Id == customerId,
            cancellationToken);
        if (customer is null)
        {
            return null;
        }

        try
        {
            customer.UpdateContact(
                input.Name,
                input.Email,
                input.Phone,
                input.Notes,
                timeProvider.GetUtcNow());
        }
        catch (InvalidOperationException exception)
        {
            throw new MasterDataConflictException(exception.Message);
        }

        await EnsureContactIsUniqueAsync(customer, customer.Id, cancellationToken);
        await SaveAsync(cancellationToken);
        return ToSummary(customer);
    }

    public async Task<bool> ArchiveAsync(
        Guid customerId,
        CancellationToken cancellationToken)
    {
        Customer? customer = await dbContext.Customers.SingleOrDefaultAsync(
            candidate => candidate.Id == customerId,
            cancellationToken);
        if (customer is null)
        {
            return false;
        }

        var now = timeProvider.GetUtcNow();
        if (customer.Archive(now))
        {
            auditWriter.Add("customer.archived", "customer", customer.Id, now);
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        return true;
    }

    private async Task EnsureContactIsUniqueAsync(
        Customer customer,
        Guid? excludedCustomerId,
        CancellationToken cancellationToken)
    {
        if (customer.NormalizedEmail is not null
            && await dbContext.Customers.AnyAsync(
                candidate => candidate.Id != excludedCustomerId
                    && candidate.NormalizedEmail == customer.NormalizedEmail,
                cancellationToken))
        {
            throw new MasterDataConflictException(
                "A customer with the same email already exists in this tenant.");
        }

        if (customer.NormalizedPhone is not null
            && await dbContext.Customers.AnyAsync(
                candidate => candidate.Id != excludedCustomerId
                    && candidate.NormalizedPhone == customer.NormalizedPhone,
                cancellationToken))
        {
            throw new MasterDataConflictException(
                "A customer with the same phone already exists in this tenant.");
        }
    }

    private async Task SaveAsync(CancellationToken cancellationToken)
    {
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (DatabaseConflict.IsUniqueConstraint(
            exception,
            "ux_customers_tenant_email",
            "ux_customers_tenant_phone"))
        {
            throw new MasterDataConflictException(
                "A customer with the same email or phone already exists in this tenant.");
        }
    }

    private static IQueryable<Customer> Order(IQueryable<Customer> query, PageRequest request) =>
        (request.SortBy, request.Descending) switch
        {
            ("name", false) => query.OrderBy(customer => customer.NormalizedName),
            ("name", true) => query.OrderByDescending(customer => customer.NormalizedName),
            ("createdAt", false) => query.OrderBy(customer => customer.CreatedAtUtc),
            ("createdAt", true) => query.OrderByDescending(customer => customer.CreatedAtUtc),
            ("updatedAt", false) => query.OrderBy(customer => customer.UpdatedAtUtc),
            ("updatedAt", true) => query.OrderByDescending(customer => customer.UpdatedAtUtc),
            _ => throw new ArgumentException("Customer sort field is not valid.", nameof(request)),
        };

    private static CustomerSummary ToSummary(Customer customer) =>
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
