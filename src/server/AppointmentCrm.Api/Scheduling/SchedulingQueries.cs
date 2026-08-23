using System.ComponentModel.DataAnnotations;
using AppointmentCrm.Api.Controllers;
using AppointmentCrm.Contracts;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace AppointmentCrm.Api.Scheduling;

public sealed class DateRangeQuery : IValidatableObject
{
    [BindRequired]
    public DateOnly? FromDate { get; init; }

    [BindRequired]
    public DateOnly? ToDate { get; init; }

    public Guid? EmployeeId { get; init; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (FromDate.HasValue && ToDate.HasValue && FromDate.Value > ToDate.Value)
        {
            yield return new ValidationResult(
                "ToDate must be on or after FromDate.",
                [nameof(ToDate)]);
        }
    }
}

public sealed class AvailabilityRequestQuery
{
    [BindRequired]
    public DateOnly? Date { get; init; }

    [BindRequired]
    [NotEmptyGuid]
    public Guid EmployeeId { get; init; }

    [BindRequired]
    [NotEmptyGuid]
    public Guid ServiceId { get; init; }

    [NotEmptyGuid]
    public Guid? ExcludeAppointmentId { get; init; }
}

public sealed class WeeklyScheduleVersionListQuery : PageQuery
{
    protected override string DefaultSort => "version";

    protected override bool DefaultDescending => true;

    protected override IReadOnlySet<string> AllowedSorts { get; } = new HashSet<string>(
        ["version", "createdAt"],
        StringComparer.Ordinal);
}

public sealed class WeeklyScheduleInheritanceQuery
{
    [BindRequired]
    [Range(0, long.MaxValue)]
    public long? ExpectedRevision { get; init; }

    [StringLength(500)]
    public string? ChangeNote { get; init; }
}
