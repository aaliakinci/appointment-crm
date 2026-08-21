using System.Reflection;
using AppointmentCrm.Api.Identity;
using AppointmentCrm.Api.Security;
using AppointmentCrm.Application.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace AppointmentCrm.UnitTests.Identity;

public sealed class ControllerAuthorizationTests
{
    [Theory]
    [InlineData(typeof(AuthController))]
    [InlineData(typeof(MembershipsController))]
    public void IdentityControllers_RequireAuthenticationAtClassLevel(Type controllerType)
    {
        Assert.True(controllerType.IsSubclassOf(typeof(ControllerBase)));
        Assert.NotNull(controllerType.GetCustomAttribute<ApiControllerAttribute>());
        Assert.NotNull(controllerType.GetCustomAttribute<AuthorizeAttribute>());
    }

    [Theory]
    [InlineData(nameof(AuthController.LoginAsync), "login")]
    [InlineData(nameof(AuthController.RefreshAsync), "refresh")]
    public void AnonymousAuthActions_DeclareAnonymousAccessAndRateLimit(
        string methodName,
        string rateLimitPolicy)
    {
        MethodInfo method = GetRequiredMethod(typeof(AuthController), methodName);

        Assert.NotNull(method.GetCustomAttribute<AllowAnonymousAttribute>());
        EnableRateLimitingAttribute rateLimit = Assert.IsType<EnableRateLimitingAttribute>(
            method.GetCustomAttribute<EnableRateLimitingAttribute>());
        Assert.Equal(rateLimitPolicy, rateLimit.PolicyName);
    }

    [Theory]
    [InlineData(nameof(AuthController.RevokeAllAsync), Permissions.SessionManageOwn)]
    [InlineData(nameof(AuthController.SwitchTenantAsync), Permissions.TenantSwitch)]
    [InlineData(nameof(AuthController.ListAvailableTenantsAsync), Permissions.TenantSwitch)]
    public void ProtectedAuthActions_DeclareExpectedPolicy(
        string methodName,
        string expectedPolicy)
    {
        AssertPolicy(typeof(AuthController), methodName, expectedPolicy);
    }

    [Theory]
    [InlineData(nameof(AuthController.RefreshAsync))]
    [InlineData(nameof(AuthController.LogoutAsync))]
    [InlineData(nameof(AuthController.RevokeAllAsync))]
    [InlineData(nameof(AuthController.SwitchTenantAsync))]
    public void SessionMutationActions_ValidateTrustedOrigin(string methodName)
    {
        MethodInfo method = GetRequiredMethod(typeof(AuthController), methodName);

        Assert.NotNull(method.GetCustomAttribute<ValidateTrustedOriginAttribute>());
    }

    [Theory]
    [InlineData(nameof(MembershipsController.ListAsync), Permissions.MembershipRead)]
    [InlineData(nameof(MembershipsController.GetReportAsync), Permissions.MembershipRead)]
    [InlineData(nameof(MembershipsController.GetAsync), Permissions.MembershipRead)]
    [InlineData(nameof(MembershipsController.UpdateAsync), Permissions.MembershipManage)]
    [InlineData(nameof(MembershipsController.ArchiveAsync), Permissions.MembershipManage)]
    public void MembershipActions_DeclareExpectedPolicy(
        string methodName,
        string expectedPolicy)
    {
        AssertPolicy(typeof(MembershipsController), methodName, expectedPolicy);
    }

    private static void AssertPolicy(
        Type controllerType,
        string methodName,
        string expectedPolicy)
    {
        MethodInfo method = GetRequiredMethod(controllerType, methodName);
        AuthorizeAttribute authorize = Assert.IsType<AuthorizeAttribute>(
            method.GetCustomAttribute<AuthorizeAttribute>());

        Assert.Equal(expectedPolicy, authorize.Policy);
    }

    private static MethodInfo GetRequiredMethod(Type controllerType, string methodName) =>
        controllerType.GetMethod(methodName)
        ?? throw new InvalidOperationException(
            $"Method '{controllerType.Name}.{methodName}' was not found.");
}
