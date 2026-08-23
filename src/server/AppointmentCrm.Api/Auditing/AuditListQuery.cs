using System.ComponentModel.DataAnnotations;
using AppointmentCrm.Api.Controllers;
using AppointmentCrm.Application.Auditing;
using Microsoft.AspNetCore.Mvc;

namespace AppointmentCrm.Api.Auditing;

public sealed class AuditListQuery : PageQuery
{
    [FromQuery(Name = "fromDate")]
    public DateOnly? FromDate { get; init; }

    [FromQuery(Name = "toDate")]
    public DateOnly? ToDate { get; init; }

    [FromQuery(Name = "action")]
    [StringLength(80)]
    public string? Action { get; init; }

    [FromQuery(Name = "targetType")]
    [StringLength(80)]
    public string? TargetType { get; init; }

    [FromQuery(Name = "actorUserId")]
    public Guid? ActorUserId { get; init; }

    protected override string DefaultSort => "occurredAt";

    protected override bool DefaultDescending => true;

    protected override IReadOnlySet<string> AllowedSorts { get; } = new HashSet<string>(
        ["occurredAt", "actor", "action", "target"],
        StringComparer.Ordinal);

    internal AuditListFilter ToFilter() =>
        new(FromDate, ToDate, Action?.Trim(), TargetType?.Trim(), ActorUserId);

    protected override IEnumerable<ValidationResult> ValidateAdditional(
        ValidationContext validationContext)
    {
        if (FromDate.HasValue && ToDate.HasValue && FromDate > ToDate)
        {
            yield return new ValidationResult(
                "Audit date range must be ordered.",
                [nameof(ToDate)]);
        }
    }
}
