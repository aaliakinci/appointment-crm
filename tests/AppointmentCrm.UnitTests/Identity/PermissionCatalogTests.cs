using AppointmentCrm.Application.Identity;
using AppointmentCrm.Domain.Identity;

namespace AppointmentCrm.UnitTests.Identity;

public sealed class PermissionCatalogTests
{
    [Fact]
    public void ReceptionistAndEmployee_CannotManageMemberships()
    {
        Assert.DoesNotContain(
            Permissions.MembershipManage,
            Permissions.ForRole(TenantRoles.Receptionist));
        Assert.DoesNotContain(
            Permissions.MembershipManage,
            Permissions.ForRole(TenantRoles.Employee));
    }

    [Fact]
    public void Owner_CanManageMemberships()
    {
        Assert.Contains(
            Permissions.MembershipManage,
            Permissions.ForRole(TenantRoles.Owner));
    }
}
