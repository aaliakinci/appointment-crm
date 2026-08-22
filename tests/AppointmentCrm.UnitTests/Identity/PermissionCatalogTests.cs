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

    [Fact]
    public void MasterDataPermissions_FollowTheExpectedRoleMatrix()
    {
        IReadOnlyList<string> manager = Permissions.ForRole(TenantRoles.Manager);
        Assert.Contains(Permissions.CustomerManage, manager);
        Assert.Contains(Permissions.ServiceManage, manager);
        Assert.Contains(Permissions.EmployeeManage, manager);

        IReadOnlyList<string> receptionist = Permissions.ForRole(TenantRoles.Receptionist);
        Assert.Contains(Permissions.CustomerManage, receptionist);
        Assert.Contains(Permissions.ServiceRead, receptionist);
        Assert.Contains(Permissions.EmployeeRead, receptionist);
        Assert.DoesNotContain(Permissions.ServiceManage, receptionist);
        Assert.DoesNotContain(Permissions.EmployeeManage, receptionist);

        IReadOnlyList<string> employee = Permissions.ForRole(TenantRoles.Employee);
        Assert.Contains(Permissions.ServiceRead, employee);
        Assert.DoesNotContain(Permissions.CustomerRead, employee);
        Assert.DoesNotContain(Permissions.EmployeeRead, employee);
    }
}
