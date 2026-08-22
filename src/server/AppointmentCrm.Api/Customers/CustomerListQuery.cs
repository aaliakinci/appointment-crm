using AppointmentCrm.Api.Controllers;
using Microsoft.AspNetCore.Mvc;

namespace AppointmentCrm.Api.Customers;

public sealed class CustomerListQuery : PageQuery
{
    [FromQuery(Name = "includeArchived")]
    public bool IncludeArchived { get; init; }

    protected override string DefaultSort => "name";

    protected override IReadOnlySet<string> AllowedSorts { get; } = new HashSet<string>(
        ["name", "createdAt", "updatedAt"],
        StringComparer.Ordinal);
}
