namespace AppointmentCrm.Application.Identity;

public static class IdentityErrorCodes
{
    public const string InvalidCredentials = "auth.invalid_credentials";
    public const string InvalidSession = "auth.invalid_session";
    public const string TenantNotAvailable = "auth.tenant_not_available";
    public const string LastActiveOwner = "memberships.last_active_owner";
}
