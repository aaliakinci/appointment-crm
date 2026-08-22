using AppointmentCrm.Application.Common;
using AppointmentCrm.Application.Employees;
using AppointmentCrm.Application.Tenancy;
using AppointmentCrm.Domain.Identity;
using AppointmentCrm.Domain.Services;
using AppointmentCrm.Infrastructure.Auditing;
using AppointmentCrm.Infrastructure.Common;
using AppointmentCrm.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AppointmentCrm.Infrastructure.Employees;

internal sealed class EmployeeManagementService(
    AppointmentCrmDbContext dbContext,
    ITenantContext tenantContext,
    AuditWriter auditWriter,
    TimeProvider timeProvider) : IEmployeeManagementService
{
    public async Task<PagedResult<EmployeeSummary>> ListAsync(
        PageRequest request,
        bool? isActive,
        Guid? serviceId,
        CancellationToken cancellationToken)
    {
        IQueryable<Employee> query = dbContext.Employees.AsNoTracking();
        if (isActive.HasValue)
        {
            query = query.Where(employee => employee.IsActive == isActive.Value);
        }

        if (serviceId.HasValue)
        {
            query = query.Where(employee => employee.ServiceAssignments.Any(
                assignment => assignment.ServiceId == serviceId.Value));
        }

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            string text = request.Search.Trim().ToUpperInvariant();
            string digits = string.Concat(request.Search.Where(char.IsAsciiDigit));
            query = query.Where(employee =>
                employee.NormalizedName.Contains(text)
                || (employee.NormalizedEmail != null && employee.NormalizedEmail.Contains(text))
                || (digits.Length > 0
                    && employee.NormalizedPhone != null
                    && employee.NormalizedPhone.Contains(digits)));
        }

        int totalCount = await query.CountAsync(cancellationToken);
        query = Order(query, request);
        List<EmployeeSummary> items = await Project(query
                .Skip(request.Skip)
                .Take(request.PageSize))
            .ToListAsync(cancellationToken);
        return new PagedResult<EmployeeSummary>(
            items,
            request.Page,
            request.PageSize,
            totalCount);
    }

    public Task<EmployeeSummary?> GetAsync(
        Guid employeeId,
        CancellationToken cancellationToken) =>
        Project(dbContext.Employees
                .AsNoTracking()
                .Where(employee => employee.Id == employeeId))
            .SingleOrDefaultAsync(cancellationToken);

    public async Task<EmployeeSummary> CreateAsync(
        EmployeeInput input,
        IReadOnlyCollection<Guid> serviceIds,
        CancellationToken cancellationToken)
    {
        await EnsureUserLinkIsAvailableAsync(input.UserId, null, cancellationToken);
        IReadOnlyList<Guid> normalizedServiceIds = await ValidateServiceIdsAsync(
            serviceIds,
            cancellationToken);
        var now = timeProvider.GetUtcNow();
        Employee employee = Employee.Create(
            Guid.NewGuid(),
            tenantContext.TenantId,
            input.UserId,
            input.Name,
            input.Email,
            input.Phone,
            now);

        dbContext.Employees.Add(employee);
        dbContext.EmployeeServices.AddRange(normalizedServiceIds.Select(serviceId =>
            EmployeeService.Create(
                tenantContext.TenantId,
                employee.Id,
                serviceId,
                now)));
        auditWriter.Add("employee.created", "employee", employee.Id, now, "active=true");
        await SaveAsync(cancellationToken);
        return await GetRequiredAsync(employee.Id, cancellationToken);
    }

    public async Task<EmployeeSummary?> UpdateAsync(
        Guid employeeId,
        EmployeeInput input,
        CancellationToken cancellationToken)
    {
        Employee? employee = await dbContext.Employees
            .Include(candidate => candidate.ServiceAssignments)
                .ThenInclude(assignment => assignment.Service)
            .SingleOrDefaultAsync(candidate => candidate.Id == employeeId, cancellationToken);
        if (employee is null)
        {
            return null;
        }

        await EnsureUserLinkIsAvailableAsync(input.UserId, employee.Id, cancellationToken);
        employee.Update(
            input.UserId,
            input.Name,
            input.Email,
            input.Phone,
            timeProvider.GetUtcNow());
        await SaveAsync(cancellationToken);
        return ToSummary(employee);
    }

    public async Task<EmployeeSummary?> SetActiveAsync(
        Guid employeeId,
        bool isActive,
        CancellationToken cancellationToken)
    {
        Employee? employee = await dbContext.Employees
            .Include(candidate => candidate.ServiceAssignments)
                .ThenInclude(assignment => assignment.Service)
            .SingleOrDefaultAsync(candidate => candidate.Id == employeeId, cancellationToken);
        if (employee is null)
        {
            return null;
        }

        var now = timeProvider.GetUtcNow();
        if (employee.SetActive(isActive, now))
        {
            auditWriter.Add(
                "employee.activation-changed",
                "employee",
                employee.Id,
                now,
                $"active={isActive.ToString().ToLowerInvariant()}");
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        return ToSummary(employee);
    }

    public async Task<EmployeeSummary?> SetServicesAsync(
        Guid employeeId,
        IReadOnlyCollection<Guid> serviceIds,
        CancellationToken cancellationToken)
    {
        Employee? employee = await dbContext.Employees
            .Include(candidate => candidate.ServiceAssignments)
                .ThenInclude(assignment => assignment.Service)
            .SingleOrDefaultAsync(candidate => candidate.Id == employeeId, cancellationToken);
        if (employee is null)
        {
            return null;
        }

        IReadOnlyList<Guid> normalizedServiceIds = await ValidateServiceIdsAsync(
            serviceIds,
            cancellationToken);
        HashSet<Guid> requested = normalizedServiceIds.ToHashSet();
        EmployeeService[] removed = employee.ServiceAssignments
            .Where(assignment => !requested.Contains(assignment.ServiceId))
            .ToArray();
        Guid[] existing = employee.ServiceAssignments
            .Select(assignment => assignment.ServiceId)
            .ToArray();
        Guid[] added = normalizedServiceIds.Except(existing).ToArray();
        if (removed.Length == 0 && added.Length == 0)
        {
            return ToSummary(employee);
        }

        var now = timeProvider.GetUtcNow();
        dbContext.EmployeeServices.RemoveRange(removed);
        dbContext.EmployeeServices.AddRange(added.Select(serviceId => EmployeeService.Create(
            tenantContext.TenantId,
            employee.Id,
            serviceId,
            now)));
        auditWriter.Add(
            "employee.services-changed",
            "employee",
            employee.Id,
            now,
            $"serviceCount={normalizedServiceIds.Count}");
        await dbContext.SaveChangesAsync(cancellationToken);
        return await GetRequiredAsync(employee.Id, cancellationToken);
    }

    public async Task<IReadOnlyList<EmployeeUserOption>> ListUserOptionsAsync(
        CancellationToken cancellationToken)
    {
        Guid[] linkedUserIds = await dbContext.Employees
            .Where(employee => employee.UserId != null)
            .Select(employee => employee.UserId!.Value)
            .ToArrayAsync(cancellationToken);
        return await dbContext.TenantMemberships
            .AsNoTracking()
            .Include(membership => membership.User)
            .Where(membership => membership.IsActive && membership.User.IsActive)
            .OrderBy(membership => membership.User.DisplayName)
            .Select(membership => new EmployeeUserOption(
                membership.UserId,
                membership.User.DisplayName,
                membership.User.Email,
                membership.Role,
                linkedUserIds.Contains(membership.UserId)))
            .ToListAsync(cancellationToken);
    }

    private async Task EnsureUserLinkIsAvailableAsync(
        Guid? userId,
        Guid? excludedEmployeeId,
        CancellationToken cancellationToken)
    {
        if (!userId.HasValue)
        {
            return;
        }

        bool membershipExists = await dbContext.TenantMemberships.AnyAsync(
            membership => membership.UserId == userId.Value,
            cancellationToken);
        if (!membershipExists)
        {
            throw new MasterDataConflictException(
                "The linked user must be a member of the active tenant.");
        }

        bool alreadyLinked = await dbContext.Employees.AnyAsync(
            employee => employee.Id != excludedEmployeeId && employee.UserId == userId.Value,
            cancellationToken);
        if (alreadyLinked)
        {
            throw new MasterDataConflictException(
                "The linked user is already assigned to another employee.");
        }
    }

    private async Task<IReadOnlyList<Guid>> ValidateServiceIdsAsync(
        IReadOnlyCollection<Guid> serviceIds,
        CancellationToken cancellationToken)
    {
        Guid[] distinctIds = serviceIds.Distinct().ToArray();
        if (distinctIds.Length != serviceIds.Count)
        {
            throw new ArgumentException("Service assignments cannot contain duplicates.");
        }

        int activeServiceCount = await dbContext.Services.CountAsync(
            service => distinctIds.Contains(service.Id) && service.IsActive,
            cancellationToken);
        if (activeServiceCount != distinctIds.Length)
        {
            throw new MasterDataConflictException(
                "Every assigned service must exist and be active in the current tenant.");
        }

        return distinctIds;
    }

    private async Task<EmployeeSummary> GetRequiredAsync(
        Guid employeeId,
        CancellationToken cancellationToken) =>
        await GetAsync(employeeId, cancellationToken)
        ?? throw new InvalidOperationException("The saved employee could not be reloaded.");

    private async Task SaveAsync(CancellationToken cancellationToken)
    {
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (DatabaseConflict.IsUniqueConstraint(
            exception,
            "ux_employees_tenant_user"))
        {
            throw new MasterDataConflictException(
                "The linked user is already assigned to another employee.");
        }
    }

    private static IQueryable<Employee> Order(
        IQueryable<Employee> query,
        PageRequest request) =>
        (request.SortBy, request.Descending) switch
        {
            ("name", false) => query.OrderBy(employee => employee.NormalizedName),
            ("name", true) => query.OrderByDescending(employee => employee.NormalizedName),
            ("createdAt", false) => query.OrderBy(employee => employee.CreatedAtUtc),
            ("createdAt", true) => query.OrderByDescending(employee => employee.CreatedAtUtc),
            ("updatedAt", false) => query.OrderBy(employee => employee.UpdatedAtUtc),
            ("updatedAt", true) => query.OrderByDescending(employee => employee.UpdatedAtUtc),
            _ => throw new ArgumentException("Employee sort field is not valid.", nameof(request)),
        };

    private static EmployeeSummary ToSummary(Employee employee) =>
        new(
            employee.Id,
            employee.UserId,
            employee.Name,
            employee.Email,
            employee.Phone,
            employee.IsActive,
            employee.ServiceAssignments
                .OrderBy(assignment => assignment.Service.NormalizedName)
                .Select(assignment => new EmployeeServiceSummary(
                    assignment.ServiceId,
                    assignment.Service.Name,
                    assignment.Service.IsActive))
                .ToList(),
            employee.CreatedAtUtc,
            employee.UpdatedAtUtc);

    private static IQueryable<EmployeeSummary> Project(IQueryable<Employee> query) =>
        query.Select(employee => new EmployeeSummary(
            employee.Id,
            employee.UserId,
            employee.Name,
            employee.Email,
            employee.Phone,
            employee.IsActive,
            employee.ServiceAssignments
                .OrderBy(assignment => assignment.Service.NormalizedName)
                .Select(assignment => new EmployeeServiceSummary(
                    assignment.ServiceId,
                    assignment.Service.Name,
                    assignment.Service.IsActive))
                .ToList(),
            employee.CreatedAtUtc,
            employee.UpdatedAtUtc));
}
