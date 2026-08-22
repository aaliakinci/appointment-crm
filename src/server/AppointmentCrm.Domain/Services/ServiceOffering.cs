using AppointmentCrm.Domain.Common;

namespace AppointmentCrm.Domain.Services;

public sealed class ServiceOffering : ITenantOwnedEntity
{
    private ServiceOffering()
    {
    }

    public Guid Id { get; private set; }

    public Guid TenantId { get; private set; }

    public string Name { get; private set; } = string.Empty;

    public string NormalizedName { get; private set; } = string.Empty;

    public int DurationMinutes { get; private set; }

    public decimal Price { get; private set; }

    public string Currency { get; private set; } = string.Empty;

    public bool IsActive { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public DateTimeOffset UpdatedAtUtc { get; private set; }

    public ICollection<EmployeeService> EmployeeAssignments { get; } = [];

    public static ServiceOffering Create(
        Guid id,
        Guid tenantId,
        string name,
        int durationMinutes,
        decimal price,
        string currency,
        DateTimeOffset now)
    {
        var service = new ServiceOffering
        {
            Id = id,
            TenantId = tenantId,
            IsActive = true,
            CreatedAtUtc = now,
        };
        service.Update(name, durationMinutes, price, currency, now);
        return service;
    }

    public void Update(
        string name,
        int durationMinutes,
        decimal price,
        string currency,
        DateTimeOffset now)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        string trimmedName = name.Trim();
        if (trimmedName.Length is < 2 or > 160)
        {
            throw new ArgumentException(
                "Service name must contain between 2 and 160 characters.",
                nameof(name));
        }

        if (durationMinutes is < 5 or > 480 || durationMinutes % 5 != 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(durationMinutes),
                "Duration must be between 5 and 480 minutes in five-minute increments.");
        }

        if (price is < 0 or > 1_000_000 || decimal.Round(price, 2) != price)
        {
            throw new ArgumentOutOfRangeException(
                nameof(price),
                "Price must be between 0 and 1,000,000 with at most two decimal places.");
        }

        string normalizedCurrency = NormalizeCurrency(currency);
        Name = trimmedName;
        NormalizedName = NormalizeName(trimmedName);
        DurationMinutes = durationMinutes;
        Price = price;
        Currency = normalizedCurrency;
        UpdatedAtUtc = now;
    }

    public bool SetActive(bool isActive, DateTimeOffset now)
    {
        if (IsActive == isActive)
        {
            return false;
        }

        IsActive = isActive;
        UpdatedAtUtc = now;
        return true;
    }

    public static string NormalizeName(string value) => value.Trim().ToUpperInvariant();

    public static string NormalizeCurrency(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        string normalized = value.Trim().ToUpperInvariant();
        if (normalized.Length != 3 || normalized.Any(character => !char.IsAsciiLetter(character)))
        {
            throw new ArgumentException(
                "Currency must be a three-letter ISO code.",
                nameof(value));
        }

        return normalized;
    }
}
