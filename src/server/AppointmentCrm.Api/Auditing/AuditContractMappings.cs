using AppointmentCrm.Application.Auditing;
using AppointmentCrm.Application.Common;
using AppointmentCrm.Contracts;

namespace AppointmentCrm.Api.Auditing;

internal static class AuditContractMappings
{
    internal static PagedResponse<AuditResponse> ToResponse(
        this PagedResult<AuditSummary> result) =>
        new(
            result.Items.Select(item => new AuditResponse(
                    item.Id,
                    item.ActorUserId,
                    item.ActorName,
                    item.Action,
                    item.TargetType,
                    item.TargetId,
                    item.Summary,
                    item.OccurredAtUtc))
                .ToList(),
            result.Page,
            result.PageSize,
            result.TotalCount,
            result.TotalPages);
}
