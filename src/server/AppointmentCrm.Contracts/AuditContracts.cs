namespace AppointmentCrm.Contracts;

public sealed record AuditResponse(
    Guid Id,
    Guid ActorUserId,
    string ActorName,
    string Action,
    string TargetType,
    Guid TargetId,
    string? Summary,
    DateTimeOffset OccurredAtUtc);
