using AppointmentCrm.Domain.Common;
using AppointmentCrm.Domain.Customers;
using AppointmentCrm.Domain.Identity;

namespace AppointmentCrm.Domain.Services;

public sealed class Employee : ITenantOwnedEntity
{
    private Employee()
    {
    }

    public Guid Id { get; private set; }

    public Guid TenantId { get; private set; }

    public Guid? UserId { get; private set; }

    public string Name { get; private set; } = string.Empty;

    public string NormalizedName { get; private set; } = string.Empty;

    public string? Email { get; private set; }

    public string? NormalizedEmail { get; private set; }

    public string? Phone { get; private set; }

    public string? NormalizedPhone { get; private set; }

    public bool IsActive { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public DateTimeOffset UpdatedAtUtc { get; private set; }

    public TenantMembership? Membership { get; private set; }

    public ICollection<EmployeeService> ServiceAssignments { get; } = [];

    public static Employee Create(
        Guid id,
        Guid tenantId,
        Guid? userId,
        string name,
        string? email,
        string? phone,
        DateTimeOffset now)
    {
        var employee = new Employee
        {
            Id = id,
            TenantId = tenantId,
            IsActive = true,
            CreatedAtUtc = now,
        };
        employee.Update(userId, name, email, phone, now);
        return employee;
    }

    public void Update(
        Guid? userId,
        string name,
        string? email,
        string? phone,
        DateTimeOffset now)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        string trimmedName = name.Trim();
        if (trimmedName.Length is < 2 or > 160)
        {
            throw new ArgumentException(
                "Employee name must contain between 2 and 160 characters.",
                nameof(name));
        }

        string? trimmedEmail = string.IsNullOrWhiteSpace(email) ? null : email.Trim();
        string? trimmedPhone = string.IsNullOrWhiteSpace(phone) ? null : phone.Trim();
        if (trimmedPhone?.Length > 40)
        {
            throw new ArgumentException("Phone cannot exceed 40 characters.", nameof(phone));
        }

        UserId = userId;
        Name = trimmedName;
        NormalizedName = Customer.NormalizeName(trimmedName);
        Email = trimmedEmail;
        NormalizedEmail = Customer.NormalizeEmail(trimmedEmail);
        Phone = trimmedPhone;
        NormalizedPhone = Customer.NormalizePhone(trimmedPhone);
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
}
