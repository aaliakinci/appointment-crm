using System.Reflection;
using AppointmentCrm.Api.Appointments;
using AppointmentCrm.Api.Auditing;
using AppointmentCrm.Api.Customers;
using AppointmentCrm.Api.Employees;
using AppointmentCrm.Api.Identity;
using AppointmentCrm.Api.Reporting;
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
    [InlineData(typeof(AppointmentsController))]
    [InlineData(typeof(MyAppointmentsController))]
    [InlineData(typeof(AccountController))]
    [InlineData(typeof(ReportingController))]
    [InlineData(typeof(AuditController))]
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
    [InlineData(typeof(ReportingController), nameof(ReportingController.GetDashboardAsync))]
    [InlineData(typeof(AuditController), nameof(AuditController.ListAsync))]
    public void OperationalReadModels_RequireReportingPermission(
        Type controllerType,
        string methodName)
    {
        AssertPolicy(controllerType, methodName, Permissions.ReportingRead);
    }

    [Theory]
    [InlineData(nameof(AccountController.ListSessionsAsync), Permissions.SessionManageOwn)]
    [InlineData(nameof(AccountController.RevokeSessionAsync), Permissions.SessionManageOwn)]
    public void AccountSessionActions_DeclareExpectedPolicy(
        string methodName,
        string expectedPolicy)
    {
        AssertPolicy(typeof(AccountController), methodName, expectedPolicy);
    }

    [Theory]
    [InlineData(nameof(AccountController.UpdateProfileAsync))]
    [InlineData(nameof(AccountController.RevokeSessionAsync))]
    public void AccountMutations_ValidateTrustedOrigin(string methodName)
    {
        MethodInfo method = GetRequiredMethod(typeof(AccountController), methodName);
        Assert.NotNull(method.GetCustomAttribute<ValidateTrustedOriginAttribute>());
    }

    [Fact]
    public void CustomerAppointmentHistory_RequiresCustomerAndAppointmentReadPermissions()
    {
        MethodInfo method = GetRequiredMethod(
            typeof(CustomersController),
            nameof(CustomersController.ListAppointmentHistoryAsync));
        string?[] policies = method.GetCustomAttributes<AuthorizeAttribute>()
            .Select(attribute => attribute.Policy)
            .ToArray();

        Assert.Contains(Permissions.CustomerRead, policies);
        Assert.Contains(Permissions.AppointmentRead, policies);
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

    [Theory]
    [InlineData(typeof(AppointmentsController), nameof(AppointmentsController.ListAsync), Permissions.AppointmentRead)]
    [InlineData(typeof(AppointmentsController), nameof(AppointmentsController.GetAsync), Permissions.AppointmentRead)]
    [InlineData(typeof(AppointmentsController), nameof(AppointmentsController.CreateAsync), Permissions.AppointmentManage)]
    [InlineData(typeof(AppointmentsController), nameof(AppointmentsController.RescheduleAsync), Permissions.AppointmentManage)]
    [InlineData(typeof(AppointmentsController), nameof(AppointmentsController.ConfirmAsync), Permissions.AppointmentManage)]
    [InlineData(typeof(AppointmentsController), nameof(AppointmentsController.CompleteAsync), Permissions.AppointmentManage)]
    [InlineData(typeof(AppointmentsController), nameof(AppointmentsController.CancelAsync), Permissions.AppointmentManage)]
    [InlineData(typeof(AppointmentsController), nameof(AppointmentsController.NoShowAsync), Permissions.AppointmentManage)]
    [InlineData(typeof(MyAppointmentsController), nameof(MyAppointmentsController.ListAsync), Permissions.AppointmentReadOwn)]
    [InlineData(typeof(MyAppointmentsController), nameof(MyAppointmentsController.GetAsync), Permissions.AppointmentReadOwn)]
    [InlineData(typeof(MyAppointmentsController), nameof(MyAppointmentsController.ConfirmAsync), Permissions.AppointmentTransitionOwn)]
    [InlineData(typeof(MyAppointmentsController), nameof(MyAppointmentsController.CompleteAsync), Permissions.AppointmentTransitionOwn)]
    [InlineData(typeof(MyAppointmentsController), nameof(MyAppointmentsController.NoShowAsync), Permissions.AppointmentTransitionOwn)]
    public void AppointmentActions_DeclareExpectedPolicy(
        Type controllerType,
        string methodName,
        string expectedPolicy)
    {
        AssertPolicy(controllerType, methodName, expectedPolicy);
    }

    [Theory]
    [InlineData(typeof(AppointmentsController), nameof(AppointmentsController.CreateAsync))]
    [InlineData(typeof(AppointmentsController), nameof(AppointmentsController.RescheduleAsync))]
    [InlineData(typeof(AppointmentsController), nameof(AppointmentsController.ConfirmAsync))]
    [InlineData(typeof(AppointmentsController), nameof(AppointmentsController.CompleteAsync))]
    [InlineData(typeof(AppointmentsController), nameof(AppointmentsController.CancelAsync))]
    [InlineData(typeof(AppointmentsController), nameof(AppointmentsController.NoShowAsync))]
    [InlineData(typeof(MyAppointmentsController), nameof(MyAppointmentsController.ConfirmAsync))]
    [InlineData(typeof(MyAppointmentsController), nameof(MyAppointmentsController.CompleteAsync))]
    [InlineData(typeof(MyAppointmentsController), nameof(MyAppointmentsController.NoShowAsync))]
    public void AppointmentMutations_ValidateTrustedOrigin(Type controllerType, string methodName)
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
