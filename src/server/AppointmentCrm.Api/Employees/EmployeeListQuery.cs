using AppointmentCrm.Api.Controllers;
using Microsoft.AspNetCore.Mvc;

namespace AppointmentCrm.Api.Employees;

public sealed class EmployeeListQuery : PageQuery
{
    [FromQuery(Name = "isActive")]
    public bool? IsActive { get; init; }

    [FromQuery(Name = "serviceId")]
    public Guid? ServiceId { get; init; }

    protected override string DefaultSort => "name";

    protected override IReadOnlySet<string> AllowedSorts { get; } = new HashSet<string>(
        ["name", "createdAt", "updatedAt"],
        StringComparer.Ordinal);
}
