using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using AppointmentCrm.Application.Tenancy;
using AppointmentCrm.Contracts;
using AppointmentCrm.Infrastructure;
using AppointmentCrm.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace AppointmentCrm.IntegrationTests;

public sealed class MasterDataTests : IClassFixture<ApiFactory>, IAsyncLifetime
{
    private static readonly Guid AtlasTenantId =
        Guid.Parse("10000000-0000-0000-0000-000000000001");
    private static readonly Guid NorthwindTenantId =
        Guid.Parse("10000000-0000-0000-0000-000000000002");
    private static readonly Guid OwnerUserId =
        Guid.Parse("20000000-0000-0000-0000-000000000001");
    private static readonly Guid ManagerUserId =
        Guid.Parse("20000000-0000-0000-0000-000000000002");

    private readonly ApiFactory _factory;

    public MasterDataTests(ApiFactory factory)
    {
        _factory = factory;
    }

    public async Task InitializeAsync()
    {
        await MigrationRunner.RunAsync(_factory.Services);
        await ResetMasterDataAsync();
        await MigrationRunner.RunAsync(_factory.Services);
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task CustomerFlow_NormalizesPaginatesArchivesAndAudits()
    {
        using HttpClient client = CreateClient();
        string accessToken = await LoginAsync(client, "owner@demo.local", AtlasTenantId);
        var input = new CreateCustomerRequest(
            "  Zeynep Kaya  ",
            "Zeynep.Kaya@Example.Test",
            "+90 (555) 111 22 33",
            "  First visit  ");

        using HttpResponseMessage createResponse = await client.SendAsync(AuthorizedWrite(
            HttpMethod.Post,
            "/api/v1/customers",
            accessToken,
            input));
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        CustomerResponse? created = await createResponse.Content.ReadFromJsonAsync<CustomerResponse>();
        Assert.NotNull(created);
        Assert.Equal("Zeynep Kaya", created.Name);
        Assert.Equal("First visit", created.Notes);

        using HttpResponseMessage searchResponse = await client.SendAsync(Authorized(
            HttpMethod.Get,
            "/api/v1/customers?search=555111&pageSize=1",
            accessToken));
        searchResponse.EnsureSuccessStatusCode();
        PagedResponse<CustomerResponse>? page = await searchResponse.Content
            .ReadFromJsonAsync<PagedResponse<CustomerResponse>>();
        Assert.NotNull(page);
        Assert.Equal(1, page.PageSize);
        Assert.Equal(1, page.TotalCount);
        Assert.Equal(created.Id, page.Items.Single().Id);

        using HttpResponseMessage duplicateResponse = await client.SendAsync(AuthorizedWrite(
            HttpMethod.Post,
            "/api/v1/customers",
            accessToken,
            input with { Name = "Duplicate", Email = "zeynep.kaya@example.test" }));
        Assert.Equal(HttpStatusCode.Conflict, duplicateResponse.StatusCode);

        using HttpResponseMessage archiveResponse = await client.SendAsync(AuthorizedWrite(
            HttpMethod.Delete,
            $"/api/v1/customers/{created.Id}",
            accessToken));
        Assert.Equal(HttpStatusCode.NoContent, archiveResponse.StatusCode);

        using HttpResponseMessage activeListResponse = await client.SendAsync(Authorized(
            HttpMethod.Get,
            "/api/v1/customers?pageSize=100",
            accessToken));
        PagedResponse<CustomerResponse>? activePage = await activeListResponse.Content
            .ReadFromJsonAsync<PagedResponse<CustomerResponse>>();
        Assert.NotNull(activePage);
        Assert.DoesNotContain(activePage.Items, customer => customer.Id == created.Id);

        await using var dbContext = CreateTenantDbContext(AtlasTenantId);
        var auditEntries = await dbContext.AuditEntries
            .Where(entry => entry.TargetId == created.Id)
            .OrderBy(entry => entry.OccurredAtUtc)
            .ToListAsync();
        Assert.Equal(["customer.created", "customer.archived"],
            auditEntries.Select(entry => entry.Action));
        Assert.All(auditEntries, entry => Assert.Equal(OwnerUserId, entry.ActorUserId));
    }

    [Fact]
    public async Task ServiceAndEmployeeFlow_ValidatesCurrencyAssignmentsAndActiveSelectors()
    {
        using HttpClient client = CreateClient();
        string accessToken = await LoginAsync(client, "owner@demo.local", AtlasTenantId);

        using HttpResponseMessage currencyConflict = await client.SendAsync(AuthorizedWrite(
            HttpMethod.Post,
            "/api/v1/services",
            accessToken,
            new CreateServiceRequest("Color consultation", 45, 900m, "EUR")));
        Assert.Equal(HttpStatusCode.Conflict, currencyConflict.StatusCode);

        using HttpResponseMessage createServiceResponse = await client.SendAsync(AuthorizedWrite(
            HttpMethod.Post,
            "/api/v1/services",
            accessToken,
            new CreateServiceRequest("Color consultation", 45, 900m, "try")));
        Assert.Equal(HttpStatusCode.Created, createServiceResponse.StatusCode);
        ServiceResponse? service = await createServiceResponse.Content
            .ReadFromJsonAsync<ServiceResponse>();
        Assert.NotNull(service);
        Assert.Equal("TRY", service.Currency);

        using HttpResponseMessage deactivateService = await client.SendAsync(AuthorizedWrite(
            HttpMethod.Post,
            $"/api/v1/services/{service.Id}/deactivate",
            accessToken));
        deactivateService.EnsureSuccessStatusCode();

        using HttpResponseMessage inactiveAssignment = await client.SendAsync(AuthorizedWrite(
            HttpMethod.Post,
            "/api/v1/employees",
            accessToken,
            new CreateEmployeeRequest(
                ManagerUserId,
                "Demo Manager",
                "manager@demo.local",
                null,
                [service.Id])));
        Assert.Equal(HttpStatusCode.Conflict, inactiveAssignment.StatusCode);

        using HttpResponseMessage activateService = await client.SendAsync(AuthorizedWrite(
            HttpMethod.Post,
            $"/api/v1/services/{service.Id}/activate",
            accessToken));
        activateService.EnsureSuccessStatusCode();

        using HttpResponseMessage createEmployeeResponse = await client.SendAsync(AuthorizedWrite(
            HttpMethod.Post,
            "/api/v1/employees",
            accessToken,
            new CreateEmployeeRequest(
                ManagerUserId,
                "Demo Manager",
                "manager@demo.local",
                "+90 555 777 88 99",
                [service.Id])));
        Assert.Equal(HttpStatusCode.Created, createEmployeeResponse.StatusCode);
        EmployeeResponse? employee = await createEmployeeResponse.Content
            .ReadFromJsonAsync<EmployeeResponse>();
        Assert.NotNull(employee);
        Assert.Equal(service.Id, employee.Services.Single().Id);

        using HttpResponseMessage duplicateLink = await client.SendAsync(AuthorizedWrite(
            HttpMethod.Post,
            "/api/v1/employees",
            accessToken,
            new CreateEmployeeRequest(ManagerUserId, "Second manager", null, null, [])));
        Assert.Equal(HttpStatusCode.Conflict, duplicateLink.StatusCode);

        using HttpResponseMessage deactivateEmployee = await client.SendAsync(AuthorizedWrite(
            HttpMethod.Post,
            $"/api/v1/employees/{employee.Id}/deactivate",
            accessToken));
        deactivateEmployee.EnsureSuccessStatusCode();

        using HttpResponseMessage activeEmployees = await client.SendAsync(Authorized(
            HttpMethod.Get,
            "/api/v1/employees?isActive=true&pageSize=100",
            accessToken));
        PagedResponse<EmployeeResponse>? employeePage = await activeEmployees.Content
            .ReadFromJsonAsync<PagedResponse<EmployeeResponse>>();
        Assert.NotNull(employeePage);
        Assert.DoesNotContain(employeePage.Items, candidate => candidate.Id == employee.Id);

        await using var dbContext = CreateTenantDbContext(AtlasTenantId);
        Assert.Contains(
            await dbContext.AuditEntries.Select(entry => entry.Action).ToListAsync(),
            action => action == "service.activation-changed");
        Assert.Contains(
            await dbContext.AuditEntries.Select(entry => entry.Action).ToListAsync(),
            action => action == "employee.activation-changed");
    }

    [Fact]
    public async Task MasterDataAuthorization_SeparatesReceptionistAndEmployeeCapabilities()
    {
        using HttpClient client = CreateClient();
        string receptionist = await LoginAsync(client, "receptionist@demo.local", AtlasTenantId);

        using HttpResponseMessage customerCreate = await client.SendAsync(AuthorizedWrite(
            HttpMethod.Post,
            "/api/v1/customers",
            receptionist,
            new CreateCustomerRequest("Reception customer", null, null, null)));
        Assert.Equal(HttpStatusCode.Created, customerCreate.StatusCode);

        using HttpResponseMessage serviceList = await client.SendAsync(Authorized(
            HttpMethod.Get,
            "/api/v1/services",
            receptionist));
        serviceList.EnsureSuccessStatusCode();
        using HttpResponseMessage employeeList = await client.SendAsync(Authorized(
            HttpMethod.Get,
            "/api/v1/employees",
            receptionist));
        employeeList.EnsureSuccessStatusCode();

        using HttpResponseMessage serviceCreate = await client.SendAsync(AuthorizedWrite(
            HttpMethod.Post,
            "/api/v1/services",
            receptionist,
            new CreateServiceRequest("Forbidden", 30, 100m, "TRY")));
        Assert.Equal(HttpStatusCode.Forbidden, serviceCreate.StatusCode);
        using HttpResponseMessage employeeCreate = await client.SendAsync(AuthorizedWrite(
            HttpMethod.Post,
            "/api/v1/employees",
            receptionist,
            new CreateEmployeeRequest(null, "Forbidden", null, null, [])));
        Assert.Equal(HttpStatusCode.Forbidden, employeeCreate.StatusCode);

        string employee = await LoginAsync(client, "employee@demo.local", AtlasTenantId);
        using HttpResponseMessage employeeServiceList = await client.SendAsync(Authorized(
            HttpMethod.Get,
            "/api/v1/services",
            employee));
        employeeServiceList.EnsureSuccessStatusCode();
        using HttpResponseMessage forbiddenCustomers = await client.SendAsync(Authorized(
            HttpMethod.Get,
            "/api/v1/customers",
            employee));
        Assert.Equal(HttpStatusCode.Forbidden, forbiddenCustomers.StatusCode);
        using HttpResponseMessage forbiddenEmployees = await client.SendAsync(Authorized(
            HttpMethod.Get,
            "/api/v1/employees",
            employee));
        Assert.Equal(HttpStatusCode.Forbidden, forbiddenEmployees.StatusCode);
    }

    [Fact]
    public async Task MasterDataEndpoints_EnforcePaginationAndTenantIsolationForGuessedIds()
    {
        using HttpClient client = CreateClient();
        string atlasToken = await LoginAsync(client, "owner@demo.local", AtlasTenantId);
        CustomerResponse customer = await CreateCustomerAsync(client, atlasToken);
        ServiceResponse service = await CreateServiceAsync(client, atlasToken);
        EmployeeResponse employee = await CreateEmployeeAsync(client, atlasToken, service.Id);

        using HttpResponseMessage oversizedPage = await client.SendAsync(Authorized(
            HttpMethod.Get,
            "/api/v1/customers?pageSize=101",
            atlasToken));
        Assert.Equal(HttpStatusCode.BadRequest, oversizedPage.StatusCode);

        string northwindToken = await LoginAsync(client, "owner@demo.local", NorthwindTenantId);
        foreach (string path in new[]
        {
            $"/api/v1/customers/{customer.Id}",
            $"/api/v1/services/{service.Id}",
            $"/api/v1/employees/{employee.Id}",
        })
        {
            using HttpResponseMessage response = await client.SendAsync(Authorized(
                HttpMethod.Get,
                path,
                northwindToken));
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        using HttpResponseMessage customerUpdate = await client.SendAsync(AuthorizedWrite(
            HttpMethod.Put,
            $"/api/v1/customers/{customer.Id}",
            northwindToken,
            new UpdateCustomerRequest("Guessed", null, null, null)));
        Assert.Equal(HttpStatusCode.NotFound, customerUpdate.StatusCode);
        using HttpResponseMessage customerArchive = await client.SendAsync(AuthorizedWrite(
            HttpMethod.Delete,
            $"/api/v1/customers/{customer.Id}",
            northwindToken));
        Assert.Equal(HttpStatusCode.NotFound, customerArchive.StatusCode);

        using HttpResponseMessage serviceUpdate = await client.SendAsync(AuthorizedWrite(
            HttpMethod.Put,
            $"/api/v1/services/{service.Id}",
            northwindToken,
            new UpdateServiceRequest("Guessed", 30, 100m, "TRY")));
        Assert.Equal(HttpStatusCode.NotFound, serviceUpdate.StatusCode);
        using HttpResponseMessage serviceActivation = await client.SendAsync(AuthorizedWrite(
            HttpMethod.Post,
            $"/api/v1/services/{service.Id}/deactivate",
            northwindToken));
        Assert.Equal(HttpStatusCode.NotFound, serviceActivation.StatusCode);

        using HttpResponseMessage employeeUpdate = await client.SendAsync(AuthorizedWrite(
            HttpMethod.Put,
            $"/api/v1/employees/{employee.Id}",
            northwindToken,
            new UpdateEmployeeRequest(null, "Guessed", null, null)));
        Assert.Equal(HttpStatusCode.NotFound, employeeUpdate.StatusCode);
        using HttpResponseMessage employeeServices = await client.SendAsync(AuthorizedWrite(
            HttpMethod.Put,
            $"/api/v1/employees/{employee.Id}/services",
            northwindToken,
            new SetEmployeeServicesRequest([])));
        Assert.Equal(HttpStatusCode.NotFound, employeeServices.StatusCode);
        using HttpResponseMessage employeeActivation = await client.SendAsync(AuthorizedWrite(
            HttpMethod.Post,
            $"/api/v1/employees/{employee.Id}/deactivate",
            northwindToken));
        Assert.Equal(HttpStatusCode.NotFound, employeeActivation.StatusCode);
    }

    private HttpClient CreateClient() => _factory.CreateClient(
        new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            HandleCookies = false,
        });

    private AppointmentCrmDbContext CreateTenantDbContext(Guid tenantId) =>
        new(
            new DbContextOptionsBuilder<AppointmentCrmDbContext>()
                .UseNpgsql(_factory.ConnectionString)
                .Options,
            new TestTenantContext(tenantId));

    private static HttpRequestMessage Authorized(
        HttpMethod method,
        string uri,
        string accessToken,
        object? body = null)
    {
        var request = new HttpRequestMessage(method, uri);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        if (body is not null)
        {
            request.Content = JsonContent.Create(body);
        }

        return request;
    }

    private static HttpRequestMessage AuthorizedWrite(
        HttpMethod method,
        string uri,
        string accessToken,
        object? body = null)
    {
        HttpRequestMessage request = Authorized(method, uri, accessToken, body);
        request.Headers.Add("Origin", "http://localhost:5173");
        return request;
    }

    private static async Task<string> LoginAsync(
        HttpClient client,
        string email,
        Guid tenantId)
    {
        using HttpResponseMessage response = await client.PostAsJsonAsync(
            "/api/v1/auth/login",
            new LoginRequest(email, IdentitySecurityTests.DemoPassword, tenantId));
        response.EnsureSuccessStatusCode();
        AuthenticationResponse? payload = await response.Content
            .ReadFromJsonAsync<AuthenticationResponse>();
        Assert.NotNull(payload?.AccessToken);
        return payload.AccessToken;
    }

    private static async Task<CustomerResponse> CreateCustomerAsync(
        HttpClient client,
        string accessToken)
    {
        using HttpResponseMessage response = await client.SendAsync(AuthorizedWrite(
            HttpMethod.Post,
            "/api/v1/customers",
            accessToken,
            new CreateCustomerRequest(
                "Isolation customer",
                "isolation.customer@example.test",
                "+90 555 300 40 50",
                null)));
        response.EnsureSuccessStatusCode();
        CustomerResponse? customer = await response.Content.ReadFromJsonAsync<CustomerResponse>();
        return customer ?? throw new InvalidOperationException("Customer response was empty.");
    }

    private static async Task<ServiceResponse> CreateServiceAsync(
        HttpClient client,
        string accessToken)
    {
        using HttpResponseMessage response = await client.SendAsync(AuthorizedWrite(
            HttpMethod.Post,
            "/api/v1/services",
            accessToken,
            new CreateServiceRequest("Isolation service", 30, 500m, "TRY")));
        response.EnsureSuccessStatusCode();
        ServiceResponse? service = await response.Content.ReadFromJsonAsync<ServiceResponse>();
        return service ?? throw new InvalidOperationException("Service response was empty.");
    }

    private static async Task<EmployeeResponse> CreateEmployeeAsync(
        HttpClient client,
        string accessToken,
        Guid serviceId)
    {
        using HttpResponseMessage response = await client.SendAsync(AuthorizedWrite(
            HttpMethod.Post,
            "/api/v1/employees",
            accessToken,
            new CreateEmployeeRequest(
                ManagerUserId,
                "Isolation employee",
                "isolation.employee@example.test",
                null,
                [serviceId])));
        response.EnsureSuccessStatusCode();
        EmployeeResponse? employee = await response.Content.ReadFromJsonAsync<EmployeeResponse>();
        return employee ?? throw new InvalidOperationException("Employee response was empty.");
    }

    private async Task ResetMasterDataAsync()
    {
        await using AsyncServiceScope scope = _factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppointmentCrmDbContext>();
        await dbContext.Database.ExecuteSqlRawAsync("""
            TRUNCATE TABLE
                audit_entries,
                employee_services,
                employees,
                services,
                customers,
                user_sessions;
            """);
        await dbContext.Database.ExecuteSqlRawAsync("""
            UPDATE users
            SET is_active = TRUE, security_version = 1;

            UPDATE tenant_memberships
            SET is_active = TRUE,
                authorization_version = 1,
                role = CASE id
                    WHEN '30000000-0000-0000-0000-000000000001'::uuid THEN 'Owner'
                    WHEN '30000000-0000-0000-0000-000000000002'::uuid THEN 'Owner'
                    WHEN '30000000-0000-0000-0000-000000000003'::uuid THEN 'Manager'
                    WHEN '30000000-0000-0000-0000-000000000004'::uuid THEN 'Receptionist'
                    WHEN '30000000-0000-0000-0000-000000000005'::uuid THEN 'Employee'
                    WHEN '30000000-0000-0000-0000-000000000006'::uuid THEN 'Owner'
                    ELSE role
                END;
            """);
    }

    private sealed class TestTenantContext(Guid tenantId) : ITenantContext
    {
        public bool IsAvailable => true;

        public Guid TenantId { get; } = tenantId;
    }
}
