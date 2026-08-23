using AppointmentCrm.Application.Reporting;
using AppointmentCrm.Infrastructure.Reporting;

namespace AppointmentCrm.UnitTests.Reporting;

public sealed class DashboardCacheKeyTests
{
    [Fact]
    public void CacheKey_SeparatesTenantAndFilterDimensions()
    {
        var filter = new ReportingFilter(
            new DateOnly(2026, 8, 1),
            new DateOnly(2026, 8, 31),
            null,
            null);
        Guid firstTenant = Guid.Parse("10000000-0000-0000-0000-000000000001");
        Guid secondTenant = Guid.Parse("10000000-0000-0000-0000-000000000002");

        string first = DashboardCacheKey.Create("appointment-crm:test:", firstTenant, filter);
        string second = DashboardCacheKey.Create("appointment-crm:test:", secondTenant, filter);

        Assert.NotEqual(first, second);
        Assert.Contains(firstTenant.ToString("N"), first, StringComparison.Ordinal);
        Assert.DoesNotContain(secondTenant.ToString("N"), first, StringComparison.Ordinal);
    }
}
