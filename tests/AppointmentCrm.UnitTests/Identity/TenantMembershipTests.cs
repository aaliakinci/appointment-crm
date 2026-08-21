using AppointmentCrm.Domain.Identity;

namespace AppointmentCrm.UnitTests.Identity;

public sealed class TenantMembershipTests
{
    [Fact]
    public void RoleAndActiveChanges_AdvanceAuthorizationVersion()
    {
        var now = DateTimeOffset.Parse("2026-08-21T00:00:00Z");
        var membership = TenantMembership.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            TenantRoles.Manager,
            now);

        membership.ChangeRole(TenantRoles.Receptionist, now.AddMinutes(1));
        membership.SetActive(false, now.AddMinutes(2));

        Assert.Equal(3, membership.AuthorizationVersion);
        Assert.Equal(TenantRoles.Receptionist, membership.Role);
        Assert.False(membership.IsActive);
    }

    [Fact]
    public void UnknownRole_IsRejected()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => TenantMembership.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Administrator",
            DateTimeOffset.UtcNow));
    }
}
