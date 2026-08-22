using System.Net.Mail;
using AppointmentCrm.Domain.Common;

namespace AppointmentCrm.Domain.Customers;

public sealed class Customer : ITenantOwnedEntity
{
    private Customer()
    {
    }

    public Guid Id { get; private set; }

    public Guid TenantId { get; private set; }

    public string Name { get; private set; } = string.Empty;

    public string NormalizedName { get; private set; } = string.Empty;

    public string? Email { get; private set; }

    public string? NormalizedEmail { get; private set; }

    public string? Phone { get; private set; }

    public string? NormalizedPhone { get; private set; }

    public string? Notes { get; private set; }

    public DateTimeOffset? ArchivedAtUtc { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public DateTimeOffset UpdatedAtUtc { get; private set; }

    public static Customer Create(
        Guid id,
        Guid tenantId,
        string name,
        string? email,
        string? phone,
        string? notes,
        DateTimeOffset now)
    {
        var customer = new Customer
        {
            Id = id,
            TenantId = tenantId,
            CreatedAtUtc = now,
        };
        customer.UpdateContact(name, email, phone, notes, now);
        return customer;
    }

    public void UpdateContact(
        string name,
        string? email,
        string? phone,
        string? notes,
        DateTimeOffset now)
    {
        if (ArchivedAtUtc is not null)
        {
            throw new InvalidOperationException("An archived customer cannot be changed.");
        }

        string trimmedName = NormalizeRequiredText(name, 2, 160, nameof(name));
        string? trimmedEmail = NormalizeOptionalText(email, 320, nameof(email));
        string? trimmedPhone = NormalizeOptionalText(phone, 40, nameof(phone));
        string? trimmedNotes = NormalizeOptionalText(notes, 2_000, nameof(notes));

        Name = trimmedName;
        NormalizedName = NormalizeName(trimmedName);
        Email = trimmedEmail;
        NormalizedEmail = NormalizeEmail(trimmedEmail);
        Phone = trimmedPhone;
        NormalizedPhone = NormalizePhone(trimmedPhone);
        Notes = trimmedNotes;
        UpdatedAtUtc = now;
    }

    public bool Archive(DateTimeOffset now)
    {
        if (ArchivedAtUtc is not null)
        {
            return false;
        }

        ArchivedAtUtc = now;
        UpdatedAtUtc = now;
        return true;
    }

    public static string NormalizeName(string value) => value.Trim().ToUpperInvariant();

    public static string? NormalizeEmail(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        string trimmed = value.Trim();
        if (!MailAddress.TryCreate(trimmed, out MailAddress? parsed)
            || !string.Equals(parsed.Address, trimmed, StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException("Email must be a valid address.", nameof(value));
        }

        return trimmed.ToUpperInvariant();
    }

    public static string? NormalizePhone(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        string digits = string.Concat(value.Where(char.IsAsciiDigit));
        if (digits.Length is < 7 or > 15)
        {
            throw new ArgumentException(
                "Phone must contain between 7 and 15 digits.",
                nameof(value));
        }

        return digits;
    }

    private static string NormalizeRequiredText(
        string value,
        int minimumLength,
        int maximumLength,
        string parameterName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, parameterName);
        string trimmed = value.Trim();
        if (trimmed.Length < minimumLength || trimmed.Length > maximumLength)
        {
            throw new ArgumentException(
                $"Value must contain between {minimumLength} and {maximumLength} characters.",
                parameterName);
        }

        return trimmed;
    }

    private static string? NormalizeOptionalText(
        string? value,
        int maximumLength,
        string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        string trimmed = value.Trim();
        if (trimmed.Length > maximumLength)
        {
            throw new ArgumentException(
                $"Value cannot exceed {maximumLength} characters.",
                parameterName);
        }

        return trimmed;
    }
}
