namespace AppointmentCrm.Contracts;

public sealed record ReportingHeadlineResponse(
    int TotalAppointments,
    int ScheduledAppointments,
    int ConfirmedAppointments,
    int CompletedAppointments,
    int CancelledAppointments,
    int NoShowAppointments,
    decimal CompletedRevenue);

public sealed record ReportingStatusBreakdownResponse(
    string Status,
    int Count,
    decimal CompletedRevenue);

public sealed record ReportingEmployeeBreakdownResponse(
    Guid EmployeeId,
    string EmployeeName,
    int TotalAppointments,
    int CompletedAppointments,
    int NoShowAppointments,
    decimal CompletedRevenue);

public sealed record ReportingDailyBreakdownResponse(
    DateOnly Date,
    int TotalAppointments,
    int CompletedAppointments,
    decimal CompletedRevenue);

public sealed record ReportingDashboardResponse(
    DateOnly FromDate,
    DateOnly ToDate,
    DateOnly Today,
    string TimeZone,
    string Currency,
    ReportingHeadlineResponse Range,
    ReportingHeadlineResponse TodaySummary,
    IReadOnlyList<ReportingStatusBreakdownResponse> ByStatus,
    IReadOnlyList<ReportingEmployeeBreakdownResponse> ByEmployee,
    IReadOnlyList<ReportingDailyBreakdownResponse> ByDay);
