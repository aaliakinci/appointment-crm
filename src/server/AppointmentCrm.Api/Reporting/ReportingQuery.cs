using System.ComponentModel.DataAnnotations;
using AppointmentCrm.Application.Reporting;
using AppointmentCrm.Domain.Appointments;
using Microsoft.AspNetCore.Mvc;

namespace AppointmentCrm.Api.Reporting;

public sealed class ReportingQuery : IValidatableObject
{
    private static readonly IReadOnlySet<string> StatusValues = new HashSet<string>(
        ["scheduled", "confirmed", "completed", "cancelled", "no-show"],
        StringComparer.OrdinalIgnoreCase);

    [FromQuery(Name = "fromDate")]
    [Required]
    public DateOnly? FromDate { get; init; }

    [FromQuery(Name = "toDate")]
    [Required]
    public DateOnly? ToDate { get; init; }

    [FromQuery(Name = "employeeId")]
    public Guid? EmployeeId { get; init; }

    [FromQuery(Name = "status")]
    public string? Status { get; init; }

    internal ReportingFilter ToFilter() =>
        new(FromDate!.Value, ToDate!.Value, EmployeeId, ParseStatus(Status));

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        ArgumentNullException.ThrowIfNull(validationContext);
        if (FromDate.HasValue
            && ToDate.HasValue
            && (FromDate > ToDate || ToDate.Value.DayNumber - FromDate.Value.DayNumber > 91))
        {
            yield return new ValidationResult(
                "Reporting date range must be ordered and cannot exceed 92 days.",
                [nameof(ToDate)]);
        }

        if (!string.IsNullOrWhiteSpace(Status) && !StatusValues.Contains(Status.Trim()))
        {
            yield return new ValidationResult(
                "Status is not valid for reporting.",
                [nameof(Status)]);
        }
    }

    private static AppointmentStatus? ParseStatus(string? value) =>
        value?.Trim().ToLowerInvariant() switch
        {
            "scheduled" => AppointmentStatus.Scheduled,
            "confirmed" => AppointmentStatus.Confirmed,
            "completed" => AppointmentStatus.Completed,
            "cancelled" => AppointmentStatus.Cancelled,
            "no-show" => AppointmentStatus.NoShow,
            _ => null,
        };
}
