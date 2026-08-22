namespace AppointmentCrm.Application.Employees;

public static class EmployeeErrorCodes
{
    public const string UserNotInTenant = "employees.user_not_in_tenant";
    public const string UserAlreadyLinked = "employees.user_already_linked";
    public const string DuplicateServiceAssignment = "employees.duplicate_service_assignment";
    public const string ServiceAssignmentInvalid = "employees.service_assignment_invalid";
}
