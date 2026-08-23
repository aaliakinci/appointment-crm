using AppointmentCrm.Domain.Identity;

namespace AppointmentCrm.UnitTests.Identity;

public sealed class UserTests
{
    [Fact]
    public void UpdateProfile_NormalizesDisplayNameWithoutInvalidatingSecurityVersion()
    {
        DateTimeOffset createdAt = DateTimeOffset.Parse("2026-08-23T10:00:00Z");
        User user = User.Create(
            Guid.NewGuid(),
            "ada@example.test",
            "Ada",
            "password-hash",
            createdAt);

        user.UpdateProfile("  Ada Lovelace  ", createdAt.AddMinutes(5));

        Assert.Equal("Ada Lovelace", user.DisplayName);
        Assert.Equal(1, user.SecurityVersion);
        Assert.Equal(createdAt.AddMinutes(5), user.UpdatedAtUtc);
    }

    [Theory]
    [InlineData("")]
    [InlineData("x")]
    public void UpdateProfile_RejectsInvalidDisplayName(string displayName)
    {
        User user = User.Create(
            Guid.NewGuid(),
            "ada@example.test",
            "Ada",
            "password-hash",
            DateTimeOffset.UtcNow);

        Assert.Throws<ArgumentException>(() =>
            user.UpdateProfile(displayName, DateTimeOffset.UtcNow));
    }
}
