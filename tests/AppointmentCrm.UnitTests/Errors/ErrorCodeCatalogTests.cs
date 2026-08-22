using System.Reflection;
using System.Text.RegularExpressions;
using AppointmentCrm.Api.Errors;
using AppointmentCrm.Application.Common;
using AppointmentCrm.Application.Customers;
using AppointmentCrm.Application.Employees;
using AppointmentCrm.Application.Identity;
using AppointmentCrm.Application.Services;

namespace AppointmentCrm.UnitTests.Errors;

public sealed class ErrorCodeCatalogTests
{
    private static readonly Type[] Catalogs =
    [
        typeof(CommonErrorCodes),
        typeof(CustomerErrorCodes),
        typeof(EmployeeErrorCodes),
        typeof(IdentityErrorCodes),
        typeof(ServiceErrorCodes),
        typeof(ApiErrorCodes),
    ];

    [Fact]
    public void PublicErrorCodes_AreUniqueAndUseStableFeatureReasonFormat()
    {
        string[] codes = Catalogs
            .SelectMany(type => type.GetFields(BindingFlags.Public | BindingFlags.Static))
            .Where(field => field.IsLiteral && !field.IsInitOnly)
            .Select(field => Assert.IsType<string>(field.GetRawConstantValue()))
            .ToArray();

        Assert.NotEmpty(codes);
        Assert.Equal(codes.Length, codes.Distinct(StringComparer.Ordinal).Count());
        Assert.All(codes, code => Assert.Matches(
            new Regex("^[a-z][a-z0-9]*\\.[a-z][a-z0-9_]*$", RegexOptions.CultureInvariant),
            code));
    }
}
