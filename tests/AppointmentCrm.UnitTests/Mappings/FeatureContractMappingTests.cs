using AppointmentCrm.Api.Appointments;
using AppointmentCrm.Api.Auditing;
using AppointmentCrm.Api.Customers;
using AppointmentCrm.Api.Employees;
using AppointmentCrm.Api.Identity;
using AppointmentCrm.Api.Reporting;
using AppointmentCrm.Api.Services;
using AppointmentCrm.Application.Appointments;
using AppointmentCrm.Application.Auditing;
using AppointmentCrm.Application.Common;
using AppointmentCrm.Application.Customers;
using AppointmentCrm.Application.Employees;
using AppointmentCrm.Application.Identity;
using AppointmentCrm.Application.Reporting;
using AppointmentCrm.Application.Services;
using AppointmentCrm.Contracts;
using AppointmentCrm.Domain.Appointments;

namespace AppointmentCrm.UnitTests.Mappings;

public sealed class FeatureContractMappingTests
{
    [Fact]
    public void CustomerMappings_PreserveInputItemsAndPageMetadata()
    {
        var request = new CreateCustomerRequest("Ada", "ada@example.test", null, "VIP");
        var summary = new CustomerSummary(
            Guid.NewGuid(),
            request.Name,
            request.Email,
            request.Phone,
            request.Notes,
            null,
            DateTimeOffset.Parse("2026-08-22T10:00:00Z"),
            DateTimeOffset.Parse("2026-08-22T11:00:00Z"));
        var result = new PagedResult<CustomerSummary>([summary], 2, 20, 41);

        CustomerInput input = request.ToInput();
        PagedResponse<CustomerResponse> response = result.ToResponse();

        Assert.Equal(request.Name, input.Name);
        Assert.Equal(request.Notes, input.Notes);
        Assert.Equal(2, response.Page);
        Assert.Equal(3, response.TotalPages);
        Assert.Equal(summary.Id, Assert.Single(response.Items).Id);
    }

    [Fact]
    public void ServiceMappings_PreserveTransportValues()
    {
        var request = new UpdateServiceRequest("Consultation", 45, 750.50m, "TRY");
        var summary = new ServiceSummary(
            Guid.NewGuid(),
            request.Name,
            request.DurationMinutes,
            request.Price,
            request.Currency,
            true,
            DateTimeOffset.Parse("2026-08-22T10:00:00Z"),
            DateTimeOffset.Parse("2026-08-22T11:00:00Z"));

        ServiceInput input = request.ToInput();
        ServiceResponse response = summary.ToResponse();

        Assert.Equal(request.DurationMinutes, input.DurationMinutes);
        Assert.Equal(request.Price, response.Price);
        Assert.Equal(request.Currency, response.Currency);
        Assert.True(response.IsActive);
    }

    [Fact]
    public void EmployeeMappings_PreserveNestedServicesAndUserOptions()
    {
        var service = new EmployeeServiceSummary(Guid.NewGuid(), "Consultation", true);
        var summary = new EmployeeSummary(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Grace",
            "grace@example.test",
            null,
            true,
            [service],
            DateTimeOffset.Parse("2026-08-22T10:00:00Z"),
            DateTimeOffset.Parse("2026-08-22T11:00:00Z"));
        EmployeeUserOption[] options =
        [
            new(Guid.NewGuid(), "Linus", "linus@example.test", "Employee", false),
        ];

        EmployeeResponse response = summary.ToResponse();
        IReadOnlyList<EmployeeUserOptionResponse> optionResponses = options.ToResponse();

        Assert.Equal(service.Id, Assert.Single(response.Services).Id);
        Assert.Equal(options[0].UserId, Assert.Single(optionResponses).UserId);
    }

    [Fact]
    public void IdentityMappings_PreserveTenantSelectionAndMembershipReport()
    {
        var tenant = new TenantOption(Guid.NewGuid(), "Atlas", "atlas", "Owner");
        AuthenticationOutcome outcome = AuthenticationOutcome.SelectionRequired([tenant]);
        var report = new MembershipReport(
            Total: 3,
            Active: 2,
            ByRole: new Dictionary<string, int> { ["Owner"] = 1 });

        AuthenticationResponse authentication = outcome.ToAuthenticationResponse();
        MembershipReportResponse membershipReport = report.ToResponse();

        Assert.True(authentication.RequiresTenantSelection);
        Assert.Equal(tenant.Id, Assert.Single(authentication.Tenants).Id);
        Assert.Equal(report.Total, membershipReport.Total);
        Assert.Equal(report.ByRole, membershipReport.ByRole);
    }

