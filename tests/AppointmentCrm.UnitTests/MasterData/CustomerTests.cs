using AppointmentCrm.Domain.Customers;

namespace AppointmentCrm.UnitTests.MasterData;

public sealed class CustomerTests
{
    [Fact]
    public void Create_NormalizesOptionalContactValues()
    {
        Customer customer = Customer.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "  Ayşe Demir  ",
            "  Ayse.Demir@Example.Test ",
            "+90 (555) 010 20 30",
            "  Morning appointments  ",
            DateTimeOffset.UtcNow);

        Assert.Equal("Ayşe Demir", customer.Name);
        Assert.Equal("AYSE.DEMIR@EXAMPLE.TEST", customer.NormalizedEmail);
        Assert.Equal("905550102030", customer.NormalizedPhone);
        Assert.Equal("Morning appointments", customer.Notes);
    }

    [Fact]
    public void Archive_IsIdempotent_AndPreventsFurtherChanges()
    {
        var now = DateTimeOffset.UtcNow;
        Customer customer = Customer.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Customer",
            null,
            null,
            null,
            now);

        Assert.True(customer.Archive(now.AddMinutes(1)));
        Assert.False(customer.Archive(now.AddMinutes(2)));
        Assert.Throws<InvalidOperationException>(() => customer.UpdateContact(
            "Changed",
            null,
            null,
            null,
            now.AddMinutes(3)));
    }

    [Theory]
    [InlineData("123456")]
    [InlineData("+90 123 456 789 012 345 6")]
    public void Create_RejectsPhoneOutsideSupportedDigitLength(string phone)
    {
        Assert.Throws<ArgumentException>(() => Customer.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Customer",
            null,
            phone,
            null,
            DateTimeOffset.UtcNow));
    }
}
