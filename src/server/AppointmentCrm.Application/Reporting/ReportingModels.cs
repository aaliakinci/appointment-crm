using AppointmentCrm.Domain.Appointments;

namespace AppointmentCrm.Application.Reporting;

public sealed record ReportingFilter(
    DateOnly FromDate,
    DateOnly ToDate,
    Guid? EmployeeId,
    AppointmentStatus? Status);

public sealed record ReportingHeadline(
    int TotalAppointments,
    int ScheduledAppointments,
    int ConfirmedAppointments,
    int CompletedAppointments,
    int CancelledAppointments,
    int NoShowAppointments,
    decimal CompletedRevenue);

public sealed record ReportingStatusBreakdown(
    AppointmentStatus Status,
    int Count,
    decimal CompletedRevenue);

public sealed record ReportingEmployeeBreakdown(
    Guid EmployeeId,
    string EmployeeName,
    int TotalAppointments,
    int CompletedAppointments,
    int NoShowAppointments,
    decimal CompletedRevenue);

public sealed record ReportingDailyBreakdown(
    DateOnly Date,
    int TotalAppointments,
    int CompletedAppointments,
    decimal CompletedRevenue);

public sealed record ReportingDashboard(
    DateOnly FromDate,
    DateOnly ToDate,
    DateOnly Today,
    string TimeZone,
    string Currency,
    ReportingHeadline Range,
    ReportingHeadline TodaySummary,
    IReadOnlyList<ReportingStatusBreakdown> ByStatus,
    IReadOnlyList<ReportingEmployeeBreakdown> ByEmployee,
    IReadOnlyList<ReportingDailyBreakdown> ByDay);

public interface IReportingService
{
    Task<ReportingDashboard> GetDashboardAsync(
        ReportingFilter filter,
        CancellationToken cancellationToken);
}
