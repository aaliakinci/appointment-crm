namespace AppointmentCrm.Application.Scheduling;

public static class SchedulingErrorCodes
{
    public const string EmployeeNotFound = "scheduling.employee_not_found";
    public const string ServiceNotFound = "availability.service_not_found";
    public const string EmployeeInactive = "availability.employee_inactive";
    public const string ServiceInactive = "availability.service_inactive";
    public const string ServiceNotAssigned = "availability.service_not_assigned";
    public const string InvalidSchedule = "scheduling.invalid_schedule";
    public const string ScheduleConflict = "scheduling.schedule_conflict";
    public const string ScheduleVersionConflict = "scheduling.schedule_version_conflict";
    public const string ScheduleVersionNotFound = "scheduling.schedule_version_not_found";
    public const string InvalidDateRange = "scheduling.invalid_date_range";
    public const string TimeOffOverlap = "scheduling.time_off_overlap";
    public const string TimeOffNotFound = "scheduling.time_off_not_found";
    public const string TimeZoneMismatch = "scheduling.time_zone_mismatch";
    public const string InvalidLocalTime = "scheduling.invalid_local_time";
    public const string AmbiguousLocalTime = "scheduling.ambiguous_local_time";
}
