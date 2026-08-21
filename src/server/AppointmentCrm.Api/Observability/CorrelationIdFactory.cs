namespace AppointmentCrm.Api.Observability;

internal static class CorrelationIdFactory
{
    public static string Create(string? candidate)
    {
        return Guid.TryParse(candidate, out var correlationId)
            ? correlationId.ToString("N")
            : Guid.NewGuid().ToString("N");
    }
}
