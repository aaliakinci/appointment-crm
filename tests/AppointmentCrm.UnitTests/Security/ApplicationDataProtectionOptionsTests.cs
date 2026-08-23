using AppointmentCrm.Api.Security;

namespace AppointmentCrm.UnitTests.Security;

public sealed class ApplicationDataProtectionOptionsTests
{
    [Fact]
    public void Production_RejectsEphemeralKeys()
    {
        var options = new ApplicationDataProtectionOptions
        {
            UseEphemeralKeys = true,
        };

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(
            () => ApplicationDataProtectionOptions.Validate(options, true));

        Assert.Contains("Ephemeral", exception.Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("", "/run/certificates/key.pfx", "secret")]
    [InlineData("/var/lib/keys", "", "secret")]
    [InlineData("/var/lib/keys", "/run/certificates/key.pfx", "")]
    public void Production_RequiresTheCompletePersistentKeyConfiguration(
        string keysPath,
        string certificatePath,
        string certificatePassword)
    {
        var options = new ApplicationDataProtectionOptions
        {
            KeysPath = keysPath,
            CertificatePath = certificatePath,
            CertificatePassword = certificatePassword,
        };

        Assert.Throws<InvalidOperationException>(
            () => ApplicationDataProtectionOptions.Validate(options, true));
    }

    [Fact]
    public void Production_AcceptsPersistentCertificateProtectedKeys()
    {
        var options = new ApplicationDataProtectionOptions
        {
            KeysPath = "/var/lib/keys",
            CertificatePath = "/run/certificates/key.pfx",
            CertificatePassword = "secret",
        };

        ApplicationDataProtectionOptions.Validate(options, true);
    }

    [Fact]
    public void Development_AllowsEphemeralKeys()
    {
        var options = new ApplicationDataProtectionOptions
        {
            UseEphemeralKeys = true,
        };

        ApplicationDataProtectionOptions.Validate(options, false);
    }
}
