using AppointmentCrm.Application.Auditing;
using AppointmentCrm.Application.Tenancy;
using AppointmentCrm.Domain.Auditing;
using AppointmentCrm.Infrastructure.Persistence;

namespace AppointmentCrm.Infrastructure.Auditing;

internal sealed class AuditWriter(
    AppointmentCrmDbContext dbContext,
    ITenantContext tenantContext,
    ICurrentActor currentActor)
{
    public void Add(
        string action,
        string targetType,
        Guid targetId,
        DateTimeOffset occurredAtUtc,
        string? summary = null)
    {
        if (!currentActor.IsAvailable)
        {
            throw new InvalidOperationException("Audit writes require an authenticated actor.");
        }

        dbContext.AuditEntries.Add(AuditEntry.Create(
            Guid.NewGuid(),
            tenantContext.TenantId,
            currentActor.UserId,
            currentActor.MembershipId,
            action,
            targetType,
            targetId,
            summary,
            occurredAtUtc));
    }
}
