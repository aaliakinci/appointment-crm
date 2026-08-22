using AppointmentCrm.Application.Identity;
using AppointmentCrm.Contracts;

namespace AppointmentCrm.Api.Identity;

internal static class MembershipContractMappings
{
    internal static IReadOnlyList<MembershipResponse> ToResponse(
        this IEnumerable<MembershipSummary> memberships) =>
        memberships.Select(membership => membership.ToResponse()).ToList();

    internal static MembershipResponse ToResponse(this MembershipSummary membership) =>
        new(
            membership.Id,
            membership.UserId,
            membership.Email,
            membership.DisplayName,
            membership.Role,
            membership.IsActive,
            membership.UpdatedAtUtc);

    internal static MembershipReportResponse ToResponse(this MembershipReport report) =>
        new(report.Total, report.Active, report.ByRole);
}
