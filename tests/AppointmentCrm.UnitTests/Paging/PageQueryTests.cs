using System.ComponentModel.DataAnnotations;
using AppointmentCrm.Api.Controllers;
using AppointmentCrm.Api.Customers;
using AppointmentCrm.Api.Employees;
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
