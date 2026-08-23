using AppointmentCrm.Application.Identity;
using AppointmentCrm.Contracts;

namespace AppointmentCrm.Api.Identity;

internal static class AccountContractMappings
{
    internal static AccountProfileResponse ToResponse(this AccountProfile profile) =>
        new(
            profile.UserId,
            profile.Email,
            profile.DisplayName,
            profile.UpdatedAtUtc);

    internal static IReadOnlyList<AccountSessionResponse> ToResponse(
        this IEnumerable<AccountSession> sessions) =>
        sessions.Select(session => new AccountSessionResponse(
                session.Id,
                session.TenantName,
                session.CreatedAtUtc,
                session.LastUsedAtUtc,
                session.ExpiresAtUtc,
                session.IsCurrent))
            .ToList();
}
