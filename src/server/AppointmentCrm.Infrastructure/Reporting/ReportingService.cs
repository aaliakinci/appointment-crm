using AppointmentCrm.Application.Reporting;
using AppointmentCrm.Domain.Appointments;
using AppointmentCrm.Infrastructure.Common;
using AppointmentCrm.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AppointmentCrm.Infrastructure.Reporting;

internal sealed class ReportingService(
    AppointmentCrmDbContext dbContext,
    TimeProvider timeProvider) : IReportingService
{
    public async Task<ReportingDashboard> GetDashboardAsync(
        ReportingFilter filter,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(filter);
        TenantReportSettings tenant = await dbContext.Tenants
            .AsNoTracking()
            .Select(item => new TenantReportSettings(item.TimeZone, item.Currency))
            .SingleAsync(cancellationToken);
        TimeZoneInfo timeZone = TimeZoneInfo.FindSystemTimeZoneById(tenant.TimeZone);
        DateOnly today = DateOnly.FromDateTime(
            TimeZoneInfo.ConvertTime(timeProvider.GetUtcNow(), timeZone).DateTime);
        DateOnly queryFrom = filter.FromDate < today ? filter.FromDate : today;
        DateOnly queryTo = filter.ToDate > today ? filter.ToDate : today;
        (DateTimeOffset startUtc, DateTimeOffset endUtc) = TenantLocalDateRange.Resolve(
            queryFrom,
            queryTo,
            timeZone);

        IQueryable<Appointment> query = dbContext.Appointments
            .AsNoTracking()
            .Where(item => item.StartsAtUtc >= startUtc && item.StartsAtUtc < endUtc);
        if (filter.EmployeeId.HasValue)
        {
            query = query.Where(item => item.EmployeeId == filter.EmployeeId.Value);
        }

        if (filter.Status.HasValue)
        {
            query = query.Where(item => item.Status == filter.Status.Value);
        }

        List<AppointmentReportRow> rows = await query
            .Select(item => new AppointmentReportRow(
                item.EmployeeId,
                item.Employee.Name,
                item.Status,
                item.StartsAtUtc,
                item.ServicePrice))
            .ToListAsync(cancellationToken);
        IReadOnlyList<LocalAppointmentReportRow> localRows = rows
            .Select(item => new LocalAppointmentReportRow(
                item.EmployeeId,
                item.EmployeeName,
                item.Status,
                DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(item.StartsAtUtc, timeZone).DateTime),
                item.ServicePrice))
            .ToList();
        LocalAppointmentReportRow[] rangeRows = localRows
            .Where(item => item.Date >= filter.FromDate && item.Date <= filter.ToDate)
            .ToArray();
        LocalAppointmentReportRow[] todayRows = localRows
            .Where(item => item.Date == today)
            .ToArray();

        return new ReportingDashboard(
            filter.FromDate,
            filter.ToDate,
            today,
            tenant.TimeZone,
            tenant.Currency,
            ToHeadline(rangeRows),
            ToHeadline(todayRows),
            Enum.GetValues<AppointmentStatus>()
                .Select(status =>
                {
                    LocalAppointmentReportRow[] statusRows = rangeRows
                        .Where(item => item.Status == status)
                        .ToArray();
                    return new ReportingStatusBreakdown(
                        status,
                        statusRows.Length,
                        CompletedRevenue(statusRows));
                })
                .ToList(),
            rangeRows
                .GroupBy(item => new { item.EmployeeId, item.EmployeeName })
                .Select(group => new ReportingEmployeeBreakdown(
                    group.Key.EmployeeId,
                    group.Key.EmployeeName,
                    group.Count(),
                    group.Count(item => item.Status == AppointmentStatus.Completed),
                    group.Count(item => item.Status == AppointmentStatus.NoShow),
                    CompletedRevenue(group)))
                .OrderByDescending(item => item.TotalAppointments)
                .ThenBy(item => item.EmployeeName)
                .ToList(),
            Enumerable.Range(0, filter.ToDate.DayNumber - filter.FromDate.DayNumber + 1)
                .Select(offset => filter.FromDate.AddDays(offset))
                .Select(date =>
                {
                    LocalAppointmentReportRow[] dayRows = rangeRows
                        .Where(item => item.Date == date)
                        .ToArray();
                    return new ReportingDailyBreakdown(
                        date,
                        dayRows.Length,
                        dayRows.Count(item => item.Status == AppointmentStatus.Completed),
                        CompletedRevenue(dayRows));
                })
                .ToList());
    }

    private static ReportingHeadline ToHeadline(
        IReadOnlyCollection<LocalAppointmentReportRow> rows) =>
        new(
            rows.Count,
            rows.Count(item => item.Status == AppointmentStatus.Scheduled),
            rows.Count(item => item.Status == AppointmentStatus.Confirmed),
            rows.Count(item => item.Status == AppointmentStatus.Completed),
            rows.Count(item => item.Status == AppointmentStatus.Cancelled),
            rows.Count(item => item.Status == AppointmentStatus.NoShow),
            CompletedRevenue(rows));

    private static decimal CompletedRevenue(
        IEnumerable<LocalAppointmentReportRow> rows) =>
        rows.Where(item => item.Status == AppointmentStatus.Completed)
            .Sum(item => item.ServicePrice);

    private sealed record TenantReportSettings(string TimeZone, string Currency);

    private sealed record AppointmentReportRow(
        Guid EmployeeId,
        string EmployeeName,
        AppointmentStatus Status,
        DateTimeOffset StartsAtUtc,
        decimal ServicePrice);

    private sealed record LocalAppointmentReportRow(
        Guid EmployeeId,
        string EmployeeName,
        AppointmentStatus Status,
        DateOnly Date,
        decimal ServicePrice);
}
