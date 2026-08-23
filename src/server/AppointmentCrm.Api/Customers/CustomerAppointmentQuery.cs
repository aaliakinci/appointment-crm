using AppointmentCrm.Api.Controllers;

namespace AppointmentCrm.Api.Customers;

public sealed class CustomerAppointmentQuery : PageQuery
{
    protected override string DefaultSort => "start";

    protected override bool DefaultDescending => true;

    protected override IReadOnlySet<string> AllowedSorts { get; } = new HashSet<string>(
        ["start", "createdAt", "updatedAt", "employee", "status"],
        StringComparer.Ordinal);
}
