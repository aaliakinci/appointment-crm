using System.Reflection;
using AppointmentCrm.Api.Customers;
using AppointmentCrm.Api.Employees;
using AppointmentCrm.Api.Identity;
using AppointmentCrm.Api.Security;
using AppointmentCrm.Api.Services;
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
    [InlineData(typeof(CustomersController))]
    [InlineData(typeof(ServicesController))]
    [InlineData(typeof(EmployeesController))]
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

    [Theory]
    [InlineData(typeof(CustomersController), nameof(CustomersController.ListAsync), Permissions.CustomerRead)]
    [InlineData(typeof(CustomersController), nameof(CustomersController.GetAsync), Permissions.CustomerRead)]
    [InlineData(typeof(CustomersController), nameof(CustomersController.CreateAsync), Permissions.CustomerManage)]
    [InlineData(typeof(CustomersController), nameof(CustomersController.UpdateAsync), Permissions.CustomerManage)]
    [InlineData(typeof(CustomersController), nameof(CustomersController.ArchiveAsync), Permissions.CustomerManage)]
    [InlineData(typeof(ServicesController), nameof(ServicesController.ListAsync), Permissions.ServiceRead)]
    [InlineData(typeof(ServicesController), nameof(ServicesController.GetAsync), Permissions.ServiceRead)]
    [InlineData(typeof(ServicesController), nameof(ServicesController.CreateAsync), Permissions.ServiceManage)]
    [InlineData(typeof(ServicesController), nameof(ServicesController.UpdateAsync), Permissions.ServiceManage)]
    [InlineData(typeof(ServicesController), nameof(ServicesController.ActivateAsync), Permissions.ServiceManage)]
    [InlineData(typeof(ServicesController), nameof(ServicesController.DeactivateAsync), Permissions.ServiceManage)]
    [InlineData(typeof(EmployeesController), nameof(EmployeesController.ListAsync), Permissions.EmployeeRead)]
    [InlineData(typeof(EmployeesController), nameof(EmployeesController.GetAsync), Permissions.EmployeeRead)]
    [InlineData(typeof(EmployeesController), nameof(EmployeesController.CreateAsync), Permissions.EmployeeManage)]
    [InlineData(typeof(EmployeesController), nameof(EmployeesController.UpdateAsync), Permissions.EmployeeManage)]
    [InlineData(typeof(EmployeesController), nameof(EmployeesController.SetServicesAsync), Permissions.EmployeeManage)]
    [InlineData(typeof(EmployeesController), nameof(EmployeesController.ActivateAsync), Permissions.EmployeeManage)]
    [InlineData(typeof(EmployeesController), nameof(EmployeesController.DeactivateAsync), Permissions.EmployeeManage)]
    public void MasterDataActions_DeclareExpectedPolicy(
        Type controllerType,
        string methodName,
        string expectedPolicy)
    {
        AssertPolicy(controllerType, methodName, expectedPolicy);
    }

    [Theory]
    [InlineData(typeof(CustomersController), nameof(CustomersController.CreateAsync))]
    [InlineData(typeof(CustomersController), nameof(CustomersController.UpdateAsync))]
    [InlineData(typeof(CustomersController), nameof(CustomersController.ArchiveAsync))]
    [InlineData(typeof(ServicesController), nameof(ServicesController.CreateAsync))]
    [InlineData(typeof(ServicesController), nameof(ServicesController.UpdateAsync))]
    [InlineData(typeof(ServicesController), nameof(ServicesController.ActivateAsync))]
    [InlineData(typeof(ServicesController), nameof(ServicesController.DeactivateAsync))]
    [InlineData(typeof(EmployeesController), nameof(EmployeesController.CreateAsync))]
    [InlineData(typeof(EmployeesController), nameof(EmployeesController.UpdateAsync))]
    [InlineData(typeof(EmployeesController), nameof(EmployeesController.SetServicesAsync))]
    [InlineData(typeof(EmployeesController), nameof(EmployeesController.ActivateAsync))]
    [InlineData(typeof(EmployeesController), nameof(EmployeesController.DeactivateAsync))]
    public void MasterDataMutations_ValidateTrustedOrigin(Type controllerType, string methodName)
    {
        MethodInfo method = GetRequiredMethod(controllerType, methodName);

        Assert.NotNull(method.GetCustomAttribute<ValidateTrustedOriginAttribute>());
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