    [Fact]
    public void AppointmentMappings_PreserveSnapshotStatusAndHistory()
    {
        var summary = new AppointmentSummary(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Ada",
            Guid.NewGuid(),
            "Grace",
            Guid.NewGuid(),
            "Consultation snapshot",
            45,
            750.50m,
            "TRY",
            AppointmentStatus.Confirmed,
            DateTimeOffset.Parse("2026-08-24T07:00:00Z"),
            DateTimeOffset.Parse("2026-08-24T07:45:00Z"),
            DateTimeOffset.Parse("2026-08-24T10:00:00+03:00"),
            DateTimeOffset.Parse("2026-08-24T10:45:00+03:00"),
            "Europe/Istanbul",
            "First visit",
            2,
            DateTimeOffset.Parse("2026-08-23T10:00:00Z"),
            DateTimeOffset.Parse("2026-08-23T11:00:00Z"));
        var history = new AppointmentStatusHistorySummary(
            Guid.NewGuid(),
            AppointmentStatus.Scheduled,
            AppointmentStatus.Confirmed,
            "Manager",
            null,
            DateTimeOffset.Parse("2026-08-23T11:00:00Z"));

        AppointmentResponse response = new AppointmentDetail(summary, [history]).ToResponse();

        Assert.Equal("confirmed", response.Appointment.Status);
        Assert.Equal("Consultation snapshot", response.Appointment.ServiceName);
        Assert.Equal(45, response.Appointment.ServiceDurationMinutes);
        AppointmentStatusHistoryResponse mappedHistory = Assert.Single(response.StatusHistory);
        Assert.Equal("scheduled", mappedHistory.FromStatus);
        Assert.Equal("confirmed", mappedHistory.ToStatus);
    }

    [Fact]
    public void ReportingMappings_PreserveCompletedSnapshotRevenueAndBreakdowns()
    {
        var headline = new ReportingHeadline(3, 1, 0, 1, 0, 1, 750m);
        var dashboard = new ReportingDashboard(
            new DateOnly(2026, 8, 1),
            new DateOnly(2026, 8, 31),
            new DateOnly(2026, 8, 23),
            "Europe/Istanbul",
            "TRY",
            headline,
            headline,
            [new ReportingStatusBreakdown(AppointmentStatus.Completed, 1, 750m)],
            [new ReportingEmployeeBreakdown(Guid.NewGuid(), "Grace", 3, 1, 1, 750m)],
            [new ReportingDailyBreakdown(new DateOnly(2026, 8, 23), 3, 1, 750m)]);

        ReportingDashboardResponse response = dashboard.ToResponse();

        Assert.Equal(750m, response.Range.CompletedRevenue);
        Assert.Equal("completed", Assert.Single(response.ByStatus).Status);
        Assert.Equal("Grace", Assert.Single(response.ByEmployee).EmployeeName);
    }

    [Fact]
    public void AuditAndAccountMappings_PreserveSafeReadModelFields()
    {
        var audit = new AuditSummary(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Ada",
            "appointment.completed",
            "appointment",
            Guid.NewGuid(),
            "status=completed",
            DateTimeOffset.Parse("2026-08-23T10:00:00Z"));
        var auditPage = new PagedResult<AuditSummary>([audit], 1, 20, 1).ToResponse();
        var profile = new AccountProfile(
            Guid.NewGuid(),
            "ada@example.test",
            "Ada",
            DateTimeOffset.Parse("2026-08-23T10:00:00Z"));

        Assert.Equal(audit.Action, Assert.Single(auditPage.Items).Action);
        Assert.Equal(audit.Summary, Assert.Single(auditPage.Items).Summary);
        Assert.Equal(profile.DisplayName, profile.ToResponse().DisplayName);
    }
}
