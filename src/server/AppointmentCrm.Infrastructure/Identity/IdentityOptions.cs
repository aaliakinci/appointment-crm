namespace AppointmentCrm.Infrastructure.Identity;

public sealed class IdentityOptions
{
    public const string SectionName = "Identity";

    public int AccessTokenMinutes { get; init; } = 10;

    public int RefreshTokenDays { get; init; } = 30;

    public string RefreshCookieName { get; init; } = "appointment_crm_refresh";

    public bool RequireSecureCookie { get; init; } = true;

    public int LoginPermitLimit { get; init; } = 30;

    public int RefreshPermitLimit { get; init; } = 30;
}

public sealed class DemoSeedOptions
{
    public const string SectionName = "DemoSeed";

    public bool Enabled { get; init; }

    public string Password { get; init; } = "123456";

    public bool PublicMode { get; init; }

    public bool ResetEnabled { get; init; }
}
