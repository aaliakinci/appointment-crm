namespace AppointmentCrm.Api.Security;

internal sealed class ApplicationDataProtectionOptions
{
    public const string SectionName = "DataProtection";

    public bool UseEphemeralKeys { get; init; }

    public string KeysPath { get; init; } = string.Empty;

    public string CertificatePath { get; init; } = string.Empty;

    public string CertificatePassword { get; init; } = string.Empty;

    public static void Validate(
        ApplicationDataProtectionOptions options,
        bool requiresPersistentProtection)
    {
        if (!requiresPersistentProtection)
        {
            return;
        }

        if (options.UseEphemeralKeys)
        {
            throw new InvalidOperationException(
                "Ephemeral Data Protection keys are forbidden outside development and testing.");
        }

        if (string.IsNullOrWhiteSpace(options.KeysPath)
            || string.IsNullOrWhiteSpace(options.CertificatePath)
            || string.IsNullOrWhiteSpace(options.CertificatePassword))
        {
            throw new InvalidOperationException(
                "Production requires persistent DataProtection:KeysPath and a password-protected DataProtection certificate.");
        }
    }
}
