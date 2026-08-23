using System.ComponentModel.DataAnnotations;
using AppointmentCrm.Api.Controllers;
using AppointmentCrm.Application.Appointments;
using AppointmentCrm.Domain.Appointments;
using Microsoft.AspNetCore.Mvc;

namespace AppointmentCrm.Api.Appointments;

public sealed class AppointmentListQuery : PageQuery
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

    [FromQuery(Name = "customerId")]
    public Guid? CustomerId { get; init; }

    [FromQuery(Name = "status")]
    public string? Status { get; init; }

    protected override string DefaultSort => "start";

    protected override IReadOnlySet<string> AllowedSorts { get; } = new HashSet<string>(
        ["start", "createdAt", "updatedAt", "customer", "employee", "status"],
        StringComparer.Ordinal);

    internal AppointmentListFilter ToFilter() =>
        new(
            FromDate!.Value,
            ToDate!.Value,
            EmployeeId,
            CustomerId,
            ParseStatus(Status));

    protected override IEnumerable<ValidationResult> ValidateAdditional(
        ValidationContext validationContext)
    {
        if (FromDate.HasValue
            && ToDate.HasValue
            && (FromDate > ToDate || ToDate.Value.DayNumber - FromDate.Value.DayNumber > 30))
        {
            yield return new ValidationResult(
                "Date range must be ordered and cannot exceed 31 days.",
                [nameof(ToDate)]);
        }

        if (!string.IsNullOrWhiteSpace(Status) && !StatusValues.Contains(Status.Trim()))
        {
            yield return new ValidationResult(
                "Status is not valid for appointments.",
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
