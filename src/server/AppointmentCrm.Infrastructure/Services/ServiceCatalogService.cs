using AppointmentCrm.Application.Common;
using AppointmentCrm.Application.Services;
using AppointmentCrm.Application.Tenancy;
using AppointmentCrm.Domain.Services;
using AppointmentCrm.Infrastructure.Auditing;
using AppointmentCrm.Infrastructure.Common;
using AppointmentCrm.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AppointmentCrm.Infrastructure.Services;

internal sealed class ServiceCatalogService(
    AppointmentCrmDbContext dbContext,
    ITenantContext tenantContext,
    AuditWriter auditWriter,
    TimeProvider timeProvider) : IServiceCatalogService
{
    public async Task<PagedResult<ServiceSummary>> ListAsync(
        PageRequest request,
        bool? isActive,
        CancellationToken cancellationToken)
    {
        IQueryable<ServiceOffering> query = dbContext.Services.AsNoTracking();
        if (isActive.HasValue)
        {
            query = query.Where(service => service.IsActive == isActive.Value);
        }

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            string search = request.Search.Trim().ToUpperInvariant();
            query = query.Where(service => service.NormalizedName.Contains(search));
        }

        int totalCount = await query.CountAsync(cancellationToken);
        query = Order(query, request);
        List<ServiceSummary> items = await query
            .Skip(request.Skip)
            .Take(request.PageSize)
            .Select(service => ToSummary(service))
            .ToListAsync(cancellationToken);
        return new PagedResult<ServiceSummary>(
            items,
            request.Page,
            request.PageSize,
            totalCount);
    }

    public Task<ServiceSummary?> GetAsync(
        Guid serviceId,
        CancellationToken cancellationToken) =>
        dbContext.Services
            .AsNoTracking()
            .Where(service => service.Id == serviceId)
            .Select(service => ToSummary(service))
            .SingleOrDefaultAsync(cancellationToken);

    public async Task<ServiceSummary> CreateAsync(
        ServiceInput input,
        CancellationToken cancellationToken)
    {
        await EnsureTenantCurrencyAsync(input.Currency, cancellationToken);
        var now = timeProvider.GetUtcNow();
        ServiceOffering service;
        try
        {
            service = ServiceOffering.Create(
                Guid.NewGuid(),
                tenantContext.TenantId,
                input.Name,
                input.DurationMinutes,
                input.Price,
                input.Currency,
                now);
        }
        catch (ArgumentException exception)
        {
            throw ToValidationException(exception);
        }
        await EnsureNameIsUniqueAsync(service, null, cancellationToken);

        dbContext.Services.Add(service);
        auditWriter.Add("service.created", "service", service.Id, now, "active=true");
        await SaveAsync(cancellationToken);
        return ToSummary(service);
    }

    public async Task<ServiceSummary?> UpdateAsync(
        Guid serviceId,
        ServiceInput input,
        CancellationToken cancellationToken)
    {
        ServiceOffering? service = await dbContext.Services.SingleOrDefaultAsync(
            candidate => candidate.Id == serviceId,
            cancellationToken);
        if (service is null)
        {
            return null;
        }

        await EnsureTenantCurrencyAsync(input.Currency, cancellationToken);
        try
        {
            service.Update(
                input.Name,
                input.DurationMinutes,
                input.Price,
                input.Currency,
                timeProvider.GetUtcNow());
        }
        catch (ArgumentException exception)
        {
            throw ToValidationException(exception);
        }
        await EnsureNameIsUniqueAsync(service, service.Id, cancellationToken);
        await SaveAsync(cancellationToken);
        return ToSummary(service);
    }

    public async Task<ServiceSummary?> SetActiveAsync(
        Guid serviceId,
        bool isActive,
        CancellationToken cancellationToken)
    {
        ServiceOffering? service = await dbContext.Services.SingleOrDefaultAsync(
            candidate => candidate.Id == serviceId,
            cancellationToken);
        if (service is null)
        {
            return null;
        }

        var now = timeProvider.GetUtcNow();
        if (service.SetActive(isActive, now))
        {
            auditWriter.Add(
                "service.activation-changed",
                "service",
                service.Id,
                now,
                $"active={isActive.ToString().ToLowerInvariant()}");
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        return ToSummary(service);
    }

    private async Task EnsureTenantCurrencyAsync(
        string currency,
        CancellationToken cancellationToken)
    {
        string normalizedCurrency;
        try
        {
            normalizedCurrency = ServiceOffering.NormalizeCurrency(currency);
        }
        catch (ArgumentException exception)
        {
            throw ApplicationValidationException.FromArgument(exception, "currency");
        }
        string tenantCurrency = await dbContext.Tenants
            .Select(tenant => tenant.Currency)
            .SingleAsync(cancellationToken);
        if (!string.Equals(normalizedCurrency, tenantCurrency, StringComparison.Ordinal))
        {
            throw new ApplicationConflictException(
                ServiceErrorCodes.CurrencyMismatch,
                $"Service currency must match the tenant currency '{tenantCurrency}'.");
        }
    }

    private async Task EnsureNameIsUniqueAsync(
        ServiceOffering service,
        Guid? excludedServiceId,
        CancellationToken cancellationToken)
    {
        if (await dbContext.Services.AnyAsync(
            candidate => candidate.Id != excludedServiceId
                && candidate.NormalizedName == service.NormalizedName,
            cancellationToken))
        {
            throw new ApplicationConflictException(
                ServiceErrorCodes.NameConflict,
                "A service with the same name already exists in this tenant.");
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
            "ux_services_tenant_name"))
        {
            throw new ApplicationConflictException(
                ServiceErrorCodes.NameConflict,
                "A service with the same name already exists in this tenant.",
                exception);
        }
    }

    private static ApplicationValidationException ToValidationException(
        ArgumentException exception)
    {
        string? field = string.Equals(exception.ParamName, "value", StringComparison.Ordinal)
            ? "currency"
            : exception.ParamName;
        return ApplicationValidationException.FromArgument(exception, field);
    }

    private static IQueryable<ServiceOffering> Order(
        IQueryable<ServiceOffering> query,
        PageRequest request) =>
        (request.SortBy, request.Descending) switch
        {
            ("name", false) => query.OrderBy(service => service.NormalizedName),
            ("name", true) => query.OrderByDescending(service => service.NormalizedName),
            ("price", false) => query.OrderBy(service => service.Price),
            ("price", true) => query.OrderByDescending(service => service.Price),
            ("duration", false) => query.OrderBy(service => service.DurationMinutes),
            ("duration", true) => query.OrderByDescending(service => service.DurationMinutes),
            ("updatedAt", false) => query.OrderBy(service => service.UpdatedAtUtc),
            ("updatedAt", true) => query.OrderByDescending(service => service.UpdatedAtUtc),
            _ => throw new ArgumentException("Service sort field is not valid.", nameof(request)),
        };

    private static ServiceSummary ToSummary(ServiceOffering service) =>
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
