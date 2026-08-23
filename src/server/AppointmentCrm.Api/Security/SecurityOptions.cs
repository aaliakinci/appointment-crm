namespace AppointmentCrm.Api.Security;

internal sealed class SecurityOptions
{
    public const string SectionName = "Security";

    public long MaximumRequestBodyBytes { get; init; } = 1_048_576;

    public int WritePermitLimit { get; init; } = 120;

    public int WriteWindowSeconds { get; init; } = 60;

    public int ForwardLimit { get; init; } = 1;

    public string[] KnownProxies { get; init; } = [];
}
