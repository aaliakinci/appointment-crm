using AppointmentCrm.Api.Controllers;
using Microsoft.AspNetCore.Mvc;

namespace AppointmentCrm.Api.Services;

public sealed class ServiceListQuery : PageQuery
{
    [FromQuery(Name = "isActive")]
    public bool? IsActive { get; init; }

    protected override string DefaultSort => "name";

    protected override IReadOnlySet<string> AllowedSorts { get; } = new HashSet<string>(
        ["name", "price", "duration", "updatedAt"],
        StringComparer.Ordinal);
}
