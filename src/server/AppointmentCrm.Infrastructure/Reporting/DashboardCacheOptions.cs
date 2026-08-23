namespace AppointmentCrm.Infrastructure.Reporting;

internal sealed class DashboardCacheOptions
{
    public const string SectionName = "DashboardCache";

    public bool Enabled { get; init; } = true;

    public int TimeToLiveSeconds { get; init; } = 30;

    public string KeyPrefix { get; init; } = "appointment-crm:";
}
