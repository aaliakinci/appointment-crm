using AppointmentCrm.Application.Common;

namespace AppointmentCrm.Application.Auditing;

public sealed record AuditListFilter(
    DateOnly? FromDate,
    DateOnly? ToDate,
    string? Action,
    string? TargetType,
    Guid? ActorUserId);

public sealed record AuditSummary(
    Guid Id,
    Guid ActorUserId,
    string ActorName,
    string Action,
    string TargetType,
    Guid TargetId,
    string? Summary,
    DateTimeOffset OccurredAtUtc);

public interface IAuditReader
{
    Task<PagedResult<AuditSummary>> ListAsync(
        PageRequest request,
        AuditListFilter filter,
        CancellationToken cancellationToken);
}
