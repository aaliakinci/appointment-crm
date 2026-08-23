using AppointmentCrm.Application.Reporting;
using AppointmentCrm.Contracts;
using AppointmentCrm.Domain.Appointments;

namespace AppointmentCrm.Api.Reporting;

internal static class ReportingContractMappings
{
    internal static ReportingDashboardResponse ToResponse(this ReportingDashboard dashboard) =>
        new(
            dashboard.FromDate,
            dashboard.ToDate,
            dashboard.Today,
            dashboard.TimeZone,
            dashboard.Currency,
            dashboard.Range.ToResponse(),
            dashboard.TodaySummary.ToResponse(),
            dashboard.ByStatus.Select(item => new ReportingStatusBreakdownResponse(
                    StatusValue(item.Status),
                    item.Count,
                    item.CompletedRevenue))
                .ToList(),
            dashboard.ByEmployee.Select(item => new ReportingEmployeeBreakdownResponse(
                    item.EmployeeId,
                    item.EmployeeName,
                    item.TotalAppointments,
                    item.CompletedAppointments,
                    item.NoShowAppointments,
                    item.CompletedRevenue))
                .ToList(),
            dashboard.ByDay.Select(item => new ReportingDailyBreakdownResponse(
                    item.Date,
                    item.TotalAppointments,
                    item.CompletedAppointments,
                    item.CompletedRevenue))
                .ToList());

    private static ReportingHeadlineResponse ToResponse(this ReportingHeadline headline) =>
        new(
            headline.TotalAppointments,
            headline.ScheduledAppointments,
            headline.ConfirmedAppointments,
            headline.CompletedAppointments,
            headline.CancelledAppointments,
            headline.NoShowAppointments,
            headline.CompletedRevenue);

    private static string StatusValue(AppointmentStatus status) =>
        status == AppointmentStatus.NoShow
            ? "no-show"
            : status.ToString().ToLowerInvariant();
}
