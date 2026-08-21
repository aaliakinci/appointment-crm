namespace AppointmentCrm.Domain.Identity;

public sealed class Tenant
{
    private Tenant()
    {
    }

    public Guid Id { get; private set; }

    public string Name { get; private set; } = string.Empty;

    public string Slug { get; private set; } = string.Empty;

    public string TimeZone { get; private set; } = string.Empty;

    public string Currency { get; private set; } = string.Empty;

    public bool IsActive { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public DateTimeOffset UpdatedAtUtc { get; private set; }

    public ICollection<TenantMembership> Memberships { get; } = [];

    public static Tenant Create(
        Guid id,
        string name,
        string slug,
        string timeZone,
        string currency,
        DateTimeOffset now)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(slug);
        ArgumentException.ThrowIfNullOrWhiteSpace(timeZone);
        ArgumentException.ThrowIfNullOrWhiteSpace(currency);

        return new Tenant
        {
            Id = id,
            Name = name.Trim(),
            Slug = slug.Trim().ToLowerInvariant(),
            TimeZone = timeZone.Trim(),
            Currency = currency.Trim().ToUpperInvariant(),
            IsActive = true,
            CreatedAtUtc = now,
            UpdatedAtUtc = now,
        };
    }
}
