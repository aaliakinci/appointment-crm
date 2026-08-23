namespace AppointmentCrm.Application.Appointments;

public static class AppointmentErrorCodes
{
    public const string NotFound = "appointments.not_found";
    public const string CustomerNotFound = "appointments.customer_not_found";
    public const string CustomerArchived = "appointments.customer_archived";
    public const string EmployeeNotFound = "appointments.employee_not_found";
    public const string EmployeeInactive = "appointments.employee_inactive";
    public const string CurrentEmployeeNotFound = "appointments.current_employee_not_found";
    public const string ServiceNotFound = "appointments.service_not_found";
    public const string ServiceInactive = "appointments.service_inactive";
    public const string ServiceNotAssigned = "appointments.service_not_assigned";
    public const string SlotUnavailable = "appointments.slot_unavailable";
    public const string TimeConflict = "appointments.time_conflict";
    public const string InvalidTransition = "appointments.invalid_transition";
    public const string TransitionForbidden = "appointments.transition_forbidden";
    public const string VersionConflict = "appointments.version_conflict";
    public const string InvalidDateRange = "appointments.invalid_date_range";
}
