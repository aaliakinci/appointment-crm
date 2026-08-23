using System.Diagnostics;
using System.Text.Json;
using AppointmentCrm.Application.Appointments;
using AppointmentCrm.Application.Auditing;
using AppointmentCrm.Application.Common;
using AppointmentCrm.Application.Scheduling;
using AppointmentCrm.Application.Tenancy;
using AppointmentCrm.Domain.Appointments;
using AppointmentCrm.Domain.Customers;
using AppointmentCrm.Domain.Outbox;
using AppointmentCrm.Domain.Services;
using AppointmentCrm.Infrastructure.Auditing;
using AppointmentCrm.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace AppointmentCrm.Infrastructure.Appointments;

internal sealed class AppointmentService(
    AppointmentCrmDbContext dbContext,
    ITenantContext tenantContext,
    ICurrentActor currentActor,
    ISchedulingService schedulingService,
    AuditWriter auditWriter,
    TimeProvider timeProvider) : IAppointmentService
{
    private static readonly JsonSerializerOptions OutboxJsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<PagedResult<AppointmentSummary>> ListAsync(
        PageRequest request,
        AppointmentListFilter filter,
        AppointmentAccessScope accessScope,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(filter);
        ValidateDateRange(filter.FromDate, filter.ToDate);
        TimeZoneInfo timeZone = await GetTenantTimeZoneAsync(cancellationToken);
        DateTimeOffset fromUtc = ResolveStartOfLocalDate(filter.FromDate, timeZone);
        DateTimeOffset toUtc = ResolveStartOfLocalDate(filter.ToDate.AddDays(1), timeZone);

        IQueryable<Appointment> query = dbContext.Appointments
            .AsNoTracking()
            .Include(appointment => appointment.Customer)
            .Include(appointment => appointment.Employee)
            .Where(appointment => appointment.StartsAtUtc >= fromUtc
                && appointment.StartsAtUtc < toUtc);
        query = await ApplyAccessScopeAsync(query, accessScope, cancellationToken);
        if (filter.EmployeeId.HasValue)
        {
            query = query.Where(appointment => appointment.EmployeeId == filter.EmployeeId);
        }

        if (filter.CustomerId.HasValue)
        {
            query = query.Where(appointment => appointment.CustomerId == filter.CustomerId);
        }

        if (filter.Status.HasValue)
        {
            query = query.Where(appointment => appointment.Status == filter.Status);
        }

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            string pattern = $"%{request.Search}%";
            query = query.Where(appointment =>
                EF.Functions.ILike(appointment.Customer.Name, pattern)
                || EF.Functions.ILike(appointment.Employee.Name, pattern)
                || EF.Functions.ILike(appointment.ServiceName, pattern));
        }

        int totalCount = await query.CountAsync(cancellationToken);
        List<Appointment> appointments = await OrderAppointments(query, request)
            .Skip(request.Skip)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);
        return new PagedResult<AppointmentSummary>(
            appointments.Select(appointment => ToSummary(appointment, timeZone)).ToList(),
            request.Page,
            request.PageSize,
            totalCount);
    }

    public async Task<AppointmentDetail> GetAsync(
        Guid appointmentId,
        AppointmentAccessScope accessScope,
        CancellationToken cancellationToken)
    {
        TimeZoneInfo timeZone = await GetTenantTimeZoneAsync(cancellationToken);
        IQueryable<Appointment> query = dbContext.Appointments
            .AsNoTracking()
            .Include(appointment => appointment.Customer)
            .Include(appointment => appointment.Employee)
            .Include(appointment => appointment.StatusHistory)
                .ThenInclude(history => history.ActorMembership)
                    .ThenInclude(membership => membership.User);
        query = await ApplyAccessScopeAsync(query, accessScope, cancellationToken);
        Appointment appointment = await query.SingleOrDefaultAsync(
            candidate => candidate.Id == appointmentId,
            cancellationToken)
            ?? throw NotFound();
        return ToDetail(appointment, timeZone);
    }

    public async Task<PagedResult<AppointmentSummary>> ListCustomerHistoryAsync(
        Guid customerId,
        PageRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        bool customerExists = await dbContext.Customers
            .AsNoTracking()
            .AnyAsync(customer => customer.Id == customerId, cancellationToken);
        if (!customerExists)
        {
            throw new ApplicationNotFoundException(
                AppointmentErrorCodes.CustomerNotFound,
                "The requested customer was not found.");
        }

        TimeZoneInfo timeZone = await GetTenantTimeZoneAsync(cancellationToken);
        IQueryable<Appointment> query = dbContext.Appointments
            .AsNoTracking()
            .Include(appointment => appointment.Customer)
            .Include(appointment => appointment.Employee)
            .Where(appointment => appointment.CustomerId == customerId);
        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            string pattern = $"%{request.Search}%";
            query = query.Where(appointment =>
                EF.Functions.ILike(appointment.Employee.Name, pattern)
                || EF.Functions.ILike(appointment.ServiceName, pattern));
        }

        int totalCount = await query.CountAsync(cancellationToken);
        List<Appointment> appointments = await OrderAppointments(query, request)
            .Skip(request.Skip)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);
        return new PagedResult<AppointmentSummary>(
            appointments.Select(appointment => ToSummary(appointment, timeZone)).ToList(),
            request.Page,
            request.PageSize,
            totalCount);
    }

    public async Task<AppointmentDetail> CreateAsync(
        CreateAppointmentInput input,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(input);
        Customer customer = await dbContext.Customers.SingleOrDefaultAsync(
            candidate => candidate.Id == input.CustomerId,
            cancellationToken)
            ?? throw new ApplicationNotFoundException(
                AppointmentErrorCodes.CustomerNotFound,
                "The requested customer was not found.");
        if (customer.ArchivedAtUtc is not null)
        {
            throw new ApplicationConflictException(
                AppointmentErrorCodes.CustomerArchived,
                "An archived customer cannot receive a new appointment.");
        }

        Employee employee = await dbContext.Employees.SingleOrDefaultAsync(
            candidate => candidate.Id == input.EmployeeId,
            cancellationToken)
            ?? throw new ApplicationNotFoundException(
                AppointmentErrorCodes.EmployeeNotFound,
                "The requested employee was not found.");
        if (!employee.IsActive)
        {
            throw new ApplicationConflictException(
                AppointmentErrorCodes.EmployeeInactive,
                "An inactive employee cannot receive a new appointment.");
        }

        ServiceOffering service = await dbContext.Services.SingleOrDefaultAsync(
            candidate => candidate.Id == input.ServiceId,
            cancellationToken)
            ?? throw new ApplicationNotFoundException(
                AppointmentErrorCodes.ServiceNotFound,
                "The requested service was not found.");
        if (!service.IsActive)
        {
            throw new ApplicationConflictException(
                AppointmentErrorCodes.ServiceInactive,
                "An inactive service cannot be booked.");
        }

        await EnsureServiceAssignmentAsync(employee.Id, service.Id, cancellationToken);
        DateTimeOffset startsAtUtc = input.StartsAtUtc.ToUniversalTime();
        await EnsureAvailableAsync(
            startsAtUtc,
            employee.Id,
            service.Id,
            excludeAppointmentId: null,
            cancellationToken);

        var now = timeProvider.GetUtcNow();
        Appointment appointment;
        try
        {
            appointment = Appointment.Create(
                Guid.NewGuid(),
                tenantContext.TenantId,
                customer.Id,
                employee.Id,
                service.Id,
                startsAtUtc,
                service.Name,
                service.DurationMinutes,
                service.Price,
                service.Currency,
                input.Notes,
                currentActor.UserId,
                currentActor.MembershipId,
                now);
        }
        catch (ArgumentException exception)
        {
            throw ApplicationValidationException.FromArgument(exception);
        }

        dbContext.Appointments.Add(appointment);
        AddAuditAndOutbox("created", appointment, now);
        await SaveAsync(cancellationToken);
        return await GetAsync(appointment.Id, AppointmentAccessScope.Tenant, cancellationToken);
    }

    public async Task<AppointmentDetail> RescheduleAsync(
        Guid appointmentId,
        RescheduleAppointmentInput input,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(input);
        Appointment appointment = await GetTrackedAsync(appointmentId, cancellationToken);
        EnsureExpectedRevision(appointment, input.ExpectedRevision);
        DateTimeOffset startsAtUtc = input.StartsAtUtc.ToUniversalTime();
        await EnsureAvailableAsync(
            startsAtUtc,
            appointment.EmployeeId,
            appointment.ServiceId,
            appointment.Id,
            cancellationToken);

        var now = timeProvider.GetUtcNow();
        try
        {
            appointment.Reschedule(startsAtUtc, input.ExpectedRevision, now);
        }
        catch (InvalidOperationException exception)
        {
            throw InvalidTransition(exception);
        }

        AddAuditAndOutbox("rescheduled", appointment, now);
        await SaveAsync(cancellationToken);
        return await GetAsync(appointment.Id, AppointmentAccessScope.Tenant, cancellationToken);
    }

    public async Task<AppointmentDetail> TransitionAsync(
        Guid appointmentId,
        AppointmentStatus targetStatus,
        TransitionAppointmentInput input,
        AppointmentAccessScope accessScope,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(input);
        Appointment appointment = await GetTrackedAsync(appointmentId, cancellationToken);
        if (accessScope == AppointmentAccessScope.CurrentEmployee)
        {
            Guid employeeId = await GetCurrentEmployeeIdAsync(cancellationToken);
            if (appointment.EmployeeId != employeeId)
            {
                throw NotFound();
            }

            if (targetStatus is not AppointmentStatus.Confirmed
                and not AppointmentStatus.Completed
                and not AppointmentStatus.NoShow)
            {
                throw new ApplicationForbiddenException(
                    AppointmentErrorCodes.TransitionForbidden,
                    "The employee cannot perform the requested appointment transition.");
            }
        }

        EnsureExpectedRevision(appointment, input.ExpectedRevision);
        var now = timeProvider.GetUtcNow();
        try
        {
            AppointmentStatusHistory history = appointment.TransitionTo(
                targetStatus,
                input.ExpectedRevision,
                input.Reason,
                currentActor.UserId,
                currentActor.MembershipId,
                now);
            dbContext.AppointmentStatusHistory.Add(history);
        }
        catch (InvalidOperationException exception)
        {
            throw InvalidTransition(exception);
        }
        catch (ArgumentException exception)
        {
            throw ApplicationValidationException.FromArgument(exception);
        }

        AddAuditAndOutbox(StatusValue(targetStatus), appointment, now);
        await SaveAsync(cancellationToken);
        return await GetAsync(appointment.Id, accessScope, cancellationToken);
    }

    private async Task<IQueryable<Appointment>> ApplyAccessScopeAsync(
        IQueryable<Appointment> query,
        AppointmentAccessScope accessScope,
        CancellationToken cancellationToken)
    {
        if (accessScope == AppointmentAccessScope.Tenant)
        {
            return query;
        }

        Guid employeeId = await GetCurrentEmployeeIdAsync(cancellationToken);
        return query.Where(appointment => appointment.EmployeeId == employeeId);
    }

    private async Task<Guid> GetCurrentEmployeeIdAsync(CancellationToken cancellationToken) =>
        await dbContext.Employees
            .AsNoTracking()
            .Where(employee => employee.UserId == currentActor.UserId)
            .Select(employee => (Guid?)employee.Id)
            .SingleOrDefaultAsync(cancellationToken)
        ?? throw new ApplicationNotFoundException(
            AppointmentErrorCodes.CurrentEmployeeNotFound,
            "The current account is not linked to an employee.");

    private async Task<Appointment> GetTrackedAsync(
        Guid appointmentId,
        CancellationToken cancellationToken) =>
        await dbContext.Appointments.SingleOrDefaultAsync(
            candidate => candidate.Id == appointmentId,
            cancellationToken)
        ?? throw NotFound();

    private async Task EnsureServiceAssignmentAsync(
        Guid employeeId,
        Guid serviceId,
        CancellationToken cancellationToken)
    {
        bool assigned = await dbContext.EmployeeServices.AnyAsync(
            assignment => assignment.EmployeeId == employeeId
                && assignment.ServiceId == serviceId,
            cancellationToken);
        if (!assigned)
        {
            throw new ApplicationConflictException(
                AppointmentErrorCodes.ServiceNotAssigned,
                "The employee is not assigned to the requested service.");
        }
    }

    private async Task EnsureAvailableAsync(
        DateTimeOffset startsAtUtc,
        Guid employeeId,
        Guid serviceId,
        Guid? excludeAppointmentId,
        CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow();
        if (startsAtUtc <= now)
        {
            throw new ApplicationValidationException(
                AppointmentErrorCodes.SlotUnavailable,
                new Dictionary<string, string[]>
                {
                    ["startsAtUtc"] = ["Appointment start must be in the future."],
                });
        }

        TimeZoneInfo timeZone = await GetTenantTimeZoneAsync(cancellationToken);
        DateOnly localDate = DateOnly.FromDateTime(
            TimeZoneInfo.ConvertTime(startsAtUtc, timeZone).DateTime);
        AvailabilityDay availability = await schedulingService.GetAvailabilityAsync(
            new AvailabilityQuery(localDate, employeeId, serviceId, excludeAppointmentId),
            cancellationToken);
        if (!availability.Slots.Any(slot => slot.StartUtc == startsAtUtc))
        {
            throw new ApplicationConflictException(
                AppointmentErrorCodes.SlotUnavailable,
                "The requested appointment slot is no longer available.");
        }
    }

    private async Task<TimeZoneInfo> GetTenantTimeZoneAsync(CancellationToken cancellationToken)
    {
        string timeZoneId = await dbContext.Tenants
            .AsNoTracking()
            .Select(tenant => tenant.TimeZone)
            .SingleAsync(cancellationToken);
        return TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
    }

    private void AddAuditAndOutbox(
        string eventName,
        Appointment appointment,
        DateTimeOffset occurredAtUtc)
    {
        string status = StatusValue(appointment.Status);
        auditWriter.Add(
            $"appointment.{eventName}",
            "appointment",
            appointment.Id,
            occurredAtUtc,
            $"employeeId={appointment.EmployeeId};status={status};revision={appointment.Revision}");
        string payload = JsonSerializer.Serialize(
            new
            {
                appointmentId = appointment.Id,
                appointment.CustomerId,
                appointment.EmployeeId,
                appointment.ServiceId,
                appointment.StartsAtUtc,
                appointment.EndsAtUtc,
                status,
                appointment.Revision,
            },
            OutboxJsonOptions);
        dbContext.OutboxMessages.Add(OutboxMessage.Create(
            Guid.NewGuid(),
            tenantContext.TenantId,
            $"appointment.{eventName}",
            "appointment",
            appointment.Id,
            payload,
            occurredAtUtc,
            Activity.Current?.Id,
            Activity.Current?.TraceStateString,
            Activity.Current?.GetBaggageItem("app.correlation_id")));
    }

    private async Task SaveAsync(CancellationToken cancellationToken)
    {
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException exception)
        {
            throw VersionConflict(exception);
        }
        catch (DbUpdateException exception) when (
            exception.InnerException is PostgresException
            {
                SqlState: PostgresErrorCodes.ExclusionViolation,
                ConstraintName: "ex_appointments_no_employee_overlap",
            })
        {
            throw new ApplicationConflictException(
                AppointmentErrorCodes.TimeConflict,
                "The employee already has an appointment in the requested time range.",
                exception);
        }
    }

    private static void EnsureExpectedRevision(Appointment appointment, long expectedRevision)
    {
        if (appointment.Revision != expectedRevision)
        {
            throw VersionConflict();
        }
    }

    private static void ValidateDateRange(DateOnly fromDate, DateOnly toDate)
    {
        if (fromDate > toDate || toDate.DayNumber - fromDate.DayNumber > 30)
        {
            throw new ApplicationValidationException(
                AppointmentErrorCodes.InvalidDateRange,
                new Dictionary<string, string[]>
                {
                    ["toDate"] = ["Date range must be ordered and cannot exceed 31 days."],
                });
        }
    }

    private static DateTimeOffset ResolveStartOfLocalDate(DateOnly date, TimeZoneInfo timeZone)
    {
        DateTime local = DateTime.SpecifyKind(
            date.ToDateTime(TimeOnly.MinValue),
            DateTimeKind.Unspecified);
        while (timeZone.IsInvalidTime(local))
        {
            local = local.AddMinutes(1);
        }

        if (timeZone.IsAmbiguousTime(local))
        {
            TimeSpan offset = timeZone.GetAmbiguousTimeOffsets(local).Max();
            return new DateTimeOffset(local, offset).ToUniversalTime();
        }

        return new DateTimeOffset(TimeZoneInfo.ConvertTimeToUtc(local, timeZone), TimeSpan.Zero);
    }

    private static IQueryable<Appointment> OrderAppointments(
        IQueryable<Appointment> query,
        PageRequest request) =>
        (request.SortBy, request.Descending) switch
        {
            ("start", true) => query.OrderByDescending(item => item.StartsAtUtc),
            ("createdAt", false) => query.OrderBy(item => item.CreatedAtUtc),
            ("createdAt", true) => query.OrderByDescending(item => item.CreatedAtUtc),
            ("updatedAt", false) => query.OrderBy(item => item.UpdatedAtUtc),
            ("updatedAt", true) => query.OrderByDescending(item => item.UpdatedAtUtc),
            ("customer", false) => query.OrderBy(item => item.Customer.NormalizedName),
            ("customer", true) => query.OrderByDescending(item => item.Customer.NormalizedName),
            ("employee", false) => query.OrderBy(item => item.Employee.NormalizedName),
            ("employee", true) => query.OrderByDescending(item => item.Employee.NormalizedName),
            ("status", false) => query.OrderBy(item => item.Status),
            ("status", true) => query.OrderByDescending(item => item.Status),
            _ => query.OrderBy(item => item.StartsAtUtc),
        };

    private static AppointmentDetail ToDetail(Appointment appointment, TimeZoneInfo timeZone) =>
        new(
            ToSummary(appointment, timeZone),
            appointment.StatusHistory
                .OrderBy(history => history.OccurredAtUtc)
                .Select(history => new AppointmentStatusHistorySummary(
                    history.Id,
                    history.FromStatus,
                    history.ToStatus,
                    history.ActorMembership.User.DisplayName,
                    history.Reason,
                    history.OccurredAtUtc))
                .ToList());

    private static AppointmentSummary ToSummary(Appointment appointment, TimeZoneInfo timeZone) =>
        new(
            appointment.Id,
            appointment.CustomerId,
            appointment.Customer.Name,
            appointment.EmployeeId,
            appointment.Employee.Name,
            appointment.ServiceId,
            appointment.ServiceName,
            appointment.ServiceDurationMinutes,
            appointment.ServicePrice,
            appointment.ServiceCurrency,
            appointment.Status,
            appointment.StartsAtUtc,
            appointment.EndsAtUtc,
            TimeZoneInfo.ConvertTime(appointment.StartsAtUtc, timeZone),
            TimeZoneInfo.ConvertTime(appointment.EndsAtUtc, timeZone),
            timeZone.Id,
            appointment.Notes,
            appointment.Revision,
            appointment.CreatedAtUtc,
            appointment.UpdatedAtUtc);

    private static string StatusValue(AppointmentStatus status) =>
        status switch
        {
            AppointmentStatus.NoShow => "no-show",
            _ => status.ToString().ToLowerInvariant(),
        };

    private static ApplicationNotFoundException NotFound() =>
        new(
            AppointmentErrorCodes.NotFound,
            "The requested appointment was not found.");

    private static ApplicationConflictException InvalidTransition(Exception exception) =>
        new(
            AppointmentErrorCodes.InvalidTransition,
            exception.Message,
            exception);

    private static ApplicationConflictException VersionConflict(Exception? exception = null) =>
        new(
            AppointmentErrorCodes.VersionConflict,
            "The appointment was changed by another request. Reload it and try again.",
            exception);
}
