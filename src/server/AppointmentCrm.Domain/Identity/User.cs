namespace AppointmentCrm.Domain.Identity;

public sealed class User
{
    private User()
    {
    }

    public Guid Id { get; private set; }

    public string Email { get; private set; } = string.Empty;

    public string NormalizedEmail { get; private set; } = string.Empty;

    public string DisplayName { get; private set; } = string.Empty;

    public string PasswordHash { get; private set; } = string.Empty;

    public bool IsActive { get; private set; }

    public int SecurityVersion { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public DateTimeOffset UpdatedAtUtc { get; private set; }

    public ICollection<TenantMembership> Memberships { get; } = [];

    public static User Create(
        Guid id,
        string email,
        string displayName,
        string passwordHash,
        DateTimeOffset now)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(email);
        ArgumentException.ThrowIfNullOrWhiteSpace(displayName);
        ArgumentException.ThrowIfNullOrWhiteSpace(passwordHash);

        return new User
        {
            Id = id,
            Email = email.Trim(),
            NormalizedEmail = NormalizeEmail(email),
            DisplayName = displayName.Trim(),
            PasswordHash = passwordHash,
            IsActive = true,
            SecurityVersion = 1,
            CreatedAtUtc = now,
            UpdatedAtUtc = now,
        };
    }

    public static string NormalizeEmail(string email) =>
        email.Trim().ToUpperInvariant();

    public void ChangePassword(string passwordHash, DateTimeOffset now)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(passwordHash);
        PasswordHash = passwordHash;
        SecurityVersion++;
        UpdatedAtUtc = now;
    }

    public void SetActive(bool isActive, DateTimeOffset now)
    {
        if (IsActive == isActive)
        {
            return;
        }

        IsActive = isActive;
        SecurityVersion++;
        UpdatedAtUtc = now;
    }
}
