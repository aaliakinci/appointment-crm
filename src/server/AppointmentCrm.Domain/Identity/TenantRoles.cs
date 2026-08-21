namespace AppointmentCrm.Domain.Identity;

public static class TenantRoles
{
    public const string Owner = "Owner";
    public const string Manager = "Manager";
    public const string Receptionist = "Receptionist";
    public const string Employee = "Employee";

    public static IReadOnlyList<string> All { get; } =
        [Owner, Manager, Receptionist, Employee];

    public static bool IsDefined(string role) =>
        All.Contains(role, StringComparer.Ordinal);
}
