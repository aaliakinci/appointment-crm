using AppointmentCrm.Domain.Services;

namespace AppointmentCrm.UnitTests.MasterData;

public sealed class ServiceOfferingTests
{
    [Fact]
    public void Create_NormalizesNameAndCurrency()
    {
        ServiceOffering service = ServiceOffering.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "  Consultation  ",
            30,
            250.50m,
            "try",
            DateTimeOffset.UtcNow);

        Assert.Equal("Consultation", service.Name);
        Assert.Equal("CONSULTATION", service.NormalizedName);
        Assert.Equal("TRY", service.Currency);
        Assert.True(service.IsActive);
    }

    [Theory]
    [InlineData(4)]
    [InlineData(31)]
    [InlineData(485)]
    public void Create_RejectsInvalidDuration(int durationMinutes)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => ServiceOffering.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Consultation",
            durationMinutes,
            250m,
            "TRY",
            DateTimeOffset.UtcNow));
    }

    [Fact]
    public void SetActive_IsIdempotent()
    {
        ServiceOffering service = ServiceOffering.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Consultation",
            30,
            250m,
            "TRY",
            DateTimeOffset.UtcNow);

        Assert.True(service.SetActive(false, DateTimeOffset.UtcNow));
        Assert.False(service.SetActive(false, DateTimeOffset.UtcNow));
    }
}
