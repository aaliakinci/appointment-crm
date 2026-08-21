using AppointmentCrm.Infrastructure.Identity;

namespace AppointmentCrm.UnitTests.Identity;

public sealed class PasswordHashServiceTests
{
    private readonly PasswordHashService _service = new();

    [Fact]
    public void Hash_UsesRandomSalt_AndVerifiesOnlyTheOriginalPassword()
    {
        const string password = "A-secure-demo-password!";

        string first = _service.Hash(password);
        string second = _service.Hash(password);

        Assert.NotEqual(first, second);
        Assert.DoesNotContain(password, first, StringComparison.Ordinal);
        Assert.True(_service.Verify(first, password));
        Assert.False(_service.Verify(first, "A-different-password!"));
    }

    [Fact]
    public void Hash_RejectsShortPasswords()
    {
        Assert.Throws<ArgumentException>(() => _service.Hash("too-short"));
    }
}
