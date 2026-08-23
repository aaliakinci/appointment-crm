using AppointmentCrm.Api.Security;

namespace AppointmentCrm.UnitTests.Security;

public sealed class SecurityConfigurationTests
{
    [Theory]
    [InlineData("https://crm.example.com")]
    [InlineData("http://localhost:5173")]
    public void CorsOrigin_AllowsOnlyAbsoluteHttpOrigins(string origin)
    {
        Assert.True(SecurityConfiguration.IsAllowedOrigin(origin));
    }

    [Theory]
    [InlineData("*")]
    [InlineData("https://*.example.com")]
    [InlineData("https://user:password@example.com")]
    [InlineData("https://example.com/path")]
    [InlineData("file:///tmp/index.html")]
    public void CorsOrigin_RejectsBroadOrNonOriginValues(string origin)
    {
        Assert.False(SecurityConfiguration.IsAllowedOrigin(origin));
    }
}
