using System.ComponentModel.DataAnnotations;
using AppointmentCrm.Api.Appointments;
using AppointmentCrm.Api.Auditing;
using AppointmentCrm.Api.Controllers;
using AppointmentCrm.Api.Customers;
using AppointmentCrm.Api.Employees;
using AppointmentCrm.Api.Reporting;
using AppointmentCrm.Api.Services;
using AppointmentCrm.Application.Common;

namespace AppointmentCrm.UnitTests.Paging;

public sealed class PageQueryTests
{
    [Fact]
    public void BaseQuery_UsesCreatedAtAsItsDefaultSort()
    {
        var query = new DefaultPageQuery();

        PageRequest request = query.ToPageRequest();

        Assert.Equal("createdAt", request.SortBy);
    }

    [Fact]
    public void CustomerQuery_MapsDefaultsAndNormalizesTransportValues()
    {
        var query = new CustomerListQuery
        {
            Page = 2,
            PageSize = 40,
            Search = "  Ada  ",
            SortDirection = " DESC ",
        };

        PageRequest request = query.ToPageRequest();

        Assert.Equal(2, request.Page);
        Assert.Equal(40, request.PageSize);
        Assert.Equal("Ada", request.Search);
        Assert.Equal("name", request.SortBy);
        Assert.True(request.Descending);
    }

    [Fact]
    public void ServiceQuery_MapsFeatureSortAndFilterWithoutChangingExternalSemantics()
    {
        var query = new ServiceListQuery
        {
            SortBy = " price ",
            SortDirection = "asc",
            IsActive = false,
        };

        IReadOnlyList<ValidationResult> errors = Validate(query);
        PageRequest request = query.ToPageRequest();

        Assert.Empty(errors);
        Assert.Equal("price", request.SortBy);
        Assert.False(request.Descending);
        Assert.False(query.IsActive);
    }

    [Fact]
    public void EmployeeQuery_RejectsInvalidPagingValues()
    {
        var query = new EmployeeListQuery
        {
            Page = 0,
            PageSize = PageRequest.MaximumPageSize + 1,
            Search = new string('a', 161),
            SortDirection = "sideways",
        };

        IReadOnlyList<ValidationResult> errors = Validate(query);
        string[] members = errors.SelectMany(error => error.MemberNames).ToArray();

        Assert.Contains(nameof(query.Page), members);
        Assert.Contains(nameof(query.PageSize), members);
        Assert.Contains(nameof(query.Search), members);
        Assert.Contains(nameof(query.SortDirection), members);
    }

    [Fact]
    public void EmployeeQuery_RejectsSortOutsideItsFeatureCatalog()
    {
        var query = new EmployeeListQuery { SortBy = "salary" };

        IReadOnlyList<ValidationResult> errors = Validate(query);

        ValidationResult error = Assert.Single(errors);
        Assert.Contains(nameof(query.SortBy), error.MemberNames);
    }

    [Fact]
    public void AppointmentQuery_MapsFiltersAndRejectsInvalidStatusOrDateRange()
    {
        var valid = new AppointmentListQuery
        {
            FromDate = new DateOnly(2026, 8, 24),
            ToDate = new DateOnly(2026, 8, 30),
            Status = " CONFIRMED ",
            SortBy = "employee",
        };

        Assert.Empty(Validate(valid));
        Assert.Equal("employee", valid.ToPageRequest().SortBy);
        Assert.Equal(AppointmentCrm.Domain.Appointments.AppointmentStatus.Confirmed, valid.ToFilter().Status);

        var invalid = new AppointmentListQuery
        {
            FromDate = new DateOnly(2026, 8, 24),
            ToDate = new DateOnly(2026, 10, 1),
            Status = "waiting",
        };
        string[] members = Validate(invalid)
            .SelectMany(error => error.MemberNames)
            .ToArray();
        Assert.Contains(nameof(invalid.ToDate), members);
        Assert.Contains(nameof(invalid.Status), members);
    }

    [Fact]
    public void ReportingQuery_MapsFiltersAndEnforcesBoundedRange()
    {
        var valid = new ReportingQuery
        {
            FromDate = new DateOnly(2026, 8, 1),
            ToDate = new DateOnly(2026, 8, 31),
            Status = " COMPLETED ",
            EmployeeId = Guid.NewGuid(),
        };

        Assert.Empty(Validate(valid));
        Assert.Equal(
            AppointmentCrm.Domain.Appointments.AppointmentStatus.Completed,
            valid.ToFilter().Status);

        var invalid = new ReportingQuery
        {
            FromDate = new DateOnly(2026, 1, 1),
            ToDate = new DateOnly(2026, 5, 1),
            Status = "waiting",
        };
        string[] members = Validate(invalid).SelectMany(error => error.MemberNames).ToArray();
        Assert.Contains(nameof(invalid.ToDate), members);
        Assert.Contains(nameof(invalid.Status), members);
    }

    [Fact]
    public void AuditAndCustomerHistoryQueries_UseTheirFeatureSortDefaults()
    {
        var audit = new AuditListQuery();
        var history = new CustomerAppointmentQuery();

        Assert.Equal("occurredAt", audit.ToPageRequest().SortBy);
        Assert.True(audit.ToPageRequest().Descending);
        Assert.Equal("start", history.ToPageRequest().SortBy);
        Assert.True(history.ToPageRequest().Descending);
    }

    private static IReadOnlyList<ValidationResult> Validate(object value)
    {
        var errors = new List<ValidationResult>();
        Validator.TryValidateObject(
            value,
            new ValidationContext(value),
            errors,
            validateAllProperties: true);
        return errors;
    }

    private sealed class DefaultPageQuery : PageQuery
    {
    }
}
