using AppointmentCrm.Domain.Identity;

namespace AppointmentCrm.Application.Identity;

public static class Permissions
{
    public const string TenantRead = "tenant.read";
    public const string TenantSwitch = "tenant.switch";
    public const string MembershipRead = "memberships.read";
    public const string MembershipManage = "memberships.manage";
    public const string SessionManageOwn = "sessions.manage-own";
    public const string CustomerRead = "customers.read";
    public const string CustomerManage = "customers.manage";
    public const string ServiceRead = "services.read";
    public const string ServiceManage = "services.manage";
    public const string EmployeeRead = "employees.read";
    public const string EmployeeManage = "employees.manage";
    public const string SchedulingManage = "scheduling.manage";
    public const string AvailabilityRead = "availability.read";
    public const string AppointmentRead = "appointments.read";
    public const string AppointmentManage = "appointments.manage";
    public const string AppointmentReadOwn = "appointments.read-own";
    public const string AppointmentTransitionOwn = "appointments.transition-own";
    public const string ReportingRead = "reporting.read";

    public static IReadOnlyList<string> All { get; } =
    [
        TenantRead,
        TenantSwitch,
        MembershipRead,
        MembershipManage,
        SessionManageOwn,
        CustomerRead,
        CustomerManage,
        ServiceRead,
        ServiceManage,
        EmployeeRead,
        EmployeeManage,
        SchedulingManage,
        AvailabilityRead,
        AppointmentRead,
        AppointmentManage,
        AppointmentReadOwn,
        AppointmentTransitionOwn,
        ReportingRead,
    ];

    public static IReadOnlyList<string> ForRole(string role) => role switch
    {
        TenantRoles.Owner => All,
        TenantRoles.Manager =>
        [
            TenantRead,
            TenantSwitch,
            MembershipRead,
            SessionManageOwn,
            CustomerRead,
            CustomerManage,
            ServiceRead,
            ServiceManage,
            EmployeeRead,
            EmployeeManage,
            SchedulingManage,
            AvailabilityRead,
            AppointmentRead,
            AppointmentManage,
            ReportingRead,
        ],
        TenantRoles.Receptionist =>
        [
            TenantRead,
            TenantSwitch,
            SessionManageOwn,
            CustomerRead,
            CustomerManage,
            ServiceRead,
            EmployeeRead,
            AvailabilityRead,
            AppointmentRead,
            AppointmentManage,
        ],
        TenantRoles.Employee =>
        [
            TenantRead,
            TenantSwitch,
            SessionManageOwn,
            ServiceRead,
            AvailabilityRead,
            AppointmentReadOwn,
            AppointmentTransitionOwn,
        ],
        _ => [],
    };
}
