using AppointmentCrm.Application.Reporting;

namespace AppointmentCrm.Infrastructure.Reporting;

internal static class DashboardCacheKey
{
    public static string Create(
        string prefix,
        Guid tenantId,
        ReportingFilter filter)
    {
        string employee = filter.EmployeeId?.ToString("N") ?? "all";
        string status = filter.Status?.ToString().ToLowerInvariant() ?? "all";
        return $"{prefix}dashboard:v1:tenant:{tenantId:N}:from:{filter.FromDate:yyyyMMdd}:to:{filter.ToDate:yyyyMMdd}:employee:{employee}:status:{status}";
    }
}
