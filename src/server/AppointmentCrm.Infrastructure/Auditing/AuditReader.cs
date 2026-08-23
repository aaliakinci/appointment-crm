using AppointmentCrm.Application.Auditing;
using AppointmentCrm.Application.Common;
using AppointmentCrm.Infrastructure.Common;
using AppointmentCrm.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AppointmentCrm.Infrastructure.Auditing;

internal sealed class AuditReader(AppointmentCrmDbContext dbContext) : IAuditReader
{
    public async Task<PagedResult<AuditSummary>> ListAsync(
        PageRequest request,
        AuditListFilter filter,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(filter);
        IQueryable<AuditProjection> query =
            from entry in dbContext.AuditEntries.AsNoTracking()
            join membership in dbContext.TenantMemberships.AsNoTracking()
                on new { entry.TenantId, Id = entry.ActorMembershipId }
                equals new { membership.TenantId, membership.Id }
            select new AuditProjection
            {
                Id = entry.Id,
                ActorUserId = entry.ActorUserId,
                ActorName = membership.User.DisplayName,
                Action = entry.Action,
                TargetType = entry.TargetType,
                TargetId = entry.TargetId,
                Summary = entry.Summary,
                OccurredAtUtc = entry.OccurredAtUtc,
            };

        if (filter.FromDate.HasValue || filter.ToDate.HasValue)
        {
            string timeZoneId = await dbContext.Tenants
                .AsNoTracking()
                .Select(tenant => tenant.TimeZone)
                .SingleAsync(cancellationToken);
            TimeZoneInfo timeZone = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
            DateOnly fromDate = filter.FromDate ?? DateOnly.MinValue.AddYears(1);
            DateOnly toDate = filter.ToDate ?? DateOnly.MaxValue.AddYears(-1);
            (DateTimeOffset startUtc, DateTimeOffset endUtc) = TenantLocalDateRange.Resolve(
                fromDate,
                toDate,
                timeZone);
            query = query.Where(item =>
                item.OccurredAtUtc >= startUtc && item.OccurredAtUtc < endUtc);
        }

        if (!string.IsNullOrWhiteSpace(filter.Action))
        {
            string action = filter.Action.Trim();
            query = query.Where(item => item.Action == action);
        }

        if (!string.IsNullOrWhiteSpace(filter.TargetType))
        {
            string targetType = filter.TargetType.Trim();
            query = query.Where(item => item.TargetType == targetType);
        }

        if (filter.ActorUserId.HasValue)
        {
            query = query.Where(item => item.ActorUserId == filter.ActorUserId.Value);
        }

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            string search = request.Search.Trim().ToUpperInvariant();
            query = query.Where(item =>
                item.ActorName.ToUpper().Contains(search)
                || item.Action.ToUpper().Contains(search)
                || item.TargetType.ToUpper().Contains(search));
        }

        int totalCount = await query.CountAsync(cancellationToken);
        query = Order(query, request);
        List<AuditSummary> items = await query
            .Skip(request.Skip)
            .Take(request.PageSize)
            .Select(item => new AuditSummary(
                item.Id,
                item.ActorUserId,
                item.ActorName,
                item.Action,
                item.TargetType,
                item.TargetId,
                item.Summary,
                item.OccurredAtUtc))
            .ToListAsync(cancellationToken);
        return new PagedResult<AuditSummary>(
            items,
            request.Page,
            request.PageSize,
            totalCount);
    }

    private static IQueryable<AuditProjection> Order(
        IQueryable<AuditProjection> query,
        PageRequest request) =>
        (request.SortBy, request.Descending) switch
        {
            ("occurredAt", false) => query.OrderBy(item => item.OccurredAtUtc),
            ("occurredAt", true) => query.OrderByDescending(item => item.OccurredAtUtc),
            ("actor", false) => query.OrderBy(item => item.ActorName),
            ("actor", true) => query.OrderByDescending(item => item.ActorName),
            ("action", false) => query.OrderBy(item => item.Action),
            ("action", true) => query.OrderByDescending(item => item.Action),
            ("target", false) => query.OrderBy(item => item.TargetType),
            ("target", true) => query.OrderByDescending(item => item.TargetType),
            _ => query.OrderByDescending(item => item.OccurredAtUtc),
        };

    private sealed class AuditProjection
    {
        public Guid Id { get; init; }

        public Guid ActorUserId { get; init; }

        public string ActorName { get; init; } = string.Empty;

        public string Action { get; init; } = string.Empty;

        public string TargetType { get; init; } = string.Empty;

        public Guid TargetId { get; init; }

        public string? Summary { get; init; }

        public DateTimeOffset OccurredAtUtc { get; init; }
    }
}
