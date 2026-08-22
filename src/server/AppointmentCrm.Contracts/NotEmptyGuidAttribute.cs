using System.ComponentModel.DataAnnotations;

namespace AppointmentCrm.Contracts;

[AttributeUsage(
    AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter,
    AllowMultiple = false)]
public sealed class NotEmptyGuidAttribute : ValidationAttribute
{
    public override bool IsValid(object? value) =>
        value is null || value is Guid guid && guid != Guid.Empty;
}
