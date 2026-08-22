using AppointmentCrm.Application.Auditing;
using AppointmentCrm.Application.Common;
using AppointmentCrm.Application.Scheduling;
using AppointmentCrm.Application.Tenancy;
using AppointmentCrm.Domain.Scheduling;
using AppointmentCrm.Domain.Services;
using AppointmentCrm.Infrastructure.Auditing;
using AppointmentCrm.Infrastructure.Common;
using AppointmentCrm.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace AppointmentCrm.Infrastructure.Scheduling;

internal sealed class SchedulingService(
    AppointmentCrmDbContext dbContext,
    ITenantContext tenantContext,
    ICurrentActor currentActor,
    AuditWriter auditWriter,
    TimeProvider timeProvider) : ISchedulingService
{
    public async Task<WeeklyScheduleSummary> GetWeeklyScheduleAsync(
        Guid? employeeId,
        CancellationToken cancellationToken)
    {
        if (employeeId.HasValue)
        {
            await EnsureEmployeeExistsAsync(employeeId.Value, cancellationToken);
        }

        WeeklySchedule? scope = await FindWeeklyScheduleAsync(employeeId, cancellationToken);
        WeeklyScheduleVersion? current = scope is null
            ? null
            : await FindWeeklyScheduleVersionAsync(scope.CurrentVersionId, cancellationToken);
        WeeklyScheduleVersion? effective = current;
        if (employeeId.HasValue
            && (current is null || current.Mode == WeeklyScheduleVersionMode.Inherited))
        {
            WeeklySchedule? tenantScope = await FindWeeklyScheduleAsync(null, cancellationToken);
            effective = tenantScope is null
                ? null
                : await FindWeeklyScheduleVersionAsync(
                    tenantScope.CurrentVersionId,
                    cancellationToken);
        }

        WeeklyScheduleVersion? attribution = current ?? effective;
        string? publishedBy = attribution is null
            ? null
            : await GetActorDisplayNameAsync(attribution.ActorUserId, cancellationToken);
        return ToWeeklySummary(scope, current, effective, employeeId, publishedBy);
    }

    public async Task<WeeklyScheduleSummary> PutWeeklyScheduleAsync(
        Guid? employeeId,
        WeeklyScheduleInput input,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(input);
        if (employeeId.HasValue)
        {
            await EnsureEmployeeExistsAsync(employeeId.Value, cancellationToken);
        }

        SchedulePeriodDefinition[] definitions = input.Periods
            .Select(period => new SchedulePeriodDefinition(
                period.DayOfWeek,
                period.StartMinute,
                period.EndMinute))
            .ToArray();
        WeeklySchedule? schedule = await FindWeeklyScheduleAsync(
            employeeId,
            cancellationToken);
        EnsureExpectedRevision(schedule, input.ExpectedRevision);
        var now = timeProvider.GetUtcNow();
        WeeklyScheduleVersion version;
        try
        {
            WeeklyScheduleVersionMode mode = definitions.Length == 0
                ? WeeklyScheduleVersionMode.Closed
                : WeeklyScheduleVersionMode.Custom;
            if (schedule is null)
            {
                schedule = WeeklySchedule.Create(
                    Guid.NewGuid(),
                    tenantContext.TenantId,
                    employeeId,
                    mode,
                    definitions,
                    currentActor.UserId,
                    currentActor.MembershipId,
                    input.ChangeNote,
                    now);
                dbContext.WeeklySchedules.Add(schedule);
                version = schedule.Versions.Single();
            }
            else
            {
                version = schedule.Publish(
                    Guid.NewGuid(),
                    mode,
                    definitions,
                    currentActor.UserId,
                    currentActor.MembershipId,
                    input.ChangeNote,
                    restoredFromVersionId: null,
                    now);
                dbContext.WeeklyScheduleVersions.Add(version);
            }
        }
        catch (ArgumentException exception)
        {
            throw InvalidSchedule(exception);
        }

        auditWriter.Add(
            "working-hours.version-published",
            "weekly-schedule-version",
            version.Id,
            now,
            $"scheduleId={schedule.Id};version={version.VersionNumber};mode={version.Mode}");
        await SaveScheduleAsync(cancellationToken);
        return await GetWeeklyScheduleAsync(employeeId, cancellationToken);
    }

    public async Task DeleteEmployeeWeeklyScheduleAsync(
        Guid employeeId,
        long expectedRevision,
        string? changeNote,
        CancellationToken cancellationToken)
    {
        await EnsureEmployeeExistsAsync(employeeId, cancellationToken);
        WeeklySchedule? schedule = await FindWeeklyScheduleAsync(employeeId, cancellationToken);
        if (schedule is null)
        {
            EnsureExpectedRevision(null, expectedRevision);
            return;
        }

        EnsureExpectedRevision(schedule, expectedRevision);
        WeeklyScheduleVersion current = await FindWeeklyScheduleVersionAsync(
            schedule.CurrentVersionId,
            cancellationToken)
            ?? throw new InvalidOperationException("The current weekly schedule version is missing.");
        if (current.Mode == WeeklyScheduleVersionMode.Inherited)
        {
            return;
        }

        var now = timeProvider.GetUtcNow();
        WeeklyScheduleVersion version = schedule.Publish(
            Guid.NewGuid(),
            WeeklyScheduleVersionMode.Inherited,
            [],
            currentActor.UserId,
            currentActor.MembershipId,
            changeNote,
            restoredFromVersionId: null,
            now);
        dbContext.WeeklyScheduleVersions.Add(version);
        auditWriter.Add(
            "working-hours.inheritance-restored",
            "weekly-schedule-version",
            version.Id,
            now,
            $"scheduleId={schedule.Id};version={version.VersionNumber}");
        await SaveScheduleAsync(cancellationToken);
    }

    public async Task<PagedResult<WeeklyScheduleVersionSummary>> ListWeeklyScheduleVersionsAsync(
        Guid? employeeId,
        PageRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (employeeId.HasValue)
        {
            await EnsureEmployeeExistsAsync(employeeId.Value, cancellationToken);
        }

        WeeklySchedule? schedule = await dbContext.WeeklySchedules
            .AsNoTracking()
            .SingleOrDefaultAsync(candidate => candidate.EmployeeId == employeeId, cancellationToken);
        if (schedule is null)
        {
            return new PagedResult<WeeklyScheduleVersionSummary>(
                [],
                request.Page,
                request.PageSize,
                0);
        }

        IQueryable<WeeklyScheduleVersion> query = dbContext.WeeklyScheduleVersions
            .AsNoTracking()
            .Where(version => version.ScheduleId == schedule.Id);
        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            string pattern = $"%{request.Search}%";
            query = query.Where(version =>
                (version.ChangeNote != null && EF.Functions.ILike(version.ChangeNote, pattern))
                || dbContext.Users.Any(user => user.Id == version.ActorUserId
                    && EF.Functions.ILike(user.DisplayName, pattern)));
        }

        int totalCount = await query.CountAsync(cancellationToken);
        query = OrderVersions(query, request);
        List<WeeklyScheduleVersion> versions = await query
            .Include(version => version.Periods)
            .Skip(request.Skip)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);
        IReadOnlyDictionary<Guid, string> actorNames = await GetActorDisplayNamesAsync(
            versions,
            cancellationToken);
        IReadOnlyDictionary<Guid, long> restoredVersionNumbers =
            await GetRestoredVersionNumbersAsync(versions, cancellationToken);
        return new PagedResult<WeeklyScheduleVersionSummary>(
            versions.Select(version => ToVersionSummary(
                    version,
                    version.ActorUserId is Guid actorUserId
                        ? actorNames.GetValueOrDefault(actorUserId)
                        : null,
                    version.RestoredFromVersionId is Guid restoredFromVersionId
                        ? restoredVersionNumbers.GetValueOrDefault(restoredFromVersionId)
                        : null))
                .ToList(),
            request.Page,
            request.PageSize,
            totalCount);
    }

    public async Task<WeeklyScheduleVersionSummary> GetWeeklyScheduleVersionAsync(
        Guid? employeeId,
        Guid versionId,
        CancellationToken cancellationToken)
    {
        if (employeeId.HasValue)
        {
            await EnsureEmployeeExistsAsync(employeeId.Value, cancellationToken);
        }

        WeeklySchedule? schedule = await dbContext.WeeklySchedules
            .AsNoTracking()
            .SingleOrDefaultAsync(candidate => candidate.EmployeeId == employeeId, cancellationToken);
        WeeklyScheduleVersion version = schedule is null
            ? throw ScheduleVersionNotFound()
            : await dbContext.WeeklyScheduleVersions
                .AsNoTracking()
                .Include(candidate => candidate.Periods)
                .SingleOrDefaultAsync(
                    candidate => candidate.ScheduleId == schedule.Id && candidate.Id == versionId,
                    cancellationToken)
                ?? throw ScheduleVersionNotFound();
        string? actor = await GetActorDisplayNameAsync(version.ActorUserId, cancellationToken);
        long? restoredFromVersionNumber = version.RestoredFromVersionId is Guid restoredFromVersionId
            ? await dbContext.WeeklyScheduleVersions
                .AsNoTracking()
                .Where(candidate => candidate.Id == restoredFromVersionId)
                .Select(candidate => (long?)candidate.VersionNumber)
                .SingleOrDefaultAsync(cancellationToken)
            : null;
        return ToVersionSummary(version, actor, restoredFromVersionNumber);
    }

    public async Task<WeeklyScheduleSummary> RestoreWeeklyScheduleVersionAsync(
        Guid? employeeId,
        Guid versionId,
        RestoreWeeklyScheduleVersionInput input,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(input);
        if (employeeId.HasValue)
        {
            await EnsureEmployeeExistsAsync(employeeId.Value, cancellationToken);
        }

        WeeklySchedule? schedule = await FindWeeklyScheduleAsync(employeeId, cancellationToken);
        if (schedule is null)
        {
            throw ScheduleVersionNotFound();
        }

        EnsureExpectedRevision(schedule, input.ExpectedRevision);
        WeeklyScheduleVersion source = await dbContext.WeeklyScheduleVersions
            .AsNoTracking()
            .Include(version => version.Periods)
            .SingleOrDefaultAsync(
                version => version.ScheduleId == schedule.Id && version.Id == versionId,
                cancellationToken)
            ?? throw ScheduleVersionNotFound();
        SchedulePeriodDefinition[] definitions = source.Periods
            .Select(period => new SchedulePeriodDefinition(
                period.DayOfWeek,
                period.StartMinute,
                period.EndMinute))
            .ToArray();
        var now = timeProvider.GetUtcNow();
        WeeklyScheduleVersion restored;
        try
        {
            restored = schedule.Publish(
                Guid.NewGuid(),
                source.Mode,
                definitions,
                currentActor.UserId,
                currentActor.MembershipId,
                input.ChangeNote,
                source.Id,
                now);
            dbContext.WeeklyScheduleVersions.Add(restored);
        }
        catch (ArgumentException exception)
        {
            throw InvalidSchedule(exception);
        }

        auditWriter.Add(
            "working-hours.version-restored",
            "weekly-schedule-version",
            restored.Id,
            now,
            $"scheduleId={schedule.Id};version={restored.VersionNumber};restoredFrom={source.Id}");
        await SaveScheduleAsync(cancellationToken);
        return await GetWeeklyScheduleAsync(employeeId, cancellationToken);
    }

    public async Task<IReadOnlyList<DateOverrideSummary>> ListDateOverridesAsync(
        Guid? employeeId,
        DateOnly fromDate,
        DateOnly toDate,
        CancellationToken cancellationToken)
    {
        ValidateDateRange(fromDate, toDate);
        if (employeeId.HasValue)
        {
            await EnsureEmployeeExistsAsync(employeeId.Value, cancellationToken);
        }

        List<DateScheduleOverride> scheduleOverrides = await dbContext.DateScheduleOverrides
            .AsNoTracking()
            .Include(scheduleOverride => scheduleOverride.Periods)
            .Where(scheduleOverride => scheduleOverride.EmployeeId == employeeId
                && scheduleOverride.Date >= fromDate
                && scheduleOverride.Date <= toDate)
            .OrderBy(scheduleOverride => scheduleOverride.Date)
            .ToListAsync(cancellationToken);
        return scheduleOverrides.Select(ToDateOverrideSummary).ToList();
    }

    public async Task<DateOverrideSummary> PutDateOverrideAsync(
        Guid? employeeId,
        DateOnly date,
        DateOverrideInput input,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(input);
        if (employeeId.HasValue)
        {
            await EnsureEmployeeExistsAsync(employeeId.Value, cancellationToken);
        }

        SchedulePeriodDefinition[] definitions = input.Periods
            .Select(period => new SchedulePeriodDefinition(
                0,
                period.StartMinute,
                period.EndMinute))
            .ToArray();
        DateScheduleOverride? scheduleOverride = await dbContext.DateScheduleOverrides
            .Include(candidate => candidate.Periods)
            .SingleOrDefaultAsync(
                candidate => candidate.EmployeeId == employeeId && candidate.Date == date,
                cancellationToken);
        var now = timeProvider.GetUtcNow();
        try
        {
            if (scheduleOverride is null)
            {
                scheduleOverride = DateScheduleOverride.Create(
                    Guid.NewGuid(),
                    tenantContext.TenantId,
                    employeeId,
                    date,
                    input.IsClosed,
                    definitions,
                    now);
                dbContext.DateScheduleOverrides.Add(scheduleOverride);
            }
            else
            {
                scheduleOverride.Replace(input.IsClosed, definitions, now);
                dbContext.DateScheduleOverridePeriods.AddRange(scheduleOverride.Periods);
            }
        }
        catch (ArgumentException exception)
        {
            throw InvalidSchedule(exception);
        }

        auditWriter.Add(
            "date-override.updated",
            employeeId.HasValue ? "employee" : "tenant",
            employeeId ?? tenantContext.TenantId,
            now,
            $"date={date:yyyy-MM-dd};closed={input.IsClosed.ToString().ToLowerInvariant()}");
        await SaveScheduleAsync(cancellationToken);
        return ToDateOverrideSummary(scheduleOverride);
    }

    public async Task DeleteDateOverrideAsync(
        Guid? employeeId,
        DateOnly date,
        CancellationToken cancellationToken)
    {
        if (employeeId.HasValue)
        {
            await EnsureEmployeeExistsAsync(employeeId.Value, cancellationToken);
        }

        DateScheduleOverride? scheduleOverride = await dbContext.DateScheduleOverrides
            .SingleOrDefaultAsync(
                candidate => candidate.EmployeeId == employeeId && candidate.Date == date,
                cancellationToken);
        if (scheduleOverride is null)
        {
            return;
        }

        var now = timeProvider.GetUtcNow();
        dbContext.DateScheduleOverrides.Remove(scheduleOverride);
        auditWriter.Add(
            "date-override.deleted",
            employeeId.HasValue ? "employee" : "tenant",
            employeeId ?? tenantContext.TenantId,
            now,
            $"date={date:yyyy-MM-dd}");
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<TimeOffSummary>> ListTimeOffAsync(
        Guid? employeeId,
        DateOnly fromDate,
        DateOnly toDate,
        CancellationToken cancellationToken)
    {
        ValidateDateRange(fromDate, toDate);
        if (employeeId.HasValue)
        {
            await EnsureEmployeeExistsAsync(employeeId.Value, cancellationToken);
        }

        TimeZoneInfo timeZone = await GetTenantTimeZoneAsync(cancellationToken);
        (DateTimeOffset searchStart, DateTimeOffset searchEnd) = BroadUtcRange(fromDate, toDate);
        IQueryable<EmployeeTimeOff> query = dbContext.EmployeeTimeOffs.AsNoTracking();
        if (employeeId.HasValue)
        {
            query = query.Where(timeOff => timeOff.EmployeeId == employeeId.Value);
        }

        var rows = await query
            .Where(timeOff => timeOff.StartUtc < searchEnd && timeOff.EndUtc > searchStart)
            .Join(
                dbContext.Employees.AsNoTracking(),
                timeOff => timeOff.EmployeeId,
                employee => employee.Id,
                (timeOff, employee) => new { TimeOff = timeOff, employee.Name })
            .OrderBy(row => row.TimeOff.StartUtc)
            .ToListAsync(cancellationToken);

        return rows
            .Select(row => ToTimeOffSummary(row.TimeOff, row.Name, timeZone))
            .Where(summary => summary.LocalStartDate <= toDate
                && summary.LocalEndDate >= fromDate)
            .ToList();
    }

    public async Task<TimeOffSummary> CreateTimeOffAsync(
        LocalTimeOffInput input,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(input);
        Employee employee = await GetEmployeeAsync(input.EmployeeId, cancellationToken);
        (string tenantTimeZoneId, TimeZoneInfo timeZone) = await GetTenantTimeZoneDetailsAsync(
            cancellationToken);
        if (!string.Equals(input.TimeZone, tenantTimeZoneId, StringComparison.Ordinal))
        {
            throw new ApplicationValidationException(
                SchedulingErrorCodes.TimeZoneMismatch,
                new Dictionary<string, string[]>
                {
                    ["timeZone"] = [$"TimeZone must match the tenant time zone '{tenantTimeZoneId}'."],
                });
        }

        DateTimeOffset startUtc = ToUtcBoundary(
            input.StartDate,
            input.StartTime,
            timeZone,
            "startTime");
        DateTimeOffset endUtc = ToUtcBoundary(
            input.EndDate,
            input.EndTime,
            timeZone,
            "endTime");
        if (startUtc >= endUtc)
        {
            throw new ApplicationValidationException(
                SchedulingErrorCodes.InvalidDateRange,
                new Dictionary<string, string[]>
                {
                    ["endTime"] = ["Time off must end after it starts."],
                });
        }

        bool overlaps = await dbContext.EmployeeTimeOffs.AnyAsync(
            timeOff => timeOff.EmployeeId == input.EmployeeId
                && startUtc < timeOff.EndUtc
                && endUtc > timeOff.StartUtc,
            cancellationToken);
        if (overlaps)
        {
            throw TimeOffOverlap();
        }

        var now = timeProvider.GetUtcNow();
        EmployeeTimeOff timeOff;
        try
        {
            timeOff = EmployeeTimeOff.Create(
                Guid.NewGuid(),
                tenantContext.TenantId,
                input.EmployeeId,
                startUtc,
                endUtc,
                input.Reason,
                now);
        }
        catch (ArgumentException exception)
        {
            throw ApplicationValidationException.FromArgument(exception);
        }

        dbContext.EmployeeTimeOffs.Add(timeOff);
        auditWriter.Add(
            "time-off.created",
            "employee-time-off",
            timeOff.Id,
            now,
            $"employeeId={input.EmployeeId};startUtc={startUtc:O};endUtc={endUtc:O}");
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (
            exception.InnerException is PostgresException
            {
                SqlState: PostgresErrorCodes.ExclusionViolation,
                ConstraintName: "ex_employee_time_offs_no_overlap",
            })
        {
            throw TimeOffOverlap(exception);
        }

        return ToTimeOffSummary(timeOff, employee.Name, timeZone);
    }

    public async Task DeleteTimeOffAsync(
        Guid timeOffId,
        CancellationToken cancellationToken)
    {
        EmployeeTimeOff? timeOff = await dbContext.EmployeeTimeOffs.SingleOrDefaultAsync(
            candidate => candidate.Id == timeOffId,
            cancellationToken);
        if (timeOff is null)
        {
            throw new ApplicationNotFoundException(
                SchedulingErrorCodes.TimeOffNotFound,
                "The requested time-off entry was not found.");
        }

        var now = timeProvider.GetUtcNow();
        dbContext.EmployeeTimeOffs.Remove(timeOff);
        auditWriter.Add("time-off.deleted", "employee-time-off", timeOff.Id, now);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<AvailabilityDay> GetAvailabilityAsync(
        AvailabilityQuery query,
        CancellationToken cancellationToken)
    {
        Employee employee = await dbContext.Employees
            .AsNoTracking()
            .SingleOrDefaultAsync(candidate => candidate.Id == query.EmployeeId, cancellationToken)
            ?? throw new ApplicationNotFoundException(
                SchedulingErrorCodes.EmployeeNotFound,
                "The requested employee was not found.");
        ServiceOffering service = await dbContext.Services
            .AsNoTracking()
            .SingleOrDefaultAsync(candidate => candidate.Id == query.ServiceId, cancellationToken)
            ?? throw new ApplicationNotFoundException(
                SchedulingErrorCodes.ServiceNotFound,
                "The requested service was not found.");
        if (!employee.IsActive)
        {
            throw new ApplicationConflictException(
                SchedulingErrorCodes.EmployeeInactive,
                "Availability cannot be calculated for an inactive employee.");
        }

        if (!service.IsActive)
        {
            throw new ApplicationConflictException(
                SchedulingErrorCodes.ServiceInactive,
                "Availability cannot be calculated for an inactive service.");
        }

        bool assigned = await dbContext.EmployeeServices.AsNoTracking().AnyAsync(
            assignment => assignment.EmployeeId == query.EmployeeId
                && assignment.ServiceId == query.ServiceId,
            cancellationToken);
        if (!assigned)
        {
            throw new ApplicationConflictException(
                SchedulingErrorCodes.ServiceNotAssigned,
                "The employee is not assigned to the requested service.");
        }

        (string timeZoneId, TimeZoneInfo timeZone) = await GetTenantTimeZoneDetailsAsync(
            cancellationToken);
        IReadOnlyList<AvailabilityPeriod> periods = await GetEffectivePeriodsAsync(
            query.Date,
            query.EmployeeId,
            cancellationToken);
        (DateTimeOffset searchStart, DateTimeOffset searchEnd) = BroadUtcRange(
            query.Date,
            query.Date);
        List<UnavailableInterval> timeOffs = await dbContext.EmployeeTimeOffs
            .AsNoTracking()
            .Where(timeOff => timeOff.EmployeeId == query.EmployeeId
                && timeOff.StartUtc < searchEnd
                && timeOff.EndUtc > searchStart)
            .Select(timeOff => new UnavailableInterval(timeOff.StartUtc, timeOff.EndUtc))
            .ToListAsync(cancellationToken);
        IReadOnlyList<AvailabilitySlot> slots = AvailabilityCalculator.Calculate(
            query.Date,
            timeZone,
            service.DurationMinutes,
            periods,
            timeOffs);

        return new AvailabilityDay(
            query.Date,
            query.EmployeeId,
            query.ServiceId,
            service.DurationMinutes,
            timeZoneId,
            slots);
    }

    private async Task<IReadOnlyList<AvailabilityPeriod>> GetEffectivePeriodsAsync(
        DateOnly date,
        Guid employeeId,
        CancellationToken cancellationToken)
    {
        DateScheduleOverride? scheduleOverride = await dbContext.DateScheduleOverrides
            .AsNoTracking()
            .Include(candidate => candidate.Periods)
            .Where(candidate => candidate.Date == date
                && (candidate.EmployeeId == employeeId || candidate.EmployeeId == null))
            .OrderByDescending(candidate => candidate.EmployeeId != null)
            .FirstOrDefaultAsync(cancellationToken);
        if (scheduleOverride is not null)
        {
            return scheduleOverride.IsClosed
                ? []
                : scheduleOverride.Periods
                    .OrderBy(period => period.StartMinute)
                    .Select(period => new AvailabilityPeriod(
                        period.StartMinute,
                        period.EndMinute))
                    .ToList();
        }

        WeeklySchedule? employeeSchedule = await dbContext.WeeklySchedules
            .AsNoTracking()
            .SingleOrDefaultAsync(
                candidate => candidate.EmployeeId == employeeId,
                cancellationToken);
        WeeklyScheduleVersion? version = employeeSchedule is null
            ? null
            : await FindWeeklyScheduleVersionAsync(
                employeeSchedule.CurrentVersionId,
                cancellationToken);
        if (version is null || version.Mode == WeeklyScheduleVersionMode.Inherited)
        {
            WeeklySchedule? tenantSchedule = await dbContext.WeeklySchedules
                .AsNoTracking()
                .SingleOrDefaultAsync(
                    candidate => candidate.EmployeeId == null,
                    cancellationToken);
            version = tenantSchedule is null
                ? null
                : await FindWeeklyScheduleVersionAsync(
                    tenantSchedule.CurrentVersionId,
                    cancellationToken);
        }

        if (version is null || version.Mode != WeeklyScheduleVersionMode.Custom)
        {
            return [];
        }

        int dayOfWeek = ((int)date.DayOfWeek + 6) % 7 + 1;
        return version.Periods
            .Where(period => period.DayOfWeek == dayOfWeek)
            .OrderBy(period => period.StartMinute)
            .Select(period => new AvailabilityPeriod(period.StartMinute, period.EndMinute))
            .ToList();
    }

    private Task<WeeklySchedule?> FindWeeklyScheduleAsync(
        Guid? employeeId,
        CancellationToken cancellationToken) =>
        dbContext.WeeklySchedules
            .SingleOrDefaultAsync(
                schedule => schedule.EmployeeId == employeeId,
                cancellationToken);

    private Task<WeeklyScheduleVersion?> FindWeeklyScheduleVersionAsync(
        Guid versionId,
        CancellationToken cancellationToken) =>
        dbContext.WeeklyScheduleVersions
            .AsNoTracking()
            .Include(version => version.Periods)
            .SingleOrDefaultAsync(version => version.Id == versionId, cancellationToken);

    private async Task EnsureEmployeeExistsAsync(
        Guid employeeId,
        CancellationToken cancellationToken) =>
        _ = await GetEmployeeAsync(employeeId, cancellationToken);

    private async Task<Employee> GetEmployeeAsync(
        Guid employeeId,
        CancellationToken cancellationToken) =>
        await dbContext.Employees
            .AsNoTracking()
            .SingleOrDefaultAsync(employee => employee.Id == employeeId, cancellationToken)
        ?? throw new ApplicationNotFoundException(
            SchedulingErrorCodes.EmployeeNotFound,
            "The requested employee was not found.");

    private async Task<TimeZoneInfo> GetTenantTimeZoneAsync(
        CancellationToken cancellationToken) =>
        (await GetTenantTimeZoneDetailsAsync(cancellationToken)).TimeZone;

    private async Task<(string Id, TimeZoneInfo TimeZone)> GetTenantTimeZoneDetailsAsync(
        CancellationToken cancellationToken)
    {
        string timeZoneId = await dbContext.Tenants
            .AsNoTracking()
            .Select(tenant => tenant.TimeZone)
            .SingleAsync(cancellationToken);
        try
        {
            return (timeZoneId, TimeZoneInfo.FindSystemTimeZoneById(timeZoneId));
        }
        catch (TimeZoneNotFoundException exception)
        {
            throw new InvalidOperationException(
                $"Tenant time zone '{timeZoneId}' is not installed on the server.",
                exception);
        }
    }

    private static DateTimeOffset ToUtcBoundary(
        DateOnly date,
        TimeOnly time,
        TimeZoneInfo timeZone,
        string field)
    {
        DateTime local = DateTime.SpecifyKind(date.ToDateTime(time), DateTimeKind.Unspecified);
        if (timeZone.IsInvalidTime(local))
        {
            throw new ApplicationValidationException(
                SchedulingErrorCodes.InvalidLocalTime,
                new Dictionary<string, string[]>
                {
                    [field] = ["This local time does not exist because of a daylight-saving transition."],
                });
        }

        if (timeZone.IsAmbiguousTime(local))
        {
            throw new ApplicationValidationException(
                SchedulingErrorCodes.AmbiguousLocalTime,
                new Dictionary<string, string[]>
                {
                    [field] = ["This local time occurs twice because of a daylight-saving transition."],
                });
        }

        return new DateTimeOffset(TimeZoneInfo.ConvertTimeToUtc(local, timeZone), TimeSpan.Zero);
    }

    private static (DateTimeOffset Start, DateTimeOffset End) BroadUtcRange(
        DateOnly fromDate,
        DateOnly toDate)
    {
        DateTime fromUtc = DateTime.SpecifyKind(
            fromDate.ToDateTime(TimeOnly.MinValue),
            DateTimeKind.Utc);
        DateTime toUtc = DateTime.SpecifyKind(
            toDate.AddDays(1).ToDateTime(TimeOnly.MinValue),
            DateTimeKind.Utc);
        return (
            new DateTimeOffset(fromUtc).AddHours(-18),
            new DateTimeOffset(toUtc).AddHours(18));
    }

    private static WeeklyScheduleSummary ToWeeklySummary(
        WeeklySchedule? scope,
        WeeklyScheduleVersion? current,
        WeeklyScheduleVersion? effective,
        Guid? requestedEmployeeId,
        string? publishedBy)
    {
        bool inherited = requestedEmployeeId.HasValue
            && (current is null || current.Mode == WeeklyScheduleVersionMode.Inherited);
        string state = requestedEmployeeId.HasValue && inherited
            ? "inherited"
            : current is null
                ? "unconfigured"
                : ModeValue(current.Mode);
        string source = effective is null
            ? "none"
            : inherited
                ? "tenant"
                : requestedEmployeeId.HasValue
                    ? "employee"
                    : "tenant";
        WeeklyScheduleVersion? attribution = current ?? effective;
        return new WeeklyScheduleSummary(
            requestedEmployeeId,
            state,
            source,
            scope?.Revision ?? 0,
            current?.Id,
            current?.VersionNumber,
            effective?.Id,
            effective?.VersionNumber,
            effective?.Periods
                .OrderBy(period => period.DayOfWeek)
                .ThenBy(period => period.StartMinute)
                .Select(period => new SchedulePeriodSummary(
                    period.DayOfWeek,
                    period.StartMinute,
                    period.EndMinute))
                .ToList() ?? [],
            attribution?.CreatedAtUtc,
            publishedBy,
            current?.ChangeNote);
    }

    private static WeeklyScheduleVersionSummary ToVersionSummary(
        WeeklyScheduleVersion version,
        string? publishedBy,
        long? restoredFromVersionNumber) =>
        new(
            version.Id,
            version.VersionNumber,
            ModeValue(version.Mode),
            version.Periods
                .OrderBy(period => period.DayOfWeek)
                .ThenBy(period => period.StartMinute)
                .Select(period => new SchedulePeriodSummary(
                    period.DayOfWeek,
                    period.StartMinute,
                    period.EndMinute))
                .ToList(),
            version.CreatedAtUtc,
            publishedBy,
            version.ChangeNote,
            version.RestoredFromVersionId,
            restoredFromVersionNumber);

    private static string ModeValue(WeeklyScheduleVersionMode mode) =>
        mode.ToString().ToLowerInvariant();

    private static DateOverrideSummary ToDateOverrideSummary(
        DateScheduleOverride scheduleOverride) =>
        new(
            scheduleOverride.Id,
            scheduleOverride.EmployeeId,
            scheduleOverride.Date,
            scheduleOverride.IsClosed,
            scheduleOverride.Periods
                .OrderBy(period => period.StartMinute)
                .Select(period => new SchedulePeriodSummary(
                    0,
                    period.StartMinute,
                    period.EndMinute))
                .ToList(),
            scheduleOverride.UpdatedAtUtc);

    private static TimeOffSummary ToTimeOffSummary(
        EmployeeTimeOff timeOff,
        string employeeName,
        TimeZoneInfo timeZone)
    {
        DateTimeOffset localStart = TimeZoneInfo.ConvertTime(timeOff.StartUtc, timeZone);
        DateTimeOffset localEnd = TimeZoneInfo.ConvertTime(timeOff.EndUtc, timeZone);
        return new TimeOffSummary(
            timeOff.Id,
            timeOff.EmployeeId,
            employeeName,
            timeOff.StartUtc,
            timeOff.EndUtc,
            DateOnly.FromDateTime(localStart.DateTime),
            TimeOnly.FromDateTime(localStart.DateTime),
            DateOnly.FromDateTime(localEnd.DateTime),
            TimeOnly.FromDateTime(localEnd.DateTime),
            timeZone.Id,
            timeOff.Reason);
    }

    private static void ValidateDateRange(DateOnly fromDate, DateOnly toDate)
    {
        if (fromDate > toDate || toDate.DayNumber - fromDate.DayNumber > 366)
        {
            throw new ApplicationValidationException(
                SchedulingErrorCodes.InvalidDateRange,
                new Dictionary<string, string[]>
                {
                    ["toDate"] = ["Date range must be ordered and cannot exceed 366 days."],
                });
        }
    }

    private static ApplicationValidationException InvalidSchedule(
        ArgumentException exception) =>
        new(
            SchedulingErrorCodes.InvalidSchedule,
            new Dictionary<string, string[]>
            {
                ["periods"] = [exception.Message],
            },
            innerException: exception);

    private static ApplicationConflictException TimeOffOverlap(Exception? exception = null) =>
        new(
            SchedulingErrorCodes.TimeOffOverlap,
            "The time-off interval overlaps an existing entry for this employee.",
            exception);

    private static void EnsureExpectedRevision(
        WeeklySchedule? schedule,
        long expectedRevision)
    {
        long actualRevision = schedule?.Revision ?? 0;
        if (actualRevision != expectedRevision)
        {
            throw ScheduleVersionConflict();
        }
    }

    private static ApplicationConflictException ScheduleVersionConflict(
        Exception? exception = null) =>
        new(
            SchedulingErrorCodes.ScheduleVersionConflict,
            "The weekly schedule was published by another request. Reload it and try again.",
            exception);

    private static ApplicationNotFoundException ScheduleVersionNotFound() =>
        new(
            SchedulingErrorCodes.ScheduleVersionNotFound,
            "The requested weekly schedule version was not found.");

    private static IQueryable<WeeklyScheduleVersion> OrderVersions(
        IQueryable<WeeklyScheduleVersion> query,
        PageRequest request) =>
        (request.SortBy, request.Descending) switch
        {
            ("createdAt", false) => query
                .OrderBy(version => version.CreatedAtUtc)
                .ThenBy(version => version.VersionNumber),
            ("createdAt", true) => query
                .OrderByDescending(version => version.CreatedAtUtc)
                .ThenByDescending(version => version.VersionNumber),
            ("version", false) => query.OrderBy(version => version.VersionNumber),
            _ => query.OrderByDescending(version => version.VersionNumber),
        };

    private async Task<string?> GetActorDisplayNameAsync(
        Guid? actorUserId,
        CancellationToken cancellationToken)
    {
        if (!actorUserId.HasValue)
        {
            return null;
        }

        return await dbContext.Users
            .AsNoTracking()
            .Where(user => user.Id == actorUserId.Value)
            .Select(user => user.DisplayName)
            .SingleAsync(cancellationToken);
    }

    private async Task<IReadOnlyDictionary<Guid, string>> GetActorDisplayNamesAsync(
        IReadOnlyCollection<WeeklyScheduleVersion> versions,
        CancellationToken cancellationToken)
    {
        Guid[] actorIds = versions
            .Where(version => version.ActorUserId.HasValue)
            .Select(version => version.ActorUserId!.Value)
            .Distinct()
            .ToArray();
        return await dbContext.Users
            .AsNoTracking()
            .Where(user => actorIds.Contains(user.Id))
            .ToDictionaryAsync(user => user.Id, user => user.DisplayName, cancellationToken);
    }

    private async Task<IReadOnlyDictionary<Guid, long>> GetRestoredVersionNumbersAsync(
        IReadOnlyCollection<WeeklyScheduleVersion> versions,
        CancellationToken cancellationToken)
    {
        Guid[] sourceIds = versions
            .Where(version => version.RestoredFromVersionId.HasValue)
            .Select(version => version.RestoredFromVersionId!.Value)
            .Distinct()
            .ToArray();
        return await dbContext.WeeklyScheduleVersions
            .AsNoTracking()
            .Where(version => sourceIds.Contains(version.Id))
            .ToDictionaryAsync(
                version => version.Id,
                version => version.VersionNumber,
                cancellationToken);
    }

    private async Task SaveScheduleAsync(CancellationToken cancellationToken)
    {
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (DatabaseConflict.IsUniqueConstraint(
            exception,
            "ux_date_overrides_tenant_date",
            "ux_date_overrides_employee_date"))
        {
            throw new ApplicationConflictException(
                SchedulingErrorCodes.ScheduleConflict,
                "The schedule was changed by another request. Reload it and try again.",
                exception);
        }
        catch (DbUpdateException exception) when (DatabaseConflict.IsUniqueConstraint(
            exception,
            "ux_weekly_schedules_tenant_default",
            "ux_weekly_schedules_tenant_employee",
            "ux_weekly_schedule_versions_schedule_number"))
        {
            throw ScheduleVersionConflict(exception);
        }
    }
}
