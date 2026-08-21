namespace AppointmentCrm.Domain.Identity;

public sealed class RoleDefinition
{
    private RoleDefinition()
    {
    }

    public string Code { get; private set; } = string.Empty;

    public string Name { get; private set; } = string.Empty;
}

public sealed class PermissionDefinition
{
    private PermissionDefinition()
    {
    }

    public string Code { get; private set; } = string.Empty;

    public string Name { get; private set; } = string.Empty;
}

public sealed class RolePermission
{
    private RolePermission()
    {
    }

    public string RoleCode { get; private set; } = string.Empty;

    public string PermissionCode { get; private set; } = string.Empty;
}
